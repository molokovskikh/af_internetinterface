using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InternetInterface.Models;
using NHibernate.Linq;
using NHibernate.Util;
using Client = Inforoom2.Models.Client;
using ClientService = Inforoom2.Models.ClientService;
using Contact = Inforoom2.Models.Contact;
using House = Inforoom2.Models.House;
using PhysicalClient = Inforoom2.Models.PhysicalClient;
using RequestType = Inforoom2.Models.RequestType;
using ServiceRequest = Inforoom2.Models.ServiceRequest;
using Status = Inforoom2.Models.Status;
using StatusType = Inforoom2.Models.StatusType;
using Street = Inforoom2.Models.Street;

namespace InforoomControlPanel.Controllers
{
	public class ClientController : ControlPanelController
	{
		public ClientController()
		{
			ViewBag.BreadCrumb = "Клиенты";
		}

		public ActionResult Index()
		{
			return List();
		}

		/// <summary>
		///		Обработка события OnActionExecuting (для каждого Action текущего контроллера) 
		/// </summary>
		/// <param name="filterContext"></param>
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.BreadCrumb = "Клиенты";
		}

		public ActionResult List()
		{
			var pager = new ModelFilter<Client>(this, urlBasePrefix: "/");
			var clients = pager.GetCriteria(i => i.PhysicalClient != null).List<Client>();

			ViewBag.Pager = pager;
			ViewBag.Clients = clients;

			//Пагинация
			ViewBag.Models = clients;
			ViewBag.Page = pager;
			ViewBag.ModelsPerPage = pager.ItemsPerPage;
			ViewBag.ModelsCount = DbSession.QueryOver<Client>().Where(i => i.PhysicalClient != null).RowCount();
			return View("List");
		}

		public ActionResult Info(int id)
		{
			// Find Client
			var client = DbSession.Query<Client>().FirstOrDefault(i => i.PhysicalClient != null && i.Id == id);
			ViewBag.Client = client;
			// Find active RentalHardware
			var activeServices = client.RentalHardwareList.Where(rh => rh.IsActive).ToList();
			ViewBag.RentIsActive = activeServices.Count > 0;
			if (client.Status != null && client.Status.Type == StatusType.BlockedAndConnected) {
				// Find Switches
				var networkNodeList = DbSession.QueryOver<SwitchAddress>().Where(s =>
					s.House == client.PhysicalClient.Address.House && s.Entrance.ToString() == client.PhysicalClient.Address.Entrance ||
					s.House == client.PhysicalClient.Address.House && s.Entrance == null).List();
				if (networkNodeList.Count > 0) {
					ViewBag.NetworkNodeList = networkNodeList;
				}
			}
			return View();
		}

		[HttpPost]
		public ActionResult Info([EntityBinder] Client client)
		{
			DbSession.Update(client);
			return View();
		}

		/// <summary>
		/// Отображает форму новой заявки
		/// </summary>
		public ActionResult Request()
		{
			InitClientRequest();
			return View();
		}

