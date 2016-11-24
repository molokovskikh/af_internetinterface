using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Mapping;
using Remotion.Linq.Clauses;

namespace InforoomControlPanel.Controllers
{
	public partial class ClientController
	{
		/// <summary>
		/// Список заявок на подключение
		/// </summary>
		/// <param name="requestMarkers"></param>
		/// <param name="archived"></param>
		/// <param name="requestText"></param>
		/// <returns></returns>
		public ActionResult RequestsList(string requestMarkers = null, string requestText = "", bool justNull = true)
		{
			var pager = new InforoomModelFilter<ClientRequest>(this);
			var markers = !string.IsNullOrEmpty(requestMarkers)
				? requestMarkers.Split(',').Select(s => int.Parse(s)).ToArray()
				: null;
			if (markers == null) {
				if (!string.IsNullOrEmpty(pager.GetParam("requestMarkers"))) {
					requestMarkers = pager.GetParam("requestMarkers");
					markers = requestMarkers.Split(',').Select(s => int.Parse(s)).ToArray();
				}
			}
			if (markers != null && markers.Length > 0) {
				ViewBag.CurrentMarkers = requestMarkers;
				pager.ParamSet("requestMarkers", requestMarkers);
			}
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("RegDate", OrderingDirection.Desc);
			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.RegDate")) &&
			    string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.RegDate"))) {
				pager.ParamDelete("filter.GreaterOrEqueal.RegDate");
				pager.ParamDelete("filter.LowerOrEqual.RegDate");
				pager.ParamSet("filter.GreaterOrEqueal.RegDate", SystemTime.Now().Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
				pager.ParamSet("filter.LowerOrEqual.RegDate", SystemTime.Now().Date.ToString("dd.MM.yyyy"));
			}
			if (string.IsNullOrEmpty(pager.GetParam("filter.Equal.Archived"))) {
				pager.ParamDelete("filter.Equal.Archived");
				pager.ParamSet("filter.Equal.Archived", "false");
			}

			if (markers != null && markers.Length > 0) {
				if (requestText != "") {
					int isId = 0;
					int.TryParse(requestText, out isId);
					var criteria =
						pager.GetCriteria(
							criterion:
								Restrictions.Or(Restrictions.In("Marker", markers),
									Restrictions.Or(
										Restrictions.Or(
											Restrictions.Or(Restrictions.Like("City", "%" + requestText + "%"),
												Restrictions.Like("Street", "%" + requestText + "%")),
											Restrictions.Or(Restrictions.Like("Housing", "%" + requestText + "%"),
												Restrictions.Like("ApplicantPhoneNumber", "%" + requestText + "%"))),
										Restrictions.Or(Restrictions.Eq("Id", isId), Restrictions.Like("ApplicantName", "%" + requestText + "%")))));
				}
				else {
					pager.GetCriteria(
						criterion: Restrictions.In("Marker", markers));
				}
			}
			else {
				if (requestText != "") {
					int isId = 0;
					int.TryParse(requestText, out isId);
					var criteria =
						pager.GetCriteria(
							criterion:
								Restrictions.Or(
									Restrictions.Or(
										Restrictions.Or(Restrictions.Like("City", "%" + requestText + "%"),
											Restrictions.Like("Street", "%" + requestText + "%")),
										Restrictions.Or(Restrictions.Like("Housing", "%" + requestText + "%"),
											Restrictions.Like("ApplicantPhoneNumber", "%" + requestText + "%"))),
									Restrictions.Or(Restrictions.Eq("Id", isId), Restrictions.Like("ApplicantName", "%" + requestText + "%"))));
				}
				else {
					if (justNull) {
						var criteria = pager.GetCriteria(s => s.Marker == null);
					}
					else {
						var criteria = pager.GetCriteria();
					}
				}
			}
			ViewBag.Markers = DbSession.Query<ConnectionRequestMarker>().OrderBy(s => s.Deleted).ThenBy(s => s.Name).ToList();
			ViewBag.Pager = pager;
			ViewBag.JustNull = justNull;
			return View();
		}

