using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.UI;
using Common.MySql;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InforoomControlPanel.ReportTemplates;
using InternetInterface.Helpers;
using NHibernate.Linq;
using NHibernate.Util;
using Remotion.Linq.Clauses;
using Agent = Inforoom2.Models.Agent;
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
using NHibernate;
using NHibernate.Proxy.DynamicProxy;
using NHibernate.Transform;
using NHibernate.Validator.Engine;

namespace InforoomControlPanel.Controllers
{
	public partial class ClientController : ControlPanelController
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


		/// <summary>
		/// Страница редактирования клиента - физ. лица (учавствует и в пост-запросе)
		/// TODO: этот божественный метод нужно распилить. Лучше не использовать Глубокую валидацию,
		/// TODO: (в идеале байндить объекты обычным байндером)
		/// TODO: ну или хотя бы выделить сложным ветвлениям по методу
		/// </summary>
		/// <param name="id">идентификатор</param>
		/// <param name="clientModelItem">модель клиента</param>
		/// <param name="subViewName">подПредставление</param>
		/// <param name="appealType">тип оповещений</param>
		/// <param name="writeOffState">объединение списка "списаний" - месяц / год</param>
		/// <param name="clientStatus">статус клиента</param>
		/// <returns></returns>
		public ActionResult InfoPhysical(int id, object clientModelItem = null, string subViewName = "", int appealType = 0,
			int writeOffState = 0, int clientStatus = 0, string clientStatusChangeComment = "")
		{
			Client client;
			//--------------------------------------------------------------------------------------| Обработка представлений (редактирование клиента)
			bool updateSce = false;
			// Получаем клиента
			if (clientModelItem != null && clientModelItem as Client != null) {
				var clientModel = (Client)clientModelItem;
				//подпредставление "контакты"
				if (subViewName == "_Contacts") {
					var listForAppeal = new List<string>();
					//такая структора из-за работы байндера (не менять, а если менять, то колностью работу со списком контактов)
					//удаление контактов
					var contactsToRemove = clientModel.Contacts.Where(s => s.ContactString == string.Empty).ToList();
					foreach (var item in contactsToRemove) {
						clientModel.Contacts.Remove(item);
						DbSession.Delete(item);
					}
					//обновление контактов, задание нужных полей при добавлении
					clientModel.Contacts.Each(s => {
						s.Client = clientModel;
						s.ContactString = s.ContactFormatString;
						s.ContactName = s.ContactName == string.Empty ? null : s.ContactName; // в БД по умолчанию Null а не пустое значение  
						if (s.Date == DateTime.MinValue) {
							s.Date = SystemTime.Now();
						}
						if (s.WhoRegistered == null) {
							s.WhoRegistered = GetCurrentEmployee();
							listForAppeal.Add(s.ContactFormatString);
						}
					});
					if (listForAppeal.Count != 0) {
						clientModel.Appeals.Add(new Appeal($"Обновлены контактные данные. Добавлены: {string.Join(",", listForAppeal)}",
							clientModel, AppealType.System, GetCurrentEmployee()));
					}
				}
				var errors = new ValidationErrors();
				if (subViewName == "_PrivatePhysicalInfo") {
					errors = ValidationRunner.Validate(clientModel);
					errors.AddRange(ValidationRunner.Validate(clientModel.PhysicalClient));
				}
				if (subViewName == "_Appeals") {
					clientModel.Appeals.Each(s => errors.AddRange(ValidationRunner.Validate(s)));
				}
				if (subViewName == "_Contacts") {
					clientModel.Contacts.Each(s => errors.AddRange(ValidationRunner.Validate(s)));
				}
				if (subViewName == "_Payments") {
					clientModel.Payments.Each(s => errors.AddRange(ValidationRunner.Validate(s)));
				}
				if (subViewName == "_WriteOffs") {
					clientModel.WriteOffs.Each(s => errors.AddRange(ValidationRunner.Validate(s)));
				}
				if (subViewName == "_Endpoint") {
					clientModel.Endpoints.Where(s => !s.Disabled).Each(s => errors.AddRange(ValidationRunner.Validate(s)));
				}
				if (subViewName == "_PassportData") {
					errors = ValidationRunner.Validate(clientModel.PhysicalClient);
				}

				if (string.IsNullOrEmpty(clientStatusChangeComment) && clientStatus != 0 &&
				    clientStatus != (int)clientModel.Status.Type
				    && clientStatus == (int)StatusType.Dissolved) {
					errors.Add(new InvalidValue("Не указана причина изменения статуса", typeof(Status), "clientStatusChangeComment",
						clientModel,
						clientModel, new List<object>()));
					ErrorMessage("Не указана причина изменения статуса");
					ViewBag.clientStatusChangeComment = clientStatusChangeComment;
				}
				//обновлдение статуса клиента, если он отличается от текущего
				if (clientStatus != 0 && clientStatus != (int)clientModel.Status.Type && errors.Length == 0) {
					var newStatus = DbSession.Query<Status>().FirstOrDefault(s => s.Id == clientStatus);
					var messageAlert = clientModel.TryToChangeStatus(DbSession, newStatus, GetCurrentEmployee(), ref updateSce);
					if (!string.IsNullOrEmpty(messageAlert)) {
						ErrorMessage(messageAlert);
						errors.Add(new InvalidValue("", typeof(Status), "Name", newStatus, newStatus, new List<object>()));
					}
					else {
						if (!string.IsNullOrEmpty(clientStatusChangeComment)) {
							clientModel.Appeals.Add(new Appeal($"Комментарий к изменению статуса: {clientStatusChangeComment}",
								clientModel, AppealType.System, GetCurrentEmployee()));
						}
					}
				}
				//если нет ошибок
				if (errors.Length == 0) {
					if (clientModel._Name != clientModel.PhysicalClient.FullName)
						clientModel._Name = clientModel.PhysicalClient.FullName;
					// сохраняем модель
					DbSession.Update(clientModel);
					if (updateSce) {
						Inforoom2.Helpers.SceHelper.UpdatePackageId(DbSession, clientModel);
					}
					return RedirectToAction("InfoPhysical",
						new {
							@id = id,
							@clientModelItem = clientModelItem,
							@subViewName = subViewName,
							@appealType = appealType,
							@writeOffState = writeOffState
						});
				}
				//иначе передаем сессию на форму, где в конце представления ею рефрешим все модели, чтобы не сохранились значения с ошибками. (из-за байндера)
				else {
					ViewBag.SessionToRefresh = DbSession;
				}
				client = clientModel;
			}
			else {
				client = DbSession.Query<Client>().FirstOrDefault(i => i.PhysicalClient != null && i.Id == id);
			}
			//--------------------------------------------------------------------------------------| Получение списка оповещений
			// список оповещений
			var appeals = client.Appeals == null || client.Appeals.Count == 0 ? new List<Appeal>() : client.Appeals.Select(s => new Appeal() {
				Client = s.Client,
				AppealType = s.AppealType,
				Date = s.Date,
				Employee = s.Employee,
				inforoom2 = s.inforoom2,
				Message = s.Message
			}).ToList();
			//если задан тип сообщений, фильтруем по типу
			if (appealType != 0) {
				appeals = appeals.Where(s => (int)s.AppealType == appealType).ToList();
			}
			else {
				//при выводе всех сообщений, нужно учесть комментарии к сервисным заявкам
				string pattern = "<li><span>{0}</span> - <span>{1}:</span><br/><span>{2}</span></li>";
				appeals.AddEach(client.ServiceRequests.Select(s => {
					return new Appeal() {
						Client = s.Client,
						AppealType = AppealType.All,
						Date = s.CreationDate,
						Employee = s.Employee,
						inforoom2 = true,
						Message =
							"<p>Сервисная заявка № <a target=_blank href=" +
							Url.Action("ServiceRequestEdit", "ServiceRequest", new { @id = s.Id }) + ">" +
							s.Id + "</a>. " + s.Description + "</p>" + "<p><ul>" +
							String.Join("", s.ServiceRequestComments.Select(d =>
								string.Format(pattern, d.CreationDate, d.Author.Name, d.Comment)).ToList()
								) + "</ul></p>"
					};
				}).ToList());
			}
			appeals = appeals.OrderByDescending(s => s.Date).ToList();

			//--------------------------------------------------------------------------------------| Получение списка списаний
			//получение списка списаний
			var writeoffsAndUserWriteOff = new List<object>();
			writeoffsAndUserWriteOff.AddRange(client.WriteOffs.Select(s => new WriteOff() {
				Id = s.Id,
				Sale = s.Sale,
				Comment = s.Comment,
				BeforeWriteOffBalance = s.BeforeWriteOffBalance,
				WriteOffDate = s.WriteOffDate,
				WriteOffSum = s.WriteOffSum,
				VirtualSum = s.VirtualSum,
				MoneySum = s.MoneySum
			}).ToList());
			//дополнение списка пользовательскими списаниями
			writeoffsAndUserWriteOff.AddRange(client.UserWriteOffs.Select(s => new UserWriteOff() {
				Comment = s.Comment,
				Date = s.Date,
				Sum = s.Sum,
				Id = s.Id,
				Employee = s.Employee,
				IsProcessedByBilling = s.IsProcessedByBilling
			}).ToList());
			//сортировка списка списаний
			writeoffsAndUserWriteOff =
				writeoffsAndUserWriteOff.OrderByDescending(
					s => s as WriteOff != null ? ((WriteOff)s).WriteOffDate : ((UserWriteOff)s).Date).ToList();
			//--------------------------------------------------------------------------------------| Передача значений на форму
			//передаем на форму - текущее представление (для фокусировки на нем)
			ViewBag.CurrentSubViewName = subViewName;
			//передаем на форму - список списаний
			ViewBag.WriteoffsAndUserWriteOff = writeoffsAndUserWriteOff;
			//передаем на форму - тип обертки списаний (месяц / год)
			ViewBag.WriteOffState = writeOffState;
			//передаем на форму - список оповещений
			ViewBag.Appeals = appeals;
			//передаем на форму - фильтр типа оповещений
			ViewBag.AppealType = appealType;
			//передаем на форму - список неопознанных звонков
			ViewBag.UnresolvedCalls = DbSession.Query<UnresolvedCall>().OrderByDescending(s => s.Id).Take(10).ToList();
			//передаем на форму - список скоростей
			ViewBag.PackageSpeedList = DbSession.Query<PackageSpeed>().ToList();
			//передаем на форму - список пулов для регионов
			ViewBag.IpPoolRegionList = DbSession.Query<IpPoolRegion>().Where(s => s.Region == client.GetRegion()).ToList();
			//передаем на форму - список статусов клиента
			ViewBag.StatusList = DbSession.Query<Status>().Where(s =>
				s.Id == (int)StatusType.Worked
				|| s.Id == (int)StatusType.NoWorked
				|| s.Id == (int)StatusType.Dissolved).OrderBy(s => s.Name).ToList();
			//список коммутаторов для региона клиента
			ViewBag.SwitchList =
				DbSession.Query<Switch>().Where(s => client.Address != null && s.Zone.Region.Id == client.Address.Region.Id).OrderBy(s => s.Name).ToList();
			//передаем на форму - список регионов
			ViewBag.RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			//ViewBag.PlanList = DbSession.Query<Plan>().Where(s => !s.Disabled).OrderBy(s => s.Name).ToList();
			ViewBag.PlanList = DbSession.Query<Plan>().OrderBy(s => s.Name).ToList();
			//передаем на форму - клиента
			ViewBag.Client = client;


			//--------------------------------------------------------------------------------------| Информация по коммутаторам (нужно уточнить)
			// Find active RentalHardware
			var activeServices = client.RentalHardwareList.Where(rh => rh.IsActive).ToList();
			ViewBag.RentIsActive = activeServices.Count > 0;
			if (client.Status != null && client.Status.Type != StatusType.BlockedAndConnected) {
				// Find Switches
				var networkNodeList = DbSession.Query<SwitchAddress>().Where(s =>
					s.House == client.PhysicalClient.Address.House && s.Entrance.ToString() == client.PhysicalClient.Address.Entrance ||
					s.House == client.PhysicalClient.Address.House && s.Entrance == null).ToList();
				if (networkNodeList.Count > 0) {
					ViewBag.NetworkNodeList = networkNodeList;
				}
			}
			ViewBag.ActionName = ((string)ViewBag.ActionName) + " hid";
			ViewBag.ServiceToActivate =
				DbSession.Query<Service>().FirstOrDefault(s => s.Id == Service.GetIdByType(typeof(SpeedBoost)));

			return View();
		}