		[HttpPost]
		public ActionResult Request([EntityBinder] ClientRequest clientRequest)
		{
			clientRequest.ActionDate = clientRequest.RegDate = DateTime.Now;
			// Заявка от оператора по умочанию  
			clientRequest.RequestSource = RequestType.FromOperator;
			clientRequest.RequestAuthor = GetCurrentEmployee();
			// Сохранение адреса  
			if (clientRequest.Housing != null && clientRequest.Housing != "") {
				string houseNumber = "";
				string justStr = clientRequest.Housing;
				foreach (char t in justStr) {
					try {
						houseNumber += Convert.ToInt32(t.ToString()).ToString();
					}
					catch (Exception) {
						break;
					}
				}
				houseNumber = houseNumber == string.Empty ? "0" : houseNumber;
				clientRequest.HouseNumber = Convert.ToInt32(houseNumber);
				// отделение буквенной части от "Номера дома"
				var housingPostfix = clientRequest.Housing.IndexOf(houseNumber);
				housingPostfix = housingPostfix == -1 ? 0 : housingPostfix + houseNumber.Length;
				clientRequest.Housing = clientRequest.Housing.Substring(housingPostfix,
					clientRequest.Housing.Length - housingPostfix);
			}
			// валидация и сохранение
			var errors = ValidationRunner.ValidateDeep(clientRequest);
			if (errors.Length == 0 && clientRequest.IsContractAccepted) {
				// чистим адрес - его сохранять не нужно					  TODO: не помогает !!!
				clientRequest.Address = null;
				// сохранение
				DbSession.Save(clientRequest);
				SuccessMessage(string.Format("Спасибо, Ваша заявка создана. Номер заявки {0}", clientRequest.Id));
				return RedirectToAction("Request");
			}
			// Пока используется IsContractAccepted=true, закомментированные строки кода не нужны
			//if (!clientRequest.IsContractAccepted) {
			//	ErrorMessage("Пожалуйста, подтвердите, что Вы согласны с договором-офертой");
			//}
			Street currentStreet = null;
			House currentHouse = null;
			Region currentRegion = null;
			// получаем списки регионов 
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			if (clientRequest.Address != null && clientRequest.Address.House != null) {
				currentHouse = clientRequest.Address.House;
				currentStreet = clientRequest.Address.House.Street;
				currentRegion = clientRequest.Address.House.Region;
			}
			// получаем списки тарифов по выбранному выбранному региону
			var planList = new List<Plan>();
			if (currentRegion != null) {
				planList = DbSession.Query<Plan>().Where(s => s.IsArchived == false && s.RegionPlans.Any(d => d.Region == (currentRegion))).OrderBy(s => s.Name).ToList();
			}
			// списки улиц и домов
			var currentStreetList = currentStreet == null || currentRegion == null ? new List<Street>() : DbSession.Query<Street>()
				.Where(s => s.Region.Id == currentRegion.Id || s.Houses.Any(a => a.Region.Id == currentRegion.Id)).OrderBy(s => s.Name).ToList();
			var currentHouseList = currentStreet == null || currentRegion == null ? new List<House>() : DbSession.Query<House>().Where(s => (s.Region == null || currentRegion.Id == s.Region.Id) &&
			                                                                                                                                ((s.Street.Region.Id == currentRegion.Id && s.Street.Id == currentStreet.Id) || (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id)) &&
			                                                                                                                                (s.Street.Region.Id == currentRegion.Id && s.Region == null || (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id))).OrderBy(s => s.Number).ToList();
			ViewBag.CurrentRegion = currentRegion;
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentHouse = currentHouse;
			ViewBag.RegionList = regionList;
			ViewBag.CurrentStreetList = currentStreetList;
			ViewBag.CurrentHouseList = currentHouseList;
			ViewBag.PlanList = planList;
			ViewBag.IsRedirected = false;
			ViewBag.IsCityValidated = false;
			ViewBag.IsStreetValidated = false;
			ViewBag.IsHouseValidated = false;
			ViewBag.ClientRequest = clientRequest;

			return View();
		}

		private void InitClientRequest()
		{
			var clientRequest = new ClientRequest {
				IsContractAccepted = true
			};
			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			// списки улиц и домов 
			ViewBag.RegionList = regionList;
			ViewBag.CurrentStreetList = new List<Street>();
			ViewBag.CurrentHouseList = new List<House>();
			ViewBag.PlanList = new List<Plan>();
			ViewBag.CurrentRegion = null;
			ViewBag.CurrentStreet = null;
			ViewBag.CurrentHouse = null;
			ViewBag.IsRedirected = false;
			ViewBag.IsCityValidated = false;
			ViewBag.IsStreetValidated = false;
			ViewBag.IsHouseValidated = false;
			ViewBag.ClientRequest = clientRequest;
		}

		// TODO: поправить процедуру!
		protected Address GetAddressByYandexData(ClientRequest clientRequest)
		{
			var region = DbSession.Query<Region>().FirstOrDefault(r => r.Name == clientRequest.City);

			var street = DbSession.Query<Street>().FirstOrDefault(s => s.Name == clientRequest.YandexStreet && s.Region == region);
			if (street == null) {
				street = new Street(clientRequest.YandexStreet);
			}
			var house = DbSession.Query<House>().FirstOrDefault(h => h.Number == clientRequest.YandexHouse
			                                                         && h.Street.Name == clientRequest.YandexStreet
			                                                         && (h.Street.Region == region
			                                                             || h.Region == region));
			if (house == null) {
				house = new House(clientRequest.YandexHouse);
			}
			var address = GetList<Address>().FirstOrDefault(a => a.IsCorrectAddress
			                                                     && a.House == house
			                                                     && a.House.Street == street
			                                                     && a.House.Street.Region == region);
			return address;
		}

		/// <summary>
		/// Список заявок на подключение
		/// </summary>
		/// <param name="ClientRequest"></param>
		/// <returns></returns>
		public ActionResult RequestsList()
		{
			var pager = new ModelFilter<ClientRequest>(this, urlBasePrefix: "/");
			var clientRequests = pager.GetCriteria().List<ClientRequest>();
			ViewBag.Pager = pager;
			ViewBag.ClientRequests = clientRequests;
			return View();
		}