		[HttpPost]
		public ActionResult RequestMarkerColorChange(string[] markedItems, int? markerId)
		{
			var markedElements = markedItems != null
				? markedItems.Where(s => s != "").Select(s => int.Parse(s)).ToArray()
				: new int[0];
			var markerList = DbSession.Query<ClientRequest>().Where(s => markedElements.Contains(s.Id)).ToList();
			var marker = DbSession.Query<ConnectionRequestMarker>().FirstOrDefault(s => s.Id == markerId);
			if (markerList.Count > 0) {
				foreach (var item in markerList) {
					if (marker != null &&
					    (marker.ShortComment == "Refused" || marker.ShortComment == "Deleted" || marker.ShortComment == "Registered")) {
						item.Archived = true;
					}
					item.Marker = marker;
					DbSession.Save(item);
				}
				if (marker != null) {
					SuccessMessage($"Заявки были успешно помечены маркером '{marker.Name}'");
				}
				else {
					SuccessMessage($"Маркировка была успешна убрана");
				}
			}
			else {
				ErrorMessage("Не удалось маркировать заявки");
			}
			return RedirectToAction("RequestsList");
		}

		/// <summary>
		/// Отображает форму новой заявки
		/// </summary>
		[HttpGet]
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
				planList = DbSession.Query<Plan>().Where(s => s.Disabled == false && s.AvailableForNewClients
				                                              && s.RegionPlans.Any(d => d.Region == (currentRegion)))
					.OrderBy(s => s.Name)
					.ToList();
			}
			// списки улиц и домов
			var currentStreetList = currentStreet == null || currentRegion == null
				? new List<Street>()
				: DbSession.Query<Street>()
					.Where(s => s.Region.Id == currentRegion.Id || s.Houses.Any(a => a.Region.Id == currentRegion.Id))
					.ToList()
					.OrderBy(s => s.PublicName())
					.ToList();
			var currentHouseList = currentStreet == null || currentRegion == null
				? new List<House>()
				: DbSession.Query<House>().Where(s => (s.Region == null || currentRegion.Id == s.Region.Id) &&
				                                      ((s.Street.Region.Id == currentRegion.Id && s.Street.Id == currentStreet.Id) ||
				                                       (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id)) &&
				                                      (s.Street.Region.Id == currentRegion.Id && s.Region == null ||
				                                       (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id)))
					.OrderBy(s => s.Number)
					.ToList();
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

