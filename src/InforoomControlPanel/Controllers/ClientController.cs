using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using InternetInterface.Models;
using NHibernate.Linq;
using NHibernate.Util;
using Client = Inforoom2.Models.Client;
using Contact = Inforoom2.Models.Contact;
using House = Inforoom2.Models.House;
using PhysicalClient = Inforoom2.Models.PhysicalClient;
using RequestType = Inforoom2.Models.RequestType;
using ServiceRequest = Inforoom2.Models.ServiceRequest;
using StatusType = Inforoom2.Models.StatusType;
using Street = Inforoom2.Models.Street;

namespace InforoomControlPanel.Controllers
{
	public class ClientController : AdminController
	{
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.BreadCrumb = "Клиенты";
		}

		public ActionResult ClientList(int page = 1)
		{
			var perpage = 100;
			var clients = DbSession.Query<Client>().Where(i => i.PhysicalClient != null).Skip((page - 1) * perpage).Take(perpage).ToList();
			ViewBag.Clients = clients;
			//Пагинация
			ViewBag.Models = clients;
			ViewBag.Page = page;
			ViewBag.ModelsPerPage = perpage;
			ViewBag.ModelsCount = DbSession.QueryOver<Client>().Where(i => i.PhysicalClient != null).RowCount();
			return View("ClientList");
		}

		public ActionResult ClientInfo(int clientId)
		{
			// Find Client
			var Client = DbSession.Query<Client>().FirstOrDefault(i => i.PhysicalClient != null && i.Id == clientId);
			ViewBag.Client = Client;

			if (Client.Status != null && Client.Status.Type == StatusType.BlockedAndConnected) {
				// Find Switches
				var networkNodeList = DbSession.QueryOver<SwitchAddress>().Where(s =>
					s.House == Client.PhysicalClient.Address.House && s.Entrance.ToString() == Client.PhysicalClient.Address.Entrance ||
					s.House == Client.PhysicalClient.Address.House && s.Entrance == null).List();

				if (networkNodeList.Count > 0) {
					ViewBag.NetworkNodeList = networkNodeList; //.NetworkNode.Switches.ToList(); 
				}
			}
			return View();
		}

		[HttpPost]
		public ActionResult ClientInfo([EntityBinder] Client client)
		{
			DbSession.Update(client);
			return View();
		}

		/// <summary>
		/// Отображает форму новой заявки
		/// </summary>
		public ActionResult ClientRequest()
		{
			InitClientRequest();
			return View();
		}