		/// <summary>
		/// Создание заявки на подключение
		/// </summary> 
		/// <param name="id"></param>
		/// <returns></returns>
		public ActionResult ServiceRequest(int id)
		{
			var client = DbSession.Get<Client>(id);
			var serviceRequest = new ServiceRequest(client);
			var servicemen = DbSession.Query<ServiceMan>().ToList();
			ViewBag.Client = client;
			ViewBag.ServiceRequest = serviceRequest;
			ViewBag.Servicemen = servicemen;
			ViewBag.ServicemenDate = DateTime.Today;
			return View();
		}

		/// <summary>
		/// Создание заявки на подключение
		/// </summary>
		/// <param name="ServiceRequest"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult ServiceRequest([EntityBinder] ServiceRequest ServiceRequest)
		{
			var client = ServiceRequest.Client;
			this.ServiceRequest(client.Id);
			ViewBag.ServicemenDate = ServiceRequest.BeginTime.Date;
			var errors = ValidationRunner.ValidateDeep(ServiceRequest);
			if (errors.Length == 0) {
				DbSession.Save(ServiceRequest);
				SuccessMessage("Сервисная заявка успешно добавлена");
				return this.ServiceRequest(client.Id);
			}
			ViewBag.ServiceRequest = ServiceRequest;
			return View();
		}


		/// <summary>
		///  Форма регистрации клиента по заявке
		/// </summary>
		/// <param name="id">Id заявки</param>
		/// <returns></returns>
		public ActionResult RequestRegistration(int id)
		{
			// Создаем клиента
			var client = new Client();
			// запрос клиента
			ClientRequest clientRequest = DbSession.Query<ClientRequest>().First(s => s.Id == id);
			// Создаем физ.клиента
			client.PhysicalClient = new PhysicalClient();
			// ФИО по запросу
			string[] fio = clientRequest.ApplicantName.Trim().Split(' ');
			client.PhysicalClient.Surname = fio.Length > 0 ? fio[0] : "";
			client.PhysicalClient.Name = fio.Length > 1 ? fio[1] : "";
			client.PhysicalClient.Patronymic = fio.Length > 2 ? fio[2] : "";
			// контакты по запросу
			// Контакты находятся в отдельной таблице
			client.Contacts = new List<Contact>();
			if (clientRequest.ApplicantPhoneNumber != null) {
				client.Contacts.Add(new Contact() {
					Client = client,
					ContactString = clientRequest.ApplicantPhoneNumber.IndexOf('-') != -1 ? clientRequest.ApplicantPhoneNumber : clientRequest.ApplicantPhoneNumber.Insert(3, "-"),
					Type = ContactType.MobilePhone,
					Date = DateTime.Now
				});
			}
			if (clientRequest.ApplicantPhoneNumber != null) {
				client.Contacts.Add(new Contact() {
					Client = client,
					ContactString = clientRequest.Email,
					Type = ContactType.Email,
					Date = DateTime.Now
				});
			}
			// дата изменений
			client.StatusChangedOn = DateTime.Now;
			// список типов документа
			var certificateTypeDic = new Dictionary<int, CertificateType>();
			certificateTypeDic.Add(0, CertificateType.Passport);
			certificateTypeDic.Add(1, CertificateType.Other);
			client.PhysicalClient.CertificateType = CertificateType.Passport;

			// Проверка адреса из заявки клиента по введенным им значениям 
			var currentRegion = DbSession.Query<Region>().FirstOrDefault(s => s.Name.ToLower() == clientRequest.City.ToLower());
			var currentStreet = DbSession.Query<Street>().FirstOrDefault(s => s.Name.ToLower().Trim() == clientRequest.Street.ToLower().Trim());
			var tempHouseNumber = (clientRequest.HouseNumber != null ? clientRequest.HouseNumber + clientRequest.Housing : "");
			var houseToFind = DbSession.Query<House>().FirstOrDefault(s => s.Number == tempHouseNumber
			                                                               && (s.Region == currentRegion || s.Street == currentStreet && s.Region == null));

			if (houseToFind == null) {
				houseToFind = new House();
				// Проверка адреса из заявки клиента по яндексу 
				var tempYandexAddress = GetAddressByYandexData(clientRequest);
				if (tempYandexAddress != null && tempYandexAddress.House != null) {
					clientRequest.Address = tempYandexAddress;
					clientRequest.Address.IsCorrectAddress = true;
					clientRequest.Address.Floor = clientRequest.Floor;
					clientRequest.Address.Entrance = clientRequest.Entrance.ToString();
					clientRequest.Address.Apartment = clientRequest.Apartment.ToString();
				}
			}
			else {
				// формирование адреса
				client.PhysicalClient.Address = new Address() {
					House = houseToFind,
					Floor = clientRequest.Floor,
					Entrance = clientRequest.Entrance.ToString(),
					Apartment = clientRequest.Apartment.ToString()
				};
			}
			// TODO: учесть принадлежность домов к региону
			// списки улиц и домов
			var currentStreetList = currentStreet == null || currentRegion == null ? new List<Street>() : DbSession.Query<Street>()
				.Where(s => s.Region.Id == currentRegion.Id || s.Houses.Any(a => a.Region.Id == currentRegion.Id)).OrderBy(s => s.Name).ToList();
			var currentHouseList = currentStreet == null || currentRegion == null ? new List<House>() : DbSession.Query<House>().Where(s => (s.Region == null || currentRegion.Id == s.Region.Id) &&
			                                                                                                                                ((s.Street.Region.Id == currentRegion.Id && s.Street.Id == currentStreet.Id) || (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id)) &&
			                                                                                                                                (s.Street.Region.Id == currentRegion.Id && s.Region == null || (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id))).OrderBy(s => s.Number).ToList();
			// тариф по запросу
			client.PhysicalClient.Plan = clientRequest.Plan;
			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			var planList = DbSession.Query<Plan>().OrderBy(s => s.Name).ToList();
			if (regionList.Count > 0) {
				planList = planList.Where(s => s.IsArchived == false && s.RegionPlans.Any(d => d.Region == currentRegion)).OrderBy(s => s.Name).ToList();
			}
			//
			ViewBag.CurrentRegion = currentRegion;
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentHouse = houseToFind;
			ViewBag.UserRequestStreet = clientRequest.Street;
			ViewBag.UserRequestHouse = clientRequest.HouseNumber;
			ViewBag.UserRequestHousing = clientRequest.Housing;
			ViewBag.CurrentStreetList = currentStreetList;
			ViewBag.CurrentHouseList = currentHouseList;
			ViewBag.RegionList = regionList;
			ViewBag.PlanList = planList;
			ViewBag.RedirectToCard = true;
			ViewBag.CertificateTypeDic = certificateTypeDic;
			ViewBag.ScapeUserNameDoubling = true;
			ViewBag.requestId = id;
			ViewBag.Client = client;

			return View();
		}