		/// <summary>
		/// Страница редактирования клиента - физ. лица  (непосредствено редактирования). // структура такая из-за того, что в проекте используется [EntityBinder] и все под него заточено. 
		/// 1) EntityBinder - есть во всем проекте (делать иначе вкаком-то конкретном случае - плохо, а менять весь проект долго) и поэтому не Angular (или др.).
		/// 2) Почему не Ajax - он используется там, где не нужно выводить пользователю валидацию по полям
		/// </summary>
		/// <param name="client">модель клиента с формы</param>
		/// <param name="subViewName">подпредставление</param>
		/// <param name="newUserAppeal">новое оповещение о клиенте</param>
		/// <param name="writeOffState">тип объединения списаний (месяц / год)</param>
		/// <param name="clientStatus">статус клиента</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult InfoPhysical([EntityBinder] Client client, string subViewName, string newUserAppeal = "",
			int writeOffState = 0, int clientStatus = 0, string clientStatusChangeComment = "")
		{
			//создание нового оповещения
			if (!string.IsNullOrEmpty(newUserAppeal)) {
				var newAppeal = new Appeal(newUserAppeal, client, AppealType.User) { Employee = GetCurrentEmployee() };
				newAppeal.Message = newAppeal.Message.ReplaceSharpWithRedmine();
				DbSession.Save(newAppeal);
			}
			//обработка модели клиента, сохранение, передача необходимых данных на форму.
			return InfoPhysical(client.Id, clientModelItem: client, subViewName: subViewName, writeOffState: writeOffState,
				clientStatus: clientStatus, clientStatusChangeComment: clientStatusChangeComment);
		}


		/// <summary>
		/// Форма регистрации клиента 
		/// </summary> 
		public ActionResult RegistrationPhysical()
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
			ViewBag.Agents = DbSession.Query<Agent>().Where(s => s.Active).OrderBy(s => s.Name).ToList();
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
		public ActionResult RegistrationPhysical([EntityBinder] Client client, bool redirectToCard,
			bool scapeUserNameDoubling = false)
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
			if (errors.Length == 0 && client.Agent != null) {
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
				//@Todo раскомментировать когда закончится интеграция со старой админкой

				//SuccessMessage("Клиент успешно зарегистрирован!"); 

				// предварительно вызывая процедуру (старой админки) которая делает необходимые поправки в записях клиента и физ.клиента
				// переходим к карте клиента *в старой админке, если выбран пункт "Показывать наряд на подключение"
				if (redirectToCard) {
					return Redirect(System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
					                "Clients/UpdateAddressByClient?clientId=" + client.Id +
					                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelNew"]
					                + $"Client/ConnectionCard/{client.Id}");
				}
				// иначе переходим к информации о клиенте *в старой админке

				return Redirect(System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
				                "Clients/UpdateAddressByClient?clientId=" + client.Id +
				                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelNew"]
				                + $"Client/InfoPhysical/{client.Id}");
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
					.OrderBy(s => s.Name)
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
			ViewBag.ScapeUserNameDoubling = scapeUserNameDoubling;

			// получаем всех диллеров (работников) 
			ViewBag.Agents = DbSession.Query<Agent>().Where(s => s.Active).OrderBy(s => s.Name).ToList();
			ViewBag.CertificateTypeDic = CertificateTypeDic;
			ViewBag.RedirectToCard = redirectToCard;
			ViewBag.Client = client;

			return View();
		}

		/// <summary>
		/// Форма редактирования клиента 
		/// TODO: убедиться, что форма не нужна - удалить мусор.
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
			var currentStreetList = currentStreet == null || currentRegion == null
				? new List<Street>()
				: DbSession.Query<Street>()
					.Where(s => s.Region.Id == currentRegion.Id || s.Houses.Any(a => a.Region.Id == currentRegion.Id))
					.OrderBy(s => s.Name)
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

			ViewBag.CertificateTypeDic = certificateTypeDic;
			ViewBag.Client = client;

			return View();
		}

		/// <summary>
		///  Форма редактирования клиента POST
		/// TODO: убедиться, что форма не нужна - удалить мусор.
		/// </summary>
		/// <param name="ClientRegistration"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Edit([EntityBinder] Client client, string viewName = "")
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
				                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelNew"]
				                + $"Client/InfoPhysical/{client.Id}");
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
					if (client.Address.House.Street.GetType().Name.IndexOf("Proxy") != -1) {
						var streetId = client.Address.House.Street.Id;
						var streetRegion = DbSession.Query<Street>().FirstOrDefault(s => s.Id == streetId);
						if (streetRegion != null) {
							currentRegion = streetRegion.Region;
						}
					}
					else {
						currentRegion = client.Address.House.Street.Region;
					}
				}
			}
			var RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();

			// списки улиц и домов

			var currentStreetList = currentStreet == null || currentRegion == null
				? new List<Street>()
				: DbSession.Query<Street>()
					.Where(s => s.Region.Id == currentRegion.Id || s.Houses.Any(a => a.Region.Id == currentRegion.Id))
					.OrderBy(s => s.Name)
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

			ViewBag.RegionList = RegionList;
			ViewBag.CurrentStreetList = currentStreetList;
			ViewBag.CurrentHouseList = currentHouseList;
			ViewBag.PlanList = planList;

			ViewBag.Client = client;

			return View();
		}

		public ActionResult ConnectionCard(int Id, string updateKeySt = "")
		{
			var client = DbSession.Query<Client>().FirstOrDefault(s => s.Id == Id);
			var password = "";
			var physicalClient = client.PhysicalClient;
			if (physicalClient != null) {
				password = Inforoom2.Helpers.CryptoPass.GeneratePassword();
				physicalClient.Password = Inforoom2.Helpers.CryptoPass.GetHashString(password);
				DbSession.Save(physicalClient);
			}
			ViewBag.Client = client;
			ViewBag.NewPassword = password;

			return View();
		}

		public ActionResult ContractOfAgency(int Id)
		{
			var payment = DbSession.Query<Payment>().FirstOrDefault(s => s.Id == Id);
			ViewBag.Payment = payment;
			ViewBag.Employee = GetCurrentEmployee();
			return View();
		}

		[HttpPost]
		public ActionResult ConnectionCardPasswordUpdate(int Id, string updatePasswordKey)
		{
			return RedirectToAction("ConnectionCard", new { Id, updateKeySt = updatePasswordKey });
		}

		/// <summary>
		/// Закрепление неизвестного номера за клиентом
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="phoneId"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult BindPhone(int clientId, int phoneId, string subViewName = "")
		{
			var client = DbSession.Load<Client>(clientId);
			var phone = DbSession.Load<UnresolvedCall>(phoneId);
			if (phone != null && client != null) {
				var number = phone.Phone;
				var registrator = GetCurrentEmployee();
				var contact = new Contact() {
					Client = client,
					ContactString = number,
					Type = ContactType.ConnectedPhone,
					Date = SystemTime.Now()
				};
				DbSession.Save(contact);
				DbSession.Delete(phone);
				var appeal = new Appeal {
					Client = client,
					Date = DateTime.Now,
					AppealType = AppealType.System,
					Employee = registrator,
					Message = string.Format("Номер {0} был привязан к данному клиенту", number)
				};
				DbSession.Save(appeal);
				SuccessMessage(appeal.Message);
			}
			else {
				ErrorMessage("Не удалось привязать выбранный номер к данному клиенту");
			}
			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new { @Id = client.Id, @subViewName = subViewName });
		}

		/// <summary>
		/// Возврат последней скидки
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="comment"></param>
		[HttpPost]
		public ActionResult GetLastSale(int clientId, string comment, string subViewName = "")
		{
			var client = DbSession.Query<Client>().FirstOrDefault(s => s.Id == clientId);
			var writeOff =
				client.WriteOffs.OrderByDescending(s => s.WriteOffDate)
					.FirstOrDefault(s => s.Sale.HasValue && s.Sale > 0);
			if (string.IsNullOrEmpty(comment)) {
				ErrorMessage("Заполните комментарий!");
			}
			if (writeOff != null) {
				if (writeOff.Sale <= client.Discount) {
					ErrorMessage("Клиент не нуждается в восстановлении скидки!");
				}
				else {
					var saleSettings = DbSession.Query<SaleSettings>().FirstOrDefault();
					if (string.IsNullOrEmpty(comment) == false && saleSettings != null) {
						client.Discount = (int)writeOff.Sale.Value;
						var monthOnStart = Convert.ToInt32((client.Discount - saleSettings.MinSale) / saleSettings.SaleStep + saleSettings.PeriodCount);
						client.StartNoBlock = SystemTime.Now().AddMonths(-monthOnStart).Date;
						var partner = GetCurrentEmployee();
						var appealText = string.Format("Скидка {0}% возвращена клиенту {1}. Вернул {2}. Причина: {3}",
							client.Discount.ToString("0.00"), client.Id, partner.Name, comment);
						client.Appeals.Add(new Appeal(appealText, client, AppealType.Statistic));
						DbSession.Save(client);


						var str = ConfigHelper.GetParam("SaleUpdateMail");
						if (str == null)
							throw new Exception("Параметр приложения SaleUpdateMail должен быть задан в config");

						var emails = str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
#if DEBUG
#else
						EmailSender.SendEmail(emails, "Уведомление о возврате скидки", appealText);
#endif
						SuccessMessage("Скидка возвращена!");
					}
				}
			}
			else {
				ErrorMessage("У клиента нет было скидки!");
			}
			return RedirectToAction("InfoPhysical", new { @Id = client.Id, @subViewName = subViewName });
		}

		[HttpPost]
		public ActionResult ChangeAddress([EntityBinder] Client client, int houseId = 0, int floor = 0, string entrance = "",
			string apartment = "", string subViewName = "")
		{
			var house = DbSession.Query<House>().FirstOrDefault(s => s.Id == houseId);
			if (house != null) {
				client.PhysicalClient.Address.House = house;
				client.PhysicalClient.Address.Floor = floor;
				client.PhysicalClient.Address.Entrance = entrance;
				client.PhysicalClient.Address.Apartment = apartment;
				DbSession.Save(client);
				SuccessMessage("Адрес изменен успешно");

				//для тестов просто редирект - TODO: поправить, когда перенесется админка.
#if DEBUG 
				return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
					new { @Id = client.Id, @subViewName = subViewName });
#endif

				return Redirect(ConfigHelper.GetParam("adminPanelOld") +
				                "Clients/UpdateAddressByClient?clientId=" + client.Id +
				                "&path=" + ConfigHelper.GetParam("adminPanelNew") +
				                "Client/InfoPhysical/" + client.Id);
			}
			else {
				ErrorMessage("Адрес не был изменен: не был указан дом");
			}

			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new { @Id = client.Id, @subViewName = subViewName });
		}

		[HttpPost]
		public ActionResult DiactivateBlockAccountService(int id)
		{
			var client = DbSession.Load<Client>(id);
			var clientService = client.ClientServices.FirstOrDefault(c => c.Service.Id == Service.GetIdByType(typeof(BlockAccountService)) && c.IsActivated);
			if (clientService == null) {
				ErrorMessage("Услуга уже была деактивирована.");
				return RedirectToAction("InfoPhysical", new { @Id = client.Id });
			}
			SuccessMessage(clientService.DeActivateFor(client, DbSession));
			Inforoom2.Helpers.SceHelper.UpdatePackageId(DbSession, client);
			client.Appeals.Add(new Appeal($"Услуга 'Добровольная блокировка' была отменена оператором.", client, AppealType.User, GetCurrentEmployee()));
			DbSession.Update(client);
			return RedirectToAction("InfoPhysical", new { @Id = client.Id });
		}

		[HttpPost]
		public ActionResult ChangePlan([EntityBinder] Client client, int plan, bool isActivatedByUser, string subViewName = "")
		{
			var newPlan = DbSession.Query<Plan>().FirstOrDefault(s => s.Id == plan);
			if (newPlan == null) {
				return RedirectToAction("InfoPhysical", new { @Id = client.Id, @subViewName = subViewName });
			}
			var oldPlan = client.PhysicalClient.Plan;
			if (oldPlan != newPlan) {
				client.PhysicalClient.Plan = newPlan;
				client.Endpoints.Where(s => !s.Disabled).ForEach(e => e.PackageId = newPlan.PackageSpeed.PackageId);
				DbSession.Save(client);
				Inforoom2.Helpers.SceHelper.UpdatePackageId(DbSession, client);

				SuccessMessage("Тариф клиента успешно изменен");

				// добавление записи в историю тарифов пользователя
				var planHistory = new PlanHistoryEntry {
					Client = client,
					DateOfChange = SystemTime.Now(),
					PlanAfter = newPlan,
					PlanBefore = oldPlan,
					Price = 0m
				};
				DbSession.Save(planHistory);

				var msg =
					string.Format(
						"Изменение тарифа оператором. Тариф изменен с '{0}'({1}) на '{2}'({3}). Стоимость перехода: {4} руб.",
						oldPlan.Name, oldPlan.Price, newPlan.Name, newPlan.Price, 0);
				var appeal = new Appeal(msg, client, AppealType.Statistic) {
					Employee = GetCurrentEmployee()
				};
				DbSession.Save(appeal);
			}

			var internetService = client.ClientServices.FirstOrDefault(s => s.Service.Name == "Internet");

			if (internetService != null && internetService.ActivatedByUser != isActivatedByUser) {
				internetService.ActivatedByUser = isActivatedByUser;
				DbSession.Save(client);
			}

			return RedirectToAction("InfoPhysical", new { @Id = client.Id, @subViewName = subViewName });
		}


		[HttpPost]
		public ActionResult UpdateConnection([EntityBinder] Client client, int endpointId,
			Inforoom2.Helpers.ConnectionHelper connection, StaticIp[] staticAddress, string connectSum = "",
			string subViewName = "")
		{
			if (staticAddress != null)
				foreach (var item in staticAddress) {
					item.Mask = item.Mask.HasValue && item.Mask.Value == 0 ? 32 : item.Mask;
				}
			string errorMessage = "";
			if (connection.Switch == 0) {
				ErrorMessage("Ошибка при обновлении подключения. Коммутатор не выбран!");
				return RedirectToAction("InfoPhysical", new { @Id = client.Id, @subViewName = subViewName });
			}
			if (connection.Port == "0") {
				ErrorMessage("Ошибка при обновлении подключения. Укажите порт!");
				return RedirectToAction("InfoPhysical", new { @Id = client.Id, @subViewName = subViewName });
			}
			client.PhysicalClient.SaveSwitchForClient(DbSession, endpointId, connectSum, connection, staticAddress,
				out errorMessage, GetCurrentEmployee());
			if (!string.IsNullOrEmpty(errorMessage)) {
				ErrorMessage(errorMessage);
			}

			return RedirectToAction("InfoPhysical", new { @Id = client.Id, @subViewName = subViewName });
		}
	}
}