		protected Address GetAddressByYandexData(ClientRequest clientRequest)
		{
			var region = DbSession.Query<Region>().FirstOrDefault(r => r.Name.Replace(" ", "").ToLower() == clientRequest.City.Replace(" ", "").ToLower());
			if (clientRequest.YandexStreet != null) {
				var street = DbSession.Query<Street>()
					.FirstOrDefault(s => s.Name.Replace(" ", "").ToLower() == clientRequest.YandexStreet.Replace(" ", "").ToLower() && s.Region == region);
				if (street == null)
					return null;
			}
			if (clientRequest.YandexStreet != null && clientRequest.YandexHouse != null) {
				var house = DbSession.Query<House>().FirstOrDefault(h => h.Number.Replace(" ", "").ToLower() == clientRequest.YandexHouse.Replace(" ", "").ToLower()
				                                                         && h.Street.Name.Replace(" ", "").ToLower() == clientRequest.YandexStreet.Replace(" ", "").ToLower()
				                                                         && (h.Street.Region == region || h.Region == region));
				if (house == null)
					return null;
				return new Address() { House = house };
			}

			return null;
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
					ContactString =
						clientRequest.ApplicantPhoneNumber.IndexOf('-') != -1
							? clientRequest.ApplicantPhoneNumber
							: clientRequest.ApplicantPhoneNumber.Insert(3, "-"),
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
			var currentStreet =
				DbSession.Query<Street>().FirstOrDefault(s => s.Name.ToLower().Trim() == clientRequest.Street.ToLower().Trim());
			var tempHouseNumber = (clientRequest.HouseNumber != null ? clientRequest.HouseNumber + clientRequest.Housing : "");
			var houseToFind = DbSession.Query<House>().FirstOrDefault(s => s.Number == tempHouseNumber
			                                                               &&
			                                                               (s.Region == currentRegion ||
			                                                                s.Street == currentStreet && s.Region == null));

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
					client.PhysicalClient.Address = clientRequest.Address;
					currentRegion = client.PhysicalClient.Address.Region;
					houseToFind = client.PhysicalClient.Address.House;
					currentStreet = client.PhysicalClient.Address.House.Street;
				}
				else {
					client.PhysicalClient.Address = new Address() {
						House = null,
						Floor = clientRequest.Floor,
						Entrance = clientRequest.Entrance.ToString(),
						Apartment = clientRequest.Apartment.ToString()
					};
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
			// списки улиц и домов
			var currentStreetList = currentStreet == null || currentRegion == null
				? new List<Street>()
				: DbSession.Query<Street>()
					.Where(s => s.Region.Id == currentRegion.Id || s.Houses.Any(a => a.Region.Id == currentRegion.Id))
					.ToList()
					.OrderBy(s => s.PublicName())
					.ToList();
			var currentHouseList = currentStreet == null || currentRegion == null
				? new List<House>()
				: DbSession.Query<House>().Where(s => (s.Region == null || currentRegion.Id == s.Region.Id) &&
				                                      ((s.Street.Region.Id == currentRegion.Id && s.Street.Id == currentStreet.Id) ||
				                                       (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id)) &&
				                                      (s.Street.Region.Id == currentRegion.Id && s.Region == null ||
				                                       (s.Street.Id == currentStreet.Id && s.Region.Id == currentRegion.Id)))
					.OrderBy(s => s.Number)
					.ToList();
			// тариф по запросу
			client.PhysicalClient.Plan = clientRequest.Plan;
			// получаем списки регионов и тарифов по выбранному выбранному региону (городу)
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			var planList = DbSession.Query<Plan>().OrderBy(s => s.Name).ToList();
			if (regionList.Count > 0) {
				planList = planList.Where(s => s.Disabled == false && s.AvailableForNewClients
				                               && s.RegionPlans.Any(d => d.Region == currentRegion)).OrderBy(s => s.Name).ToList();
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
		public ActionResult RequestRegistration([EntityBinder] Client client, int requestId, bool redirectToCard,
			bool scapeUserNameDoubling = false)
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
				var services =
					DbSession.Query<Service>().Where(s => s.Name == "IpTv" || s.Name == "Internet" || s.Name == "PlanChanger").ToList();
				IList<ClientService> csList = services.Select(service => new ClientService {
					Service = service,
					Client = client,
					BeginDate = DateTime.Now,
					IsActivated = (service.Name == "PlanChanger"),
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
					//получаем маркер по умолчанию для зарегистрированных заявок 
					var markerRegistered = ConfigHelper.GetParam("connectionRequestDefaultMarker_registered");
					var markerRegisteredId = int.Parse(markerRegistered);
					var defaultMarker = DbSession.Query<ConnectionRequestMarker>().FirstOrDefault(s => s.Id == markerRegisteredId);
					// привязка текущего клиента к поданой им заявке
					// отправление запроса на регистрацию в архив
					clientRequest.Client = client;
					clientRequest.Archived = true;
					clientRequest.Marker = defaultMarker;
					DbSession.Save(clientRequest);
				}
				// предварительно вызывая процедуру (старой админки) которая делает необходимые поправки в записях клиента и физ.клиента
				// переходим к карте клиента *в старой админке, если выбран пункт "Показывать наряд на подключение"
				if (redirectToCard) {
					return Redirect(System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
					                "Clients/UpdateAddressByClient?clientId=" + client.Id +
					                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelNew"]
					                + $"Client/ConnectionCard/{client.Id}");
				}
				// переходим к информации о клиенте *в старой админке
				return Redirect(System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
				                "Clients/UpdateAddressByClient?clientId=" + client.Id +
				                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelNew"]
				                + $"Client/InfoPhysical/{client.Id}");
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
				currentStreetList = currentStreet == null || currentRegion == null
					? new List<Street>()
					: DbSession.Query<Street>()
						.Where(s => s.Region.Id == currentRegion.Id || s.Houses.Any(a => a.Region.Id == currentRegion.Id))
						.ToList()
						.OrderBy(s => s.PublicName())
						.ToList();
				currentHouseList = DbSession.Query<House>().Where(s => (s.Region == null || currentRegion.Id == s.Region.Id) &&
				                                                       ((s.Street.Region.Id == currentRegion.Id &&
				                                                         s.Street.Id == currentStreet.Id) ||
				                                                        (s.Street.Id == currentStreet.Id &&
				                                                         s.Region.Id == currentRegion.Id)) &&
				                                                       (s.Street.Region.Id == currentRegion.Id && s.Region == null ||
				                                                        (s.Street.Id == currentStreet.Id &&
				                                                         s.Region.Id == currentRegion.Id)))
					.OrderBy(s => s.Number)
					.ToList();
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
				planList = DbSession.Query<Plan>().Where(s => s.Disabled == false && s.AvailableForNewClients
				                                              && s.RegionPlans.Any(d => d.Region == (currentRegion)))
					.OrderBy(s => s.Name)
					.ToList();
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
	}
}