		/// <summary>
		///  Форма регистрации клиента по заявке POST
		/// </summary> 
		[HttpPost]
		public ActionResult RequestRegistration([EntityBinder] Client client, int requestId, bool redirectToCard, bool scapeUserNameDoubling = false)
		{
			// удаление неиспользованного контакта *иначе в БД лишняя запись
			client.Contacts = client.Contacts.Where(s => s.ContactString != string.Empty).ToList();
			// указываем статус
			client.Status = Inforoom2.Models.Status.Get(StatusType.BlockedAndNoConnected, DbSession);
			client.WhoRegistered = GetCurrentEmployee();
			// добавление клиента
			var errors = ValidationRunner.ValidateDeep(client);
			if (!scapeUserNameDoubling) {
				// Принудительная валидация, проверка дублирования ФИО
				var scapeNameDoubling = new Inforoom2.validators.ValidatorPhysicalClient();
				ViewBag.ValidatorFullNameOriginal = scapeNameDoubling;
				errors = ValidationRunner.ForcedValidationByAttribute(
					client, client.GetType().GetProperty("PhysicalClient"), scapeNameDoubling, false, errors);
			}
			// убираем из списка ошибок те, которые допустимы в данном случае
			errors.RemoveErrors(new List<string>() {
				"Inforoom2.Models.PhysicalClient.PassportDate",
				"Inforoom2.Models.PhysicalClient.CertificateName"
			});
			// получаем заявку
			ClientRequest clientRequest = DbSession.Query<ClientRequest>().First(s => s.Id == requestId);
			// список типов документа
			var certificateTypeDic = new Dictionary<int, CertificateType>();
			certificateTypeDic.Add(0, CertificateType.Passport);
			certificateTypeDic.Add(1, CertificateType.Other);

			// если ошибок нет
			if (errors.Length == 0) {
				// указываем имя лица, которое проводит регистрирацию
				client.WhoRegisteredName = client.WhoRegistered.Name;
				// генерируем пароль и его хыш сохраняем в модель физ.клиента
				PhysicalClient.GeneratePassword(client.PhysicalClient);
				// указываем полное имя клиента
				client._Name = client.PhysicalClient.FullName;
				// добавляем клиенту стандартные сервисы 
				var services = DbSession.Query<Service>().Where(s => s.Name == "IpTv" || s.Name == "Internet").ToList();
				IList<ClientService> csList = services.Select(service => new ClientService {
					Service = service,
					Client = client,
					BeginDate = DateTime.Now,
					IsActivated = false,
					ActivatedByUser = (service.Name == "Internet")
				}).ToList();
				client.ClientServices = csList;
				// дублируем моб.номер клиента в смс рассылку
				var mobilePhone = client.Contacts.FirstOrDefault(s => s.Type == ContactType.MobilePhone);
				if (mobilePhone != null) {
					mobilePhone.Type = ContactType.SmsSending;
					client.Contacts.Add(mobilePhone);
				}
				// сохраняем модель
				DbSession.Save(client);
				// Обновление заявки
				if (clientRequest != null) {
					// привязка текущего клиента к поданой им заявке
					// отправление запроса на регистрацию в архив
					clientRequest.Client = client;
					clientRequest.Archived = true;
					DbSession.Save(clientRequest);
				}
				// предварительно вызывая процедуру (старой админки) которая делает необходимые поправки в записях клиента и физ.клиента
				// переходим к карте клиента *в старой админке, если выбран пункт "Показывать наряд на подключение"
				if (redirectToCard) {
					return Redirect(System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
					                "Clients/UpdateAddressByClient?clientId=" + client.Id +
					                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
					                "UserInfo/PassAndShowCard?ClientID=" + client.Id);
				}
				// переходим к информации о клиенте *в старой админке
				return Redirect(System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
				                "Clients/UpdateAddressByClient?clientId=" + client.Id +
				                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
				                "UserInfo/ShowPhysicalClient?filter.ClientCode=" + client.Id);
			}
			// адресные данные по запросу 
			Street currentStreet = null;
			House currentHouse = null;
			Region currentRegion = null;

			var currentStreetList = new List<Street>();
			var currentHouseList = new List<House>();

			// пустой список тарифов
			var planList = new List<Plan>();
			// если дом существует, на его основе создать адрес
			if (client.Address.House != null) {
				currentRegion = client.Address.House.Region;
				currentStreet = client.Address.House.Street;
				currentHouse = client.Address.House;
				if (currentRegion == null) {
					currentRegion = client.Address.House.Street.Region;
				}
				client.PhysicalClient.Address = new Address() {
					House = currentHouse,
					Floor = client.Address.Floor,
					Entrance = client.Address.Entrance,
					Apartment = client.Address.Apartment
				};
				// списки улиц и домов
				currentStreetList = currentStreet == null || currentRegion == null ? new List<Street>() : DbSession.Query<Street>()
					.Where(s => s.Region.Id == currentRegion.Id || s.Houses.Any(a => a.Region.Id == currentRegion.Id)).OrderBy(s => s.Name).ToList();
				currentHouseList = DbSession.Query<House>().Where(s => (s.Region == null || currentRegion.Id == s.Region.Id) &&
				                                                       ((s.Street.Region.Id == currentRegion.Id && s.Street.Id == currentStreet.Id) || (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id)) &&
				                                                       (s.Street.Region.Id == currentRegion.Id && s.Region == null || (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id))).OrderBy(s => s.Number).ToList();
			}
			else {
				//если адрес пустой создаем новый дом ( not null )
				client.PhysicalClient.Address = new Address() {
					House = new House(),
					Floor = 0,
					Entrance = "",
					Apartment = ""
				};
			}
			// получаем списки регионов и тарифов по выбранному выбранному региону (городу) 
			var regionList = DbSession.Query<Region>().ToList();
			if (currentRegion != null) {
				planList = DbSession.Query<Plan>().Where(s => s.IsArchived == false && s.RegionPlans.Any(d => d.Region == (currentRegion))).OrderBy(s => s.Name).ToList();
			}
			//
			ViewBag.CurrentRegion = currentRegion;
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentHouse = currentHouse;
			ViewBag.UserRequestStreet = clientRequest.Street;
			ViewBag.UserRequestHouse = clientRequest.HouseNumber;
			ViewBag.UserRequestHousing = clientRequest.Housing;
			ViewBag.RegionList = regionList;
			ViewBag.CurrentStreetList = currentStreetList;
			ViewBag.CurrentHouseList = currentHouseList;
			ViewBag.PlanList = planList;
			ViewBag.CertificateTypeDic = certificateTypeDic;
			ViewBag.RedirectToCard = redirectToCard;
			ViewBag.ScapeUserNameDoubling = scapeUserNameDoubling;
			ViewBag.requestId = requestId;
			ViewBag.Client = client;

			return View();
		}