		[HttpPost]
		public ActionResult ClientRequest([EntityBinder] ClientRequest clientRequest)
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
				return RedirectToAction("ClientRequest");
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
			var dealersList = DbSession.Query<Dealer>().Select(d => d.Employee).ToList();
			ViewBag.Employees = dealersList.OrderBy(e => e.Name).ToList();
			return View();
		}

		private void InitClientRequest(Plan plan = null, string city = "", string street = "", string house = "")
		{
			ViewBag.IsRedirected = false;
			ViewBag.IsCityValidated = false;
			ViewBag.IsStreetValidated = false;
			ViewBag.IsHouseValidated = false;
			var curDealer = DbSession.Query<Dealer>().Where(d => d.Employee == GetCurrentEmployee()).ToList();
			var clientRequest = new ClientRequest {
				IsContractAccepted = true,
				RequestAuthor = (curDealer.Count > 0) ? curDealer.FirstOrDefault().Employee : null
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
			var dealersList = DbSession.Query<Dealer>().Select(d => d.Employee).ToList();
			ViewBag.Employees = dealersList.OrderBy(e => e.Name).ToList();
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
		public ActionResult ClientRequestsList()
		{
			var clientRequests = DbSession.Query<ClientRequest>().OrderByDescending(i => i.Id).ToList();
			ViewBag.ClientRequests = clientRequests;
			return View();
		}

		/// <summary>
		/// Создание заявки на подключение
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		public ActionResult ServiceRequest(int clientId)
		{
			var client = DbSession.Get<Client>(clientId);
			var ServiceRequest = new ServiceRequest(client);
			var servicemen = DbSession.Query<ServiceMan>().ToList();
			ViewBag.Client = client;
			ViewBag.ServiceRequest = ServiceRequest;
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
		/// <param name="requestId">Id заявки</param>
		/// <returns></returns>
		public ActionResult ClientRequestRegistration(int requestId)
		{
			// Создаем клиента
			var client = new Client();
			// запрос клиента
			ClientRequest clientRequest = DbSession.Query<ClientRequest>().First(s => s.Id == requestId);
			// Создаем физ.клиента
			client.PhysicalClient = new PhysicalClient();
			// ФИО по запросу
			string[] fio = clientRequest.ApplicantName.Trim().Split(' ');
			client.PhysicalClient.Name = fio.Length > 0 ? fio[0] : "";
			client.PhysicalClient.Surname = fio.Length > 1 ? fio[1] : "";
			client.PhysicalClient.Patronymic = fio.Length > 2 ? fio[2] : "";

			// контакты по запросу
			client.PhoneNumber = clientRequest.ApplicantPhoneNumber;
			client.Email = clientRequest.Email;

			// адресные данные по запросу
			var currentRegion = DbSession.Query<Region>().FirstOrDefault(s => s.Name.ToLower() == clientRequest.City.ToLower()) ?? new Region();
			var currentStreet = DbSession.Query<Street>().FirstOrDefault(s => s.Name.ToLower().Trim() == clientRequest.Street.ToLower().Trim() && s.Region == currentRegion);
			var houseToFind = DbSession.Query<House>().FirstOrDefault(s => s.Number == (clientRequest.HouseNumber ?? 0).ToString() && s.Street == currentStreet) ?? new House();
			client.PhysicalClient.Address = new Address() {
				House = houseToFind,
				Floor = clientRequest.Floor,
				Entrance = clientRequest.Entrance,
				Apartment = clientRequest.Apartment
			};
			// списки улиц и домов
			ViewBag.currentStreet = currentStreet ?? new Street();
			ViewBag.curStreetList = currentStreet == null ? new List<Street>() : DbSession.Query<Street>().Where(s => s.Region.Id == currentStreet.Region.Id).ToList();
			ViewBag.curHouseList = currentStreet == null ? new List<House>() : DbSession.Query<House>().Where(s => s.Street.Id == currentStreet.Id).ToList();

			// тариф по запросу
			client.PhysicalClient.Plan = clientRequest.Plan;

			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			var RegionList = DbSession.Query<Region>().ToList();
			var PlanList = DbSession.Query<Plan>().ToList();
			if (RegionList.Count > 0) {
				PlanList = PlanList.Where(s => s.RegionPlans.Any(d => d.Region == currentRegion)).ToList();
			}
			ViewBag.currentRegion = currentRegion;
			ViewBag.RegionList = RegionList;
			ViewBag.PlanList = PlanList;

			client.PhysicalClient.CertificateType = CertificateType.Passport;
			client.StatusChangedOn = DateTime.Now;
			client.Contacts = new List<Contact>() {
				new Contact() { ContactName = "", ContactString = "", Type = ContactType.HousePhone, Date = DateTime.Now },
				new Contact() { ContactName = "", ContactString = "", Type = ContactType.ConnectedPhone, Date = DateTime.Now }
			};
			// список типов документа
			var CertificateTypeDic = new Dictionary<int, CertificateType>();
			CertificateTypeDic.Add(0, CertificateType.Passport);
			CertificateTypeDic.Add(1, CertificateType.Other);
			ViewBag.CertificateTypeDic = CertificateTypeDic;


			ViewBag.requestId = requestId;
			ViewBag.Client = client;
			return View();
		}

		/// <summary>
		///  Форма регистрации клиента по заявке POST
		/// </summary>
		/// <param name="ClientRegistration"></param>y
		/// <returns></returns>
		[HttpPost]
		public ActionResult ClientRequestRegistration([EntityBinder] Client client, int requestId)
		{
			// добавление клиента
			var errors = ValidationRunner.ValidateDeep(client);
			if (errors.Length == 0) {
				DbSession.Save(client);
				ClientRequest clientRequest = DbSession.Query<ClientRequest>().First(s => s.Id == requestId);
				// отправление запроса на регистрацию в архив
				if (clientRequest != null) {
					clientRequest.Archived = true;
					DbSession.Save(clientRequest);
				}
				SuccessMessage("Клиент успешно зарегистрирован!");
				return RedirectToAction("ClientList");
			}

			// адресные данные по запросу 
			if (client.Address.House != null) {
				var currentCity = client.Address.House.Street.Region.City;
				var currentStreet = client.Address.House.Street;
				var houseToFind = client.Address.House;
				client.PhysicalClient.Address = new Address() {
					House = houseToFind,
					Floor = client.Address.Floor,
					Entrance = client.Address.Entrance,
					Apartment = client.Address.Apartment
				};
				// списки улиц и домов
				ViewBag.currentStreet = currentStreet;
				ViewBag.curStreetList = DbSession.Query<Street>().
					Where(s => s.Region.Id == currentStreet.Region.Id).ToList();
				ViewBag.curHouseList = DbSession.Query<House>().Where(s => s.Street.Id == currentStreet.Id).ToList();
			}
			else {
				client.PhysicalClient.Address = new Address() {
					House = new House(),
					Floor = 0,
					Entrance = 0,
					Apartment = 0
				};
				ViewBag.currentStreet = new Street();
				ViewBag.curStreetList = new List<Street>();
				ViewBag.curHouseList = new List<House>();
			}

			// дополнительные контакты
			client.Contacts = new List<Contact>() {
				new Contact() { ContactName = "", ContactString = "", Type = ContactType.HousePhone, Date = DateTime.Now },
				new Contact() { ContactName = "", ContactString = "", Type = ContactType.ConnectedPhone, Date = DateTime.Now }
			};

			// список типов документа
			var CertificateTypeDic = new Dictionary<int, CertificateType>();
			CertificateTypeDic.Add(0, CertificateType.Passport);
			CertificateTypeDic.Add(1, CertificateType.Other);
			ViewBag.CertificateTypeDic = CertificateTypeDic;

			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			var RegionList = DbSession.Query<Region>().ToList();
			var PlanList = DbSession.Query<Plan>().ToList();
			if (RegionList.Count > 0) {
				PlanList = PlanList.Where(s => s.RegionPlans.Any(d => d.Region == RegionList[0])).ToList();
			}
			ViewBag.RegionList = RegionList;
			ViewBag.PlanList = PlanList;

			ViewBag.requestId = requestId;
			ViewBag.Client = client;
			return View();
		}

		/// <summary>
		/// Форма регистрации клиента 
		/// </summary>
		/// <param name="ClientRegistration"></param>
		/// <returns></returns>
		public ActionResult ClientRegistration()
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
				Entrance = 0,
				Apartment = 0
			};
			client.StatusChangedOn = DateTime.Now;
			client.Contacts = new List<Contact>() {
				new Contact() { ContactName = "", ContactString = "", Type = ContactType.HousePhone, Date = DateTime.Now },
				new Contact() { ContactName = "", ContactString = "", Type = ContactType.ConnectedPhone, Date = DateTime.Now }
			};
			// список типов документа
			var CertificateTypeDic = new Dictionary<int, CertificateType>();
			CertificateTypeDic.Add(0, CertificateType.Passport);
			CertificateTypeDic.Add(1, CertificateType.Other);
			ViewBag.CertificateTypeDic = CertificateTypeDic;

			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			var RegionList = DbSession.Query<Region>().ToList();
			var PlanList = DbSession.Query<Plan>().ToList();
			if (RegionList.Count > 0) {
				PlanList = PlanList.Where(s => s.RegionPlans.Any(d => d.Region == RegionList[0])).ToList();
			}
			ViewBag.RegionList = RegionList;
			ViewBag.PlanList = PlanList;

			ViewBag.Client = client;
			return View();
		}

		/// <summary>
		///  Форма регистрации клиента POST
		/// </summary>
		/// <param name="ClientRegistration"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult ClientRegistration([EntityBinder] Client client)
		{
			var errors = ValidationRunner.ValidateDeep(client);
			if (errors.Length == 0) {
				DbSession.Save(client);
				SuccessMessage("Клиент успешно зарегистрирован!");
				return RedirectToAction("ClientList");
			}
			client.Contacts = new List<Contact>() {
				new Contact() { ContactName = "", ContactString = "", Type = ContactType.HousePhone, Date = DateTime.Now },
				new Contact() { ContactName = "", ContactString = "", Type = ContactType.ConnectedPhone, Date = DateTime.Now }
			};

			// список типов документа
			var CertificateTypeDic = new Dictionary<int, CertificateType>();
			CertificateTypeDic.Add(0, CertificateType.Passport);
			CertificateTypeDic.Add(1, CertificateType.Other);
			ViewBag.CertificateTypeDic = CertificateTypeDic;

			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			var RegionList = DbSession.Query<Region>().ToList();
			var PlanList = DbSession.Query<Plan>().ToList();
			if (RegionList.Count > 0) {
				PlanList = PlanList.Where(s => s.RegionPlans.Any(d => d.Region == RegionList[0])).ToList();
			}

			ViewBag.RegionList = RegionList;
			ViewBag.PlanList = PlanList;

			ViewBag.Client = client;
			return View();
		}

		/// <summary>
		/// Форма редактирования клиента 
		/// </summary>
		/// <param name="ClientRegistration"></param>
		/// <returns></returns>
		public ActionResult ClientEditor(int clientId)
		{
			var client = DbSession.Query<Client>().First(s => s.Id == clientId);
			var CertificateTypeDic = new Dictionary<int, CertificateType>();
			CertificateTypeDic.Add(0, CertificateType.Passport);
			CertificateTypeDic.Add(1, CertificateType.Other);
			ViewBag.CertificateTypeDic = CertificateTypeDic;

			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			var RegionList = DbSession.Query<Region>().ToList();
			var PlanList = DbSession.Query<Plan>().ToList();
			if (RegionList.Count > 0) {
				PlanList = PlanList.Where(s => s.RegionPlans.Any(d => d.Region == RegionList[0])).ToList();
			}
			ViewBag.StreetList = DbSession.Query<Street>().
				Where(s => s.Region.Id == client.Address.House.Street.Region.Id).ToList();
			ViewBag.HouseList = DbSession.Query<House>().
				Where(s => s.Street.Id == client.Address.House.Street.Id).ToList();
			ViewBag.RegionList = RegionList;
			ViewBag.PlanList = PlanList;

			ViewBag.Client = client;
			return View();
		}

		/// <summary>
		///  Форма редактирования клиента POST
		/// </summary>
		/// <param name="ClientRegistration"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult ClientEditor([EntityBinder] Client client)
		{
			var errors = ValidationRunner.ValidateDeep(client);
			if (errors.Length == 0) {
				DbSession.Update(client);
				SuccessMessage("Клиент успешно изменен!");
				return RedirectToAction("ClientList");
			}

			// список типов документа
			var CertificateTypeDic = new Dictionary<int, CertificateType>();
			CertificateTypeDic.Add(0, CertificateType.Passport);
			CertificateTypeDic.Add(1, CertificateType.Other);
			ViewBag.CertificateTypeDic = CertificateTypeDic;
			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			var RegionList = DbSession.Query<Region>().ToList();
			var PlanList = DbSession.Query<Plan>().ToList();
			if (RegionList.Count > 0) {
				PlanList = PlanList.Where(s => s.RegionPlans.Any(d => d.Region == RegionList[0])).ToList();
			}
			ViewBag.StreetList = DbSession.Query<Street>().
				Where(s => s.Region.Id == client.Address.House.Street.Region.Id).ToList();
			ViewBag.HouseList = DbSession.Query<House>().
				Where(s => s.Street.Id == client.Address.House.Street.Id).ToList();
			ViewBag.RegionList = RegionList;
			ViewBag.PlanList = PlanList;

			ViewBag.Client = client;
			return View();
		}
	}
}