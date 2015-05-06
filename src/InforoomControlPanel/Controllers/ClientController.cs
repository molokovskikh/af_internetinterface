﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate.Linq;
using Client = Inforoom2.Models.Client;
using ClientService = Inforoom2.Models.ClientService;
using Contact = Inforoom2.Models.Contact;
using House = Inforoom2.Models.House;
using PhysicalClient = Inforoom2.Models.PhysicalClient;
using RequestType = Inforoom2.Models.RequestType;
using ServiceRequest = Inforoom2.Models.ServiceRequest;
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
			var pager = new ModelFilter<Client>(this);
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

			if (client.Status != null && client.Status.Type == StatusType.BlockedAndConnected) {
				// Find Switches
				var networkNodeList = DbSession.QueryOver<SwitchAddress>().Where(s =>
					s.House == client.PhysicalClient.Address.House && s.Entrance.ToString() == client.PhysicalClient.Address.Entrance ||
					s.House == client.PhysicalClient.Address.House && s.Entrance == null).List();

				if (networkNodeList.Count > 0) {
					ViewBag.NetworkNodeList = networkNodeList; //.NetworkNode.Switches.ToList(); 
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
			var tariff = InitRequestPlans().FirstOrDefault(k => k.Id == clientRequest.Plan.Id);
			clientRequest.Plan = tariff;
			clientRequest.ActionDate = clientRequest.RegDate = DateTime.Now;
			Employee reqAuthor = null;
			if (clientRequest.RequestSource != RequestType.FromClient) {
				reqAuthor = DbSession.Query<Employee>().ToList()
					.FirstOrDefault(e => e.Id == clientRequest.RequestAuthor.Id);
			}
			clientRequest.RequestAuthor = reqAuthor;

			var errors = ValidationRunner.ValidateDeep(clientRequest);
			if (errors.Length == 0 && clientRequest.IsContractAccepted) {
				clientRequest.Address = GetAddressByYandexData(clientRequest);
				DbSession.Save(clientRequest);
				SuccessMessage(string.Format("Спасибо, Ваша заявка создана. Номер заявки {0}", clientRequest.Id));
				return RedirectToAction("Request");
			}
			// Пока используется IsContractAccepted=true, закомментированные строки кода не нужны
			//if (!clientRequest.IsContractAccepted) {
			//	ErrorMessage("Пожалуйста, подтвердите, что Вы согласны с договором-офертой");
			//}
			ViewBag.IsRedirected = false;
			ViewBag.IsCityValidated = false;
			ViewBag.IsStreetValidated = false;
			ViewBag.IsHouseValidated = false;
			ViewBag.ClientRequest = clientRequest;
			ViewBag.Employees = DbSession.Query<Employee>().ToList().OrderBy(e => e.Name).ToList();
			return View();
		}

		private void InitClientRequest(Plan plan = null, string city = "", string street = "", string house = "")
		{
			ViewBag.IsRedirected = false;
			ViewBag.IsCityValidated = false;
			ViewBag.IsStreetValidated = false;
			ViewBag.IsHouseValidated = false;
			var clientRequest = new ClientRequest {
				IsContractAccepted = true,
				RequestAuthor = GetCurrentEmployee()
			};

			if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(street) && !string.IsNullOrEmpty(house)) {
				clientRequest.Street = street;
				clientRequest.City = city;
				ViewBag.IsCityValidated = true;
				ViewBag.IsStreetValidated = true;

				int housen;
				int.TryParse(house, out housen);
				if (housen != 0) {
					ViewBag.IsHouseValidated = true;
					clientRequest.HouseNumber = housen;
				}
			}

			if (plan != null) {
				clientRequest.Plan = plan;
				ViewBag.IsRedirected = true;
			}
			ViewBag.ClientRequest = clientRequest;
			ViewBag.Employees = DbSession.Query<Employee>().ToList().OrderBy(e => e.Name).ToList();
			InitRequestPlans();
		}

		private List<Plan> InitRequestPlans()
		{
			var plans = DbSession.Query<Plan>().Where(p => !p.IsArchived).ToList();
			ViewBag.Plans = plans;
			return plans;
		}

		protected Address GetAddressByYandexData(ClientRequest clientRequest)
		{
			var city = GetList<City>().FirstOrDefault(c => c.Name.Equals(clientRequest.YandexCity, StringComparison.InvariantCultureIgnoreCase));

			if (city == null || !clientRequest.IsYandexAddressValid()) {
				var badAddress = new Address { IsCorrectAddress = false };
				return badAddress;
			}
			var region = GetList<Region>().FirstOrDefault(r => r.City == city);

			var street = GetList<Street>().FirstOrDefault(s => s.Name.Equals(clientRequest.YandexStreet, StringComparison.InvariantCultureIgnoreCase)
			                                                   && s.Region.Equals(region));

			if (street == null) {
				street = new Street(clientRequest.YandexStreet);
			}

			var house = GetList<House>().FirstOrDefault(h => h.Number.Equals(clientRequest.YandexHouse, StringComparison.InvariantCultureIgnoreCase)
			                                                 && h.Street.Name.Equals(clientRequest.YandexStreet, StringComparison.InvariantCultureIgnoreCase)
			                                                 && h.Street.Region.Equals(region));

			if (house == null) {
				house = new House(clientRequest.YandexHouse);
			}
			var address = GetList<Address>().FirstOrDefault(a => a.IsCorrectAddress
			                                                     && a.House.Equals(house)
			                                                     && a.House.Street.Equals(street)
			                                                     && a.House.Street.Region.Equals(region)
			                                                     && a.Entrance == clientRequest.Entrance.ToString()
			                                                     && a.Floor == clientRequest.Floor
			                                                     && a.Apartment == clientRequest.Apartment.ToString());

			//if (address == null) {
			//	address = new Address();
			//	address.House = house;
			//	address.Apartment = clientRequest.Apartment;
			//	address.Floor = clientRequest.Floor;
			//	address.Entrance = clientRequest.Entrance;
			//	address.House.Street = street;
			//	address.House.Street.Region = region;
			//	address.IsCorrectAddress = true;
			//}
			return address;
		}

		/// <summary>
		/// Список заявок на подключение
		/// </summary>
		/// <param name="ClientRequest"></param>
		/// <returns></returns>
		public ActionResult RequestsList()
		{
			var pager = new ModelFilter<ClientRequest>(this);
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
			client.PhysicalClient.Name = fio.Length > 0 ? fio[0] : "";
			client.PhysicalClient.Surname = fio.Length > 1 ? fio[1] : "";
			client.PhysicalClient.Patronymic = fio.Length > 2 ? fio[2] : "";

			// контакты по запросу

			// Контакты находятся в отдельной таблице
			//client.PhoneNumber = clientRequest.ApplicantPhoneNumber;
			//client.Email = clientRequest.Email;

			// адресные данные по запросу
			var currentRegion = DbSession.Query<Region>().FirstOrDefault(s => s.Name.ToLower() == clientRequest.City.ToLower()) ?? new Region();
			var currentStreet = DbSession.Query<Street>().FirstOrDefault(s => s.Name.ToLower().Trim() == clientRequest.Street.ToLower().Trim() && s.Region == currentRegion);
			var houseToFind = DbSession.Query<House>().FirstOrDefault(s => s.Number == (clientRequest.HouseNumber ?? 0).ToString() && s.Street == currentStreet) ?? new House();
			client.PhysicalClient.Address = new Address() {
				House = houseToFind,
				Floor = clientRequest.Floor,
				Entrance = clientRequest.Entrance.ToString(),
				Apartment = clientRequest.Apartment.ToString()
			};
			// списки улиц и домов
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentStreetList = currentRegion == null ? new List<Street>() : DbSession.Query<Street>().Where(s => s.Region.Id == currentRegion.Id).OrderBy(s => s.Name).ToList();
			ViewBag.CurrentHouseList = currentStreet == null ? new List<House>() : DbSession.Query<House>().Where(s => s.Street.Id == currentStreet.Id).OrderBy(s => s.Number).ToList();

			// тариф по запросу
			client.PhysicalClient.Plan = clientRequest.Plan;

			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			var planList = DbSession.Query<Plan>().OrderBy(s => s.Name).ToList();
			if (regionList.Count > 0) {
				planList = planList.Where(s => s.RegionPlans.Any(d => d.Region == currentRegion)).OrderBy(s => s.Name).ToList();
			}
			ViewBag.CurrentRegion = currentRegion;
			ViewBag.RegionList = regionList;
			ViewBag.PlanList = planList;
			ViewBag.RedirectToCard = true;

			client.PhysicalClient.CertificateType = CertificateType.Passport;
			client.StatusChangedOn = DateTime.Now;

			// список типов документа
			var certificateTypeDic = new Dictionary<int, CertificateType>();
			certificateTypeDic.Add(0, CertificateType.Passport);
			certificateTypeDic.Add(1, CertificateType.Other);
			ViewBag.CertificateTypeDic = certificateTypeDic;


			ViewBag.requestId = id;
			ViewBag.Client = client;
			return View();
		}

		/// <summary>
		///  Форма регистрации клиента по заявке POST
		/// </summary> 
		[HttpPost]
		public ActionResult RequestRegistration([EntityBinder] Client client, int requestId, bool redirectToCard)
		{
			// удаление неиспользованного контакта *иначе в БД лишняя запись
			client.Contacts = client.Contacts.Where(s => s.ContactString != string.Empty).ToList();
			// указываем статус
			client.Status = Inforoom2.Models.Status.Get(StatusType.BlockedAndNoConnected, DbSession);
			client.WhoRegistered = GetCurrentEmployee();
			// добавление клиента
			var errors = ValidationRunner.ValidateDeep(client);
			// убираем из списка ошибок те, которые допустимы в данном случае
			errors.RemoveErrors(new List<string>() {
				"Inforoom2.Models.PhysicalClient.PassportDate",
				"Inforoom2.Models.PhysicalClient.CertificateName"
			});
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
				DbSession.Save(client);
				ClientRequest clientRequest = DbSession.Query<ClientRequest>().First(s => s.Id == requestId);
				// отправление запроса на регистрацию в архив
				if (clientRequest != null) {
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
			// пустой список тарифов
			var planList = new List<Plan>();

			if (client.Address.House != null) {
				currentRegion = client.Address.House.Street.Region;
				currentStreet = client.Address.House.Street;
				currentHouse = client.Address.House;
				client.PhysicalClient.Address = new Address() {
					House = currentHouse,
					Floor = client.Address.Floor,
					Entrance = client.Address.Entrance,
					Apartment = client.Address.Apartment
				};
				// списки улиц и домов
				ViewBag.CurrentStreetList = DbSession.Query<Street>().
					Where(s => s.Region.Id == currentStreet.Region.Id).OrderBy(s => s.Name).ToList();
				ViewBag.currentHouseList = DbSession.Query<House>().Where(s => s.Street.Id == currentStreet.Id).OrderBy(s => s.Number).ToList();
			}
			else {
				//если адрес пустой создаем новый дом ( not null )
				client.PhysicalClient.Address = new Address() {
					House = new House(),
					Floor = 0,
					Entrance = "",
					Apartment = ""
				};
				ViewBag.CurrentStreetList = new List<Street>();
				ViewBag.CurrentHouseList = new List<House>();
			}

			// список типов документа
			var certificateTypeDic = new Dictionary<int, CertificateType>();
			certificateTypeDic.Add(0, CertificateType.Passport);
			certificateTypeDic.Add(1, CertificateType.Other);
			ViewBag.CertificateTypeDic = certificateTypeDic;

			// получаем списки регионов и тарифов по выбранному выбранному региону (городу) 
			var regionList = DbSession.Query<Region>().ToList();
			if (currentRegion != null) {
				planList = DbSession.Query<Plan>().Where(s => s.RegionPlans.Any(d => d.Region == (currentRegion))).OrderBy(s => s.Name).ToList();
			}

			ViewBag.RedirectToCard = redirectToCard;

			ViewBag.CurrentRegion = currentRegion;
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentHouse = currentHouse;

			ViewBag.RegionList = regionList;
			ViewBag.PlanList = planList;

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
			ViewBag.Dealers = DbSession.Query<Dealer>().Select(s => s.Employee).ToList();
			ViewBag.RegionList = regionList;
			ViewBag.CurrentStreetList = new List<Street>();
			ViewBag.CurrentHouseList = new List<House>();
			ViewBag.PlanList = new List<Plan>();
			ViewBag.RedirectToCard = true;
			ViewBag.Client = client;
			return View();
		}

		/// <summary>
		///  Форма регистрации клиента POST
		/// </summary> 
		[HttpPost]
		public ActionResult Registration([EntityBinder] Client client, bool redirectToCard)
		{
			// удаление неиспользованного контакта *иначе в БД лишняя запись  
			client.Contacts = client.Contacts.Where(s => s.ContactString != string.Empty).ToList();
			// указываем статус
			client.Status = Status.Get(StatusType.BlockedAndNoConnected, DbSession);
			// указываем, кто проводит регистрирацию
			client.WhoRegistered = GetCurrentEmployee();

			// проводим валидацию модели клиента
			var errors = ValidationRunner.ValidateDeep(client);
			// убираем из списка ошибок те, которые допустимы в данном случае
			errors.RemoveErrors(new List<string>()
			{ "Inforoom2.Models.PhysicalClient.PassportDate", "Inforoom2.Models.PhysicalClient.CertificateName" });
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
					Service = service, Client = client,
					BeginDate = DateTime.Now,
					IsActivated = false,
					ActivatedByUser = (service.Name == "Internet")
				}).ToList();
				client.ClientServices = csList;
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
			ViewBag.CertificateTypeDic = CertificateTypeDic;


			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			Street currentStreet = null;
			House currentHouse = null;
			Region currentRegion = null;
			var planList = new List<Plan>();
			if (client.Address != null && client.Address.House != null) {
				currentHouse = client.Address.House;
				currentStreet = client.Address.House.Street;
				currentRegion = client.Address.House.Street.Region;
			}
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			if (currentRegion != null) {
				planList = DbSession.Query<Plan>().Where(s => s.RegionPlans.Any(d => d.Region == (currentRegion))).OrderBy(s => s.Name).ToList();
			}
			// списки улиц и домов
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentStreetList = currentRegion == null ? new List<Street>() : DbSession.Query<Street>().Where(s => s.Region.Id == currentRegion.Id).OrderBy(s => s.Name).ToList();
			ViewBag.CurrentHouseList = currentStreet == null ? new List<House>() : DbSession.Query<House>().Where(s => s.Street.Id == currentStreet.Id).OrderBy(s => s.Number).ToList();
			ViewBag.CurrentRegion = currentRegion;
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentHouse = currentHouse;
			// получаем всех диллеров (работников)
			ViewBag.Dealers = DbSession.Query<Dealer>().Select(s => s.Employee).OrderBy(s => s.Name).ToList();
			ViewBag.RegionList = regionList;
			ViewBag.PlanList = planList;
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
			var client = DbSession.Query<Client>().First(s => s.Id == id);

			// список типов документа
			var certificateTypeDic = new Dictionary<int, CertificateType>();
			certificateTypeDic.Add(0, CertificateType.Passport);
			certificateTypeDic.Add(1, CertificateType.Other);
			ViewBag.CertificateTypeDic = certificateTypeDic;

			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			Street currentStreet = null;
			House currentHouse = null;
			Region currentRegion = null;
			if (client.Address != null && client.Address.House != null) {
				currentHouse = client.Address.House;
				currentStreet = client.Address.House.Street;
				currentRegion = client.Address.House.Street.Region;
			}
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			// списки улиц и домов
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentStreetList = currentStreet == null ? new List<Street>() : DbSession.Query<Street>().Where(s => s.Region.Id == currentStreet.Region.Id).OrderBy(s => s.Name).ToList();
			ViewBag.CurrentHouseList = currentStreet == null ? new List<House>() : DbSession.Query<House>().Where(s => s.Street.Id == currentStreet.Id).OrderBy(s => s.Number).ToList();
			ViewBag.CurrentRegion = currentRegion;
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentHouse = currentHouse;
			ViewBag.RegionList = regionList;

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
				currentRegion = client.Address.House.Street.Region;
			}
			var RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			// списки улиц и домов
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentStreetList = currentStreet == null ? new List<Street>() : DbSession.Query<Street>().Where(s => s.Region.Id == currentRegion.Id).OrderBy(s => s.Name).ToList();
			ViewBag.CurrentHouseList = currentStreet == null ? new List<House>() : DbSession.Query<House>().Where(s => s.Street.Id == currentStreet.Id).OrderBy(s => s.Number).ToList();
			ViewBag.CurrentRegion = currentRegion;
			ViewBag.CurrentStreet = currentStreet;
			ViewBag.CurrentHouse = currentHouse;

			ViewBag.RegionList = RegionList;
			ViewBag.PlanList = planList;

			ViewBag.Client = client;
			return View();
		}
	}
}