		/// <summary>
		/// Форма регистрации клиента 
		/// </summary> 
		public ActionResult Registration()
		{
			// Создание клиента
			var client = new Client();
			// Создание физ.клиента
			client.PhysicalClient = new PhysicalClient();
			// заполнение основнеых полей
			client.PhysicalClient.Name = "";
			client.PhysicalClient.Surname = "";
			client.PhysicalClient.Patronymic = "";
			client.PhysicalClient.CertificateType = 0;
			client.PhysicalClient.CertificateType = CertificateType.Passport;
			client.PhysicalClient.Address = new Address() {
				House = new House(),
				Floor = 0,
				Entrance = "",
				Apartment = ""
			};
			//	client.Contacts.Add();

			// список типов документа
			var certificateTypeDic = new Dictionary<int, CertificateType>();
			certificateTypeDic.Add(0, CertificateType.Passport);
			certificateTypeDic.Add(1, CertificateType.Other);
			ViewBag.CertificateTypeDic = certificateTypeDic;

			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();

			ViewBag.CurrentStreet = null;
			ViewBag.CurrentHouse = null;
			// получаем всех диллеров (работников)
			ViewBag.Dealers = DbSession.Query<Dealer>().Select(s => s.Employee).OrderBy(s => s.Name).ToList();
			ViewBag.RegionList = regionList;
			ViewBag.CurrentStreetList = new List<Street>();
			ViewBag.CurrentHouseList = new List<House>();
			ViewBag.PlanList = new List<Plan>();
			ViewBag.RedirectToCard = true;
			ViewBag.ScapeUserNameDoubling = true;
			ViewBag.Client = client;
			return View();
		}

		/// <summary>
		///  Форма регистрации клиента POST
		/// </summary> 
		[HttpPost]
		public ActionResult Registration([EntityBinder] Client client, bool redirectToCard, bool scapeUserNameDoubling = false)
		{
			// удаление неиспользованного контакта *иначе в БД лишняя запись  
			client.Contacts = client.Contacts.Where(s => s.ContactString != string.Empty).ToList();
			// указываем статус
			client.Status = Status.Get(StatusType.BlockedAndNoConnected, DbSession);
			// указываем, кто проводит регистрирацию
			client.WhoRegistered = GetCurrentEmployee();

			// проводим валидацию модели клиента
			var errors = ValidationRunner.ValidateDeep(client);
			if (!scapeUserNameDoubling) // Принудительная валидация, проверка дублирования ФИО
			{
				var scapeNameDoubling = new Inforoom2.validators.ValidatorPhysicalClient();
				ViewBag.ValidatorFullNameOriginal = scapeNameDoubling;
				errors = ValidationRunner.ForcedValidationByAttribute(
					client, client.GetType().GetProperty("PhysicalClient"), scapeNameDoubling, false, errors);
			}
			// убираем из списка ошибок те, которые допустимы в данном случае
			errors.RemoveErrors(new List<string>() {
				"Inforoom2.Models.PhysicalClient.PassportDate",
				"Inforoom2.Models.PhysicalClient.CertificateName"
			});
			// если нет ошибок и регистрирующее лицо указано
			if (errors.Length == 0 && client.Dealer != null) {
				// указываем имя лица, которое проводит регистрирацию
				client.WhoRegisteredName = client.WhoRegistered.Name;
				// генерируем пароль и его хыш сохраняем в модель физ.клиента
				PhysicalClient.GeneratePassword(client.PhysicalClient);
				// указываем полное имя клиента
				client._Name = client.PhysicalClient.FullName;
				// добавляем клиенту стандартные сервисы 
				var services = DbSession.Query<Service>().Where(s => s.Name == "IpTv" || s.Name == "Internet").ToList();
				IList<ClientService> csList = services.Select(service => new ClientService {
					Service = service,
					Client = client,
					BeginDate = DateTime.Now,
					IsActivated = false,
					ActivatedByUser = (service.Name == "Internet")
				}).ToList();
				client.ClientServices = csList;
				// дублируем моб.номер клиента в смс рассылку
				var mobilePhone = client.Contacts.FirstOrDefault(s => s.Type == ContactType.MobilePhone);
				if (mobilePhone != null) {
					mobilePhone.Type = ContactType.SmsSending;
					client.Contacts.Add(mobilePhone);
				}
				// сохраняем модель
				DbSession.Save(client);
				//@Todo раскомментировать когда закончится интеграция со старой админкой

				//SuccessMessage("Клиент успешно зарегистрирован!"); 

				// предварительно вызывая процедуру (старой админки) которая делает необходимые поправки в записях клиента и физ.клиента
				// переходим к карте клиента *в старой админке, если выбран пункт "Показывать наряд на подключение"
				if (redirectToCard) {
					return Redirect(System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
					                "Clients/UpdateAddressByClient?clientId=" + client.Id +
					                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
					                "UserInfo/PassAndShowCard?ClientID=" + client.Id);
				}
				// иначе переходим к информации о клиенте *в старой админке
				return Redirect(System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
				                "Clients/UpdateAddressByClient?clientId=" + client.Id +
				                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
				                "UserInfo/ShowPhysicalClient?filter.ClientCode=" + client.Id);
			}
			// заполняем список типов документа
			var CertificateTypeDic = new Dictionary<int, CertificateType>();
			CertificateTypeDic.Add(0, CertificateType.Passport);
			CertificateTypeDic.Add(1, CertificateType.Other);

			Street currentStreet = null;
			House currentHouse = null;
			Region currentRegion = null;
			if (client.Address != null && client.Address.House != null) {
				currentHouse = client.Address.House;
				currentStreet = client.Address.House.Street;
				currentRegion = client.Address.House.Region;
				if (currentRegion == null) {
					currentRegion = client.Address.House.Street.Region;
				}
			}
			var planList = new List<Plan>();
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			if (currentRegion != null) {
				planList = DbSession.Query<Plan>().Where(s => s.IsArchived == false && s.RegionPlans.Any(d => d.Region == (currentRegion))).OrderBy(s => s.Name).ToList();
			}
			// списки улиц и домов 
			var currentStreetList = currentStreet == null || currentRegion == null ? new List<Street>() : DbSession.Query<Street>()
				.Where(s => s.Region.Id == currentRegion.Id || s.Houses.Any(a => a.Region.Id == currentRegion.Id)).OrderBy(s => s.Name).ToList();
			var currentHouseList = currentStreet == null || currentRegion == null ? new List<House>() : DbSession.Query<House>().Where(s => (s.Region == null || currentRegion.Id == s.Region.Id) &&
			                                                                                                                                ((s.Street.Region.Id == currentRegion.Id && s.Street.Id == currentStreet.Id) || (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id)) &&
			                                                                                                                                (s.Street.Region.Id == currentRegion.Id && s.Region == null || (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id))).OrderBy(s => s.Number).ToList();
			ViewBag.CurrentRegion = currentRegion;
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentHouse = currentHouse;

			ViewBag.RegionList = regionList;
			ViewBag.CurrentStreetList = currentStreetList;
			ViewBag.CurrentHouseList = currentHouseList;
			ViewBag.PlanList = planList;
			ViewBag.ScapeUserNameDoubling = scapeUserNameDoubling;

			// получаем всех диллеров (работников)
			ViewBag.Dealers = DbSession.Query<Dealer>().Select(s => s.Employee).OrderBy(s => s.Name).ToList();
			ViewBag.CertificateTypeDic = CertificateTypeDic;
			ViewBag.RedirectToCard = redirectToCard;
			ViewBag.Client = client;

			return View();
		}

		/// <summary>
		/// Форма редактирования клиента 
		/// </summary> 
		/// <param name="id"></param>
		/// <returns></returns>
		public ActionResult Edit(int id)
		{
			// получаем клиента
			var client = DbSession.Query<Client>().First(s => s.Id == id);

			// список типов документа
			var certificateTypeDic = new Dictionary<int, CertificateType>();
			certificateTypeDic.Add(0, CertificateType.Passport);
			certificateTypeDic.Add(1, CertificateType.Other);

			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			Street currentStreet = null;
			House currentHouse = null;
			Region currentRegion = null;
			if (client.Address != null && client.Address.House != null) {
				currentHouse = client.Address.House;
				currentStreet = client.Address.House.Street;
				currentRegion = client.Address.House.Region;
				if (currentRegion == null) {
					currentRegion = client.Address.House.Street.Region;
				}
			}
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			// списки улиц и домов 
			var currentStreetList = currentStreet == null || currentRegion == null ? new List<Street>() : DbSession.Query<Street>()
				.Where(s => s.Region.Id == currentRegion.Id || s.Houses.Any(a => a.Region.Id == currentRegion.Id)).OrderBy(s => s.Name).ToList();
			var currentHouseList = currentStreet == null || currentRegion == null ? new List<House>() : DbSession.Query<House>().Where(s => (s.Region == null || currentRegion.Id == s.Region.Id) &&
			                                                                                                                                ((s.Street.Region.Id == currentRegion.Id && s.Street.Id == currentStreet.Id) || (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id)) &&
			                                                                                                                                (s.Street.Region.Id == currentRegion.Id && s.Region == null || (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id))).OrderBy(s => s.Number).ToList();
			ViewBag.CurrentRegion = currentRegion;
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentHouse = currentHouse;

			ViewBag.RegionList = regionList;
			ViewBag.CurrentStreetList = currentStreetList;
			ViewBag.CurrentHouseList = currentHouseList;

			ViewBag.CertificateTypeDic = certificateTypeDic;
			ViewBag.Client = client;

			return View();
		}

		/// <summary>
		///  Форма редактирования клиента POST
		/// </summary>
		/// <param name="ClientRegistration"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Edit([EntityBinder] Client client)
		{
			var errors = ValidationRunner.ValidateDeep(client);
			errors.RemoveErrors(new List<string>() {
				"Inforoom2.Models.PhysicalClient.PassportDate",
				"Inforoom2.Models.PhysicalClient.CertificateName"
			});
			if (errors.Length == 0) {
				// сохраняем модель
				DbSession.Update(client);

				//@Todo раскомментировать когда закончится интеграция со старой админкой 
				//SuccessMessage("Клиент успешно изменен!");  

				return Redirect(System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
				                "Clients/UpdateAddressByClient?clientId=" + client.Id +
				                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
				                "UserInfo/ShowPhysicalClient?filter.ClientCode=" + client.Id);
			}
			else {
				DbSession.Clear();
			}
			// список типов документа
			var certificateTypeDic = new Dictionary<int, CertificateType>();
			certificateTypeDic.Add(0, CertificateType.Passport);
			certificateTypeDic.Add(1, CertificateType.Other);
			ViewBag.CertificateTypeDic = certificateTypeDic;


			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			Street currentStreet = null;
			House currentHouse = null;
			Region currentRegion = null;
			var planList = new List<Plan>();
			if (client.Address != null && client.Address.House != null) {
				currentHouse = client.Address.House;
				currentStreet = client.Address.House.Street;
				currentRegion = client.Address.House.Region;
				if (currentRegion == null) {
					currentRegion = client.Address.House.Street.Region;
				}
			}
			var RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();

			// списки улиц и домов

			var currentStreetList = currentStreet == null || currentRegion == null ? new List<Street>() : DbSession.Query<Street>()
				.Where(s => s.Region.Id == currentRegion.Id || s.Houses.Any(a => a.Region.Id == currentRegion.Id)).OrderBy(s => s.Name).ToList();
			var currentHouseList = currentStreet == null || currentRegion == null ? new List<House>() : DbSession.Query<House>().Where(s => (s.Region == null || currentRegion.Id == s.Region.Id) &&
			                                                                                                                                ((s.Street.Region.Id == currentRegion.Id && s.Street.Id == currentStreet.Id) || (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id)) &&
			                                                                                                                                (s.Street.Region.Id == currentRegion.Id && s.Region == null || (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id))).OrderBy(s => s.Number).ToList();

			ViewBag.CurrentRegion = currentRegion;
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentHouse = currentHouse;

			ViewBag.RegionList = RegionList;
			ViewBag.CurrentStreetList = currentStreetList;
			ViewBag.CurrentHouseList = currentHouseList;
			ViewBag.PlanList = planList;

			ViewBag.Client = client;
			return View();
		}

		public ActionResult DealerList()
		{
			var dealer = DbSession.QueryOver<Dealer>().List();
			dealer = dealer.OrderBy(s => s.Employee.Name).ToList();
			var employee = DbSession.QueryOver<Employee>().List();
			ViewBag.DealerList = dealer;
			ViewBag.EmployeeList = employee.Where(s => !dealer.Any(c => c.Employee == s)).OrderBy(s => s.Name).ToList();
			ViewBag.DealerMan = new Dealer();
			return View("DealerList");
		}

		public ActionResult DealerAdd([EntityBinder] Dealer dealer)
		{
			var employee = DbSession.Query<Employee>().FirstOrDefault(s => s == dealer.Employee);
			var existedDealer = DbSession.Query<Dealer>().FirstOrDefault(s => s.Employee == dealer.Employee);
			if (employee != null && existedDealer == null) {
				DbSession.Save(dealer);
				SuccessMessage("Дилер успешно добавлен");
			}
			return RedirectToAction("DealerList");
		}

		public ActionResult DealerDelete(int id)
		{
			var dealer = DbSession.Query<Dealer>().FirstOrDefault(s => s.Id == id);
			if (dealer != null) {
				DbSession.Delete(dealer);
			}
			return RedirectToAction("DealerList");
		}
	}
}