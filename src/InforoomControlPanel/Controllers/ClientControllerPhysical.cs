using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
		/// Страница списка клиентов
		/// </summary>
		public ActionResult List(bool openInANewTab = true, bool error = false)
		{
			InforoomModelFilter<Client> pager = null;
			try {
				pager = new InforoomModelFilter<Client>(this);
				pager = ClientReports.GetGeneralReport(this, pager, false);
			}
			catch (Exception ex) {
				if (!(ex is FormatException)) {
					throw ex;
				}
				pager = null;
			}
			if (pager == null) {
				return RedirectToAction("List", new {@error = true});
			}
			if (error) {
				ErrorMessage("Ошибка ввода: неподдерживаемый формат введенных данных.");
			}
			if (!openInANewTab && pager.TotalItems == 1) {
				var clientList = pager.GetItems();
				//TODO: выпилить после полного перехода на новую админку
				//var OldRedirect = ConfigHelper.GetParam("adminPanelOld");
				//
				//var urlToRedirect = OldRedirect + String.Format("UserInfo/{0}?filter.ClientCode={1}",
				//	(clientList.First().PhysicalClient != null ? "ShowPhysicalClient" : "ShowLawyerPerson"), clientList.First().Id);
				//return Redirect(urlToRedirect);

				return RedirectToAction((clientList.First().PhysicalClient != null ? "InfoPhysical" : "InfoLegal"), "Client", new {clientList.First().Id});
			}
			return View("List");
		}

		/// <summary>
		/// Страница редактирования клиента - физ. лица (учавствует и в пост-запросе)
		/// </summary>
		/// <param name="id">идентификатор</param>
		/// <param name="clientModelItem">модель клиента</param>
		/// <param name="subViewName">подПредставление</param>
		/// <param name="appealType">тип оповещений</param>
		/// <param name="writeOffState">объединение списка "списаний" - месяц / год</param>
		/// <param name="clientStatus">статус клиента</param>
		/// <returns></returns>
		public ActionResult InfoPhysical(int id, object clientModelItem = null, string subViewName = "", int appealType = 0,
			int writeOffState = 0, int clientStatus = 0, string redmineTaskComment = "")
		{
			Client client;
			//--------------------------------------------------------------------------------------| Обработка представлений (редактирование клиента)
			bool updateSce = false;
			// Получаем клиента
			if (clientModelItem != null && clientModelItem as Client != null) {
				var clientModel = (Client) clientModelItem;
				//подпредставление "контакты"
				if (subViewName == "_Contacts") {
					// удаление неиспользованного контакта *иначе в БД лишняя запись  
					clientModel.Contacts.RemoveEach(s => s.ContactString == string.Empty);
					clientModel.Contacts.Each(s => s.ContactString = s.ContactPhoneSplitFormat);
				}
				// проводим валидацию модели клиента
				var errors = ValidationRunner.ValidateDeep(clientModel);

				if (!string.IsNullOrEmpty(redmineTaskComment)) {
					if (!string.IsNullOrEmpty(clientModel.RedmineTask)) {
						var newAppeal =
							new Appeal(
								redmineTaskComment +
								String.Format(" <a target='_blank' href='http://redmine.analit.net/issues/{0}'>#{0}</a>",
									clientModel.RedmineTask), clientModel,
								AppealType.User)
							{Employee = GetCurrentEmployee()};
						DbSession.Save(newAppeal);
					}
					else {
						errors.Add(new InvalidValue("Не указана задача в Redmine", typeof (Status), "redmineTaskComment", clientModel,
							clientModel, new List<object>()));
						ErrorMessage("Не указана задача в Redmine");
						ViewBag.RedmineTaskComment = redmineTaskComment;
					}
				}

				//если появились ошибки в любом из подпредставлений, кроме контактов, удаляем их
				if (subViewName != "_Contacts") {
					errors.RemoveErrors(new List<string>()
					{
						"Inforoom2.Models.Client.Contacts"
					});
				}
				//проводим валидацию "паспортных данных"
				if (subViewName == "_PassportData") {
					errors = ValidationRunner.Validate(clientModel.PhysicalClient);
				}

				//обновлдение статуса клиента, если он отличается от текущего
				if (clientStatus != 0 && clientStatus != (int) clientModel.Status.Type && errors.Length == 0) {
					var newStatus = DbSession.Query<Status>().FirstOrDefault(s => s.Id == clientStatus);
					var messageAlert = clientModel.TryToChangeStatus(DbSession, newStatus, GetCurrentEmployee(), ref updateSce);
					if (!string.IsNullOrEmpty(messageAlert)) {
						ErrorMessage(messageAlert);
						errors.Add(new InvalidValue("", typeof (Status), "Name", newStatus, newStatus, new List<object>()));
					}
				}
				//если нет ошибок
				if (errors.Length == 0) {
					// сохраняем модель
					DbSession.Update(clientModel);
					if (updateSce) {
						Inforoom2.Helpers.SceHelper.UpdatePackageId(DbSession, clientModel);
					}
					return RedirectToAction("InfoPhysical",
						new
						{
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
			var appeals = client.Appeals.Select(s => new Appeal()
			{
				Client = s.Client,
				AppealType = s.AppealType,
				Date = s.Date,
				Employee = s.Employee,
				inforoom2 = s.inforoom2,
				Message = s.Message
			}).ToList();
			//если задан тип сообщений, фильтруем по типу
			if (appealType != 0) {
				appeals = appeals.Where(s => (int) s.AppealType == appealType).ToList();
			}
			else {
				//при выводе всех сообщений, нужно учесть комментарии к сервисным заявкам
				string pattern = "<li><span>{0}</span> - <span>{1}:</span><br/><span>{2}</span></li>";
				appeals.AddEach(client.ServiceRequests.Select(s =>
				{
					return new Appeal()
					{
						Client = s.Client,
						AppealType = AppealType.All,
						Date = s.CreationDate,
						Employee = s.Employee,
						inforoom2 = true,
						Message =
							"<p>Сервисная заявка № <a target=_blank href=" +
							Url.Action("ServiceRequestEdit", "ServiceRequest", new {@id = s.Id}) + ">" +
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
			writeoffsAndUserWriteOff.AddRange(client.WriteOffs.Select(s => new WriteOff()
			{
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
			writeoffsAndUserWriteOff.AddRange(client.UserWriteOffs.Select(s => new UserWriteOff()
			{
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
					s => s as WriteOff != null ? ((WriteOff) s).WriteOffDate : ((UserWriteOff) s).Date).ToList();
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
				s.Id == (int) StatusType.Worked
				|| s.Id == (int) StatusType.NoWorked
				|| s.Id == (int) StatusType.Dissolved).OrderBy(s => s.Name).ToList();
			//список коммутаторов для региона клиента
			ViewBag.SwitchList =
				DbSession.Query<Switch>().Where(s => s.Zone.Region == client.Address.Region).OrderBy(s => s.Name).ToList();
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
			ViewBag.ActionName = ((string) ViewBag.ActionName) + " hid";
			return View();
		}

		/// <summary>
		/// Страница редактирования клиента - физ. лица  (непосредствено редактирования). // структура такая из-за того, что в проекте используется [EntityBinder] и все под него заточено. 
		/// 1) EntityBinder - есть во всем проекте (делать иначе вкаком-то конкретном случае - плохо, а менять весь проект долго) и поэтому не Angular (или др.).
		/// 2) Почему не Ajax - он использован там, где не нужно выводить пользователю валидировать по полям
		/// </summary>
		/// <param name="client">модель клиента с формы</param>
		/// <param name="subViewName">подпредставление</param>
		/// <param name="newUserAppeal">новое оповещение о клиенте</param>
		/// <param name="writeOffState">тип объединения списаний (месяц / год)</param>
		/// <param name="clientStatus">статус клиента</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult InfoPhysical([EntityBinder] Client client, string subViewName, string newUserAppeal = "",
			int writeOffState = 0, int clientStatus = 0, string redmineTaskComment = "")
		{
			//создание нового оповещения
			if (!string.IsNullOrEmpty(newUserAppeal)) {
				var newAppeal = new Appeal(newUserAppeal, client, AppealType.User) {Employee = GetCurrentEmployee()};
				newAppeal.ReplaceSharpWithRedmine();
				DbSession.Save(newAppeal);
			}
			//обработка модели клиента, сохранение, передача необходимых данных на форму.
			return InfoPhysical(client.Id, clientModelItem: client, subViewName: subViewName, writeOffState: writeOffState,
				clientStatus: clientStatus, redmineTaskComment: redmineTaskComment);
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
			client.PhysicalClient.Address = new Address()
			{
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
			errors.RemoveErrors(new List<string>()
			{
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
				IList<ClientService> csList = services.Select(service => new ClientService
				{
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
					                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelNew"] +
					                Url.Action("ConnectionCard", "Client", new {@id = client.Id}));
				}
				// иначе переходим к информации о клиенте *в старой админке

				return Redirect(System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
				                "Clients/UpdateAddressByClient?clientId=" + client.Id +
				                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelNew"] +
				                Url.Action("InfoPhysical", "Client", new {@id = client.Id}));
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
		/// </summary>
		/// <param name="ClientRegistration"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Edit([EntityBinder] Client client, string viewName = "")
		{
			var errors = ValidationRunner.ValidateDeep(client);
			errors.RemoveErrors(new List<string>()
			{
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
				                "&path=" + System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelNew"] +
				                Url.Action("InfoPhysical", "Client", new {@id = client.Id}));
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

		///| -----------------------------------------------|Агенты (не ясно нужны или нет)|------------------------------------------------->>
		public ActionResult AgentList()
		{
			var Agent = DbSession.QueryOver<Agent>().List();
			Agent = Agent.OrderByDescending(s => s.Active).ThenBy(s => s.Name).ToList();
			var employee = DbSession.QueryOver<Employee>().List();
			ViewBag.AgentList = Agent;
			ViewBag.AgentMan = new Agent();
			return View("AgentList");
		}

		public ActionResult AgentAdd([EntityBinder] Agent agent)
		{
			var existedAgent = DbSession.Query<Agent>()
				.FirstOrDefault(s => s.Name.ToLower().Replace(" ", "") == agent.Name.ToLower().Replace(" ", ""));
			if (existedAgent == null) {
				var errors = ValidationRunner.ValidateDeep(agent);
				if (errors.Length == 0) {
					DbSession.Save(agent);
					SuccessMessage("Агент успешно добавлен");
				}
				else {
					ErrorMessage(errors[0].Message);
				}
			}
			else {
				ErrorMessage("Агент с подобным ФИО уже существует!");
			}
			return RedirectToAction("AgentList");
		}

		public ActionResult AgentStatusChange(int id)
		{
			var Agent = DbSession.Query<Agent>().FirstOrDefault(s => s.Id == id);
			if (Agent != null) {
				Agent.Active = !Agent.Active;
				DbSession.Update(Agent);
			}
			return RedirectToAction("AgentList");
		}

		public ActionResult AgentDelete(int id)
		{
			var Agent = DbSession.Query<Agent>().FirstOrDefault(s => s.Id == id);
			if (Agent != null) {
				DbSession.Delete(Agent);
			}
			return RedirectToAction("AgentList");
		}

		//<-----------------------------------------------------------------------------------------------------------------------------------||


		/// <summary>
		/// Страница списка клиентов
		/// </summary>
		public ActionResult Appeals(bool openInANewTab = true)
		{
			var pager = new InforoomModelFilter<Appeal>(this);
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Id", OrderingDirection.Desc);
			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.Date")) &&
			    string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.Date"))) {
				pager.ParamDelete("filter.GreaterOrEqueal.Date");
				pager.ParamDelete("filter.LowerOrEqual.Date");
				pager.ParamSet("filter.GreaterOrEqueal.Date", SystemTime.Now().Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
				pager.ParamSet("filter.LowerOrEqual.Date", SystemTime.Now().Date.ToString("dd.MM.yyyy"));
			}
			var criteria = pager.GetCriteria();
			ViewBag.Pager = pager;
			return View();
		}

		/// <summary>
		/// Страница списка клиентов
		/// </summary>
		public ActionResult ListOnline(bool openInANewTab = true)
		{
			var packageSpeedList = DbSession.Query<PackageSpeed>().ToList();
			var leaseWithoutEndPointList = DbSession.Query<Lease>().Where(s => s.Endpoint == null).ToList();
			var pager = new InforoomModelFilter<Lease>(this);
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Id", OrderingDirection.Desc);
			var cr = pager.GetCriteria();
			ViewBag.Pager = pager;
			ViewBag.LeaseWithoutEndPointList = leaseWithoutEndPointList;
			ViewBag.PackageSpeedList = packageSpeedList;
			return View();
		}

		/// <summary>
		/// Страница списка клиентов
		/// </summary>
		public ActionResult LeasesLog(int Id = 0)
		{
			var pager = new FilterReport<Internetsessionslog>(this);
			if (Id == 0 && string.IsNullOrEmpty(pager.GetParam("Id")) == false) {
				int.TryParse(pager.GetParam("Id"), out Id);
			}
			if (Id == 0) {
				return RedirectToAction("List");
			}
			var client = DbSession.Query<Client>().FirstOrDefault(s => s.Id == Id);
			if (client == null) {
				return RedirectToAction("List");
			}

			var result = LeaseReport.GetGeneralReport(this, pager, DbSession, client);
			if (result == null) {
				return RedirectToAction("List");
			}

			ViewBag.Result = result;
			ViewBag.Pager = pager;
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
			return RedirectToAction("ConnectionCard", new {Id, updateKeySt = updatePasswordKey});
		}

		public ActionResult _Contacts(int Id, string updatePasswordKey)
		{
			return View("_Contacts");
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
				var contact = new Contact()
				{
					Client = client,
					ContactString = number,
					Type = ContactType.ConnectedPhone,
					Date = SystemTime.Now()
				};
				DbSession.Save(contact);
				DbSession.Delete(phone);
				var appeal = new Appeal
				{
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
				new {@Id = client.Id, @subViewName = subViewName});
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
					if (string.IsNullOrEmpty(comment) == false) {
						client.Discount = (int) writeOff.Sale.Value;
						client.StartNoBlock = SystemTime.Now();
						var partner = GetCurrentEmployee();
						var appealText = string.Format("Скидка {0}% возвращена клиенту {1}. Вернул {2}. Причина: {3}",
							client.Discount.ToString("0.00"), client.Id, partner.Name, comment);
						client.Appeals.Add(new Appeal(appealText, client, AppealType.Statistic));
						DbSession.Save(client);


						var str = ConfigHelper.GetParam("SaleUpdateMail");
						if (str == null)
							throw new Exception("Параметр приложения SaleUpdateMail должен быть задан в config");

						var emails = str.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
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
			return RedirectToAction("InfoPhysical", new {@Id = client.Id, @subViewName = subViewName});
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
				if (ConfigHelper.GetParam("PhysicalAddressEditingFlag", true) != null &&
				    ConfigHelper.GetParam("PhysicalAddressEditingFlag") == "true") {
					return RedirectToAction("InfoPhysical", new {@Id = client.Id, @subViewName = subViewName});
				}

				return Redirect(ConfigHelper.GetParam("adminPanelOld") +
				                "Clients/UpdateAddressByClient?clientId=" + client.Id +
				                "&path=" + ConfigHelper.GetParam("adminPanelNew") +
				                "Client/InfoPhysical/" + client.Id);
			}
			else {
				ErrorMessage("Адрес не был изменен: не был указан дом");
			}

			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}

		[HttpPost]
		public ActionResult ChangePlan([EntityBinder] Client client, int plan, bool isActivatedByUser, string subViewName = "")
		{
			var newPlan = DbSession.Query<Plan>().FirstOrDefault(s => s.Id == plan);
			if (newPlan == null) {
				return RedirectToAction("InfoPhysical", new {@Id = client.Id, @subViewName = subViewName});
			}
			var oldPlan = client.PhysicalClient.Plan;
			if (oldPlan != newPlan) {
				client.PhysicalClient.Plan = newPlan;

				DbSession.Save(client);
				Inforoom2.Helpers.SceHelper.UpdatePackageId(DbSession, client);

				SuccessMessage("Тариф клиента успешно изменен");

				// добавление записи в историю тарифов пользователя
				var planHistory = new PlanHistoryEntry
				{
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
				var appeal = new Appeal(msg, client, AppealType.Statistic)
				{
					Employee = GetCurrentEmployee()
				};
				DbSession.Save(appeal);
			}

			var internetService = client.ClientServices.FirstOrDefault(s => s.Service.Name == "Internet");

			if (internetService != null && internetService.ActivatedByUser != isActivatedByUser) {
				internetService.ActivatedByUser = isActivatedByUser;
				DbSession.Save(client);
			}

			return RedirectToAction("InfoPhysical", new {@Id = client.Id, @subViewName = subViewName});
		}

		[HttpPost]
		public ActionResult AddPayment([EntityBinder] Client client, string sum = "", string comment = "",
			bool isBonus = false, string subViewName = "")
		{
			decimal realSum = 0m;
			Decimal.TryParse(sum.ToString().Replace(".", ","), out realSum);
			if (realSum > 0) {
				if (client.LegalClient == null) {
					realSum = Decimal.Round(realSum, 2);
					var payment = new Payment()
					{
						Client = client,
						Comment = comment,
						Employee = GetCurrentEmployee(),
						PaidOn = SystemTime.Now(),
						RecievedOn = SystemTime.Now(),
						Virtual = isBonus,
						Sum = realSum,
						BillingAccount = false
					};
					client.Payments.Add(payment);
					DbSession.Save(payment);
					DbSession.Save(client);
					SuccessMessage("Платеж успешно добавлен и ожидает обработки");
				}
				else {
					ErrorMessage("Юридические лица не могут оплачивать наличностью");
				}
			}
			else {
				ErrorMessage("Платеж не был добавлен: данные введены неверно");
			}

			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}

		[HttpPost]
		public ActionResult MovePayment([EntityBinder] Client client, int clientReceiverId = 0, int paymentId = 0,
			string comment = "", string subViewName = "")
		{
			var clientReceiver = DbSession.Query<Client>().FirstOrDefault(s => s.Id == clientReceiverId);

			if (clientReceiver == null && string.IsNullOrEmpty(comment)) {
				ErrorMessage("Платеж не был переведен: указанный лицевой счет не существует, причина перевода не указана");
				return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
					new {@Id = client.Id, @subViewName = subViewName});
			}
			if (clientReceiver == null) {
				ErrorMessage("Платеж не был переведен: указанный лицевой счет не существует");
				return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
					new {@Id = client.Id, @subViewName = subViewName});
			}
			if (string.IsNullOrEmpty(comment)) {
				ErrorMessage("Платеж не был переведен: не указана причина перевода");
				return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
					new {@Id = client.Id, @subViewName = subViewName});
			}

			decimal paymentSum = -1;
			if (paymentId != 0) {
				var payment = client.Payments.FirstOrDefault(s => s.Id == paymentId);
				if (!payment.BillingAccount) {
					ErrorMessage($"Платеж {paymentId} не был переведен: платеж ожидает обработки");
					return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
						new {@Id = client.Id, @subViewName = subViewName});
				}
				paymentSum = payment.Sum;
				var appeal = payment.Cancel(comment, GetCurrentEmployee());
				client.Appeals.Add(appeal);

				payment.Client.Payments.Remove(payment);
				payment.Client = clientReceiver;
				payment.BillingAccount = false;
				clientReceiver.Payments.Add(payment);

				var msgTextFormat =
					"Платеж №{4} клиента №<a href='{5}'>{1}</a> на сумму {0} руб.  был перемещен клиенту №<a href='{6}'>{2}</a>.<br/>Комментарий: {3} ";
				var msgTextA = String.Format(msgTextFormat + "<br/>Баланс: {7}. ", payment.Sum.ToString("0.00"),
					client.Id, clientReceiver.Id, comment, payment.Id,
					ConfigHelper.GetParam("adminPanelNew") +
					Url.Action(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal", new {@Id = client.Id}),
					ConfigHelper.GetParam("adminPanelNew") +
					Url.Action(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal", new {@Id = clientReceiver.Id}),
					client.Balance.ToString("0.00"));
				var msgTextB = String.Format(msgTextFormat, payment.Sum.ToString("0.00"), client.Id, clientReceiver.Id, comment,
					payment.Id,
					ConfigHelper.GetParam("adminPanelNew") +
					Url.Action(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal", new {@Id = client.Id}),
					ConfigHelper.GetParam("adminPanelNew") +
					Url.Action(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal", new {@Id = clientReceiver.Id}));
				var clientAppeal = new Appeal(msgTextA, client, AppealType.System)
				{
					Employee = GetCurrentEmployee(),
					inforoom2 = true
				};
				var clientReceiverAppeal = new Appeal(msgTextB, clientReceiver, AppealType.System)
				{
					Employee = GetCurrentEmployee(),
					inforoom2 = true
				};

				DbSession.Save(payment);
				client.Appeals.Add(clientAppeal);
				clientReceiver.Appeals.Add(clientReceiverAppeal);


				DbSession.Save(client);
				DbSession.Save(clientReceiver);


				var appealText = string.Format(@"
Переведен платеж №{0}
От клиента: №{1}
Клиенту: №{2}
Сумма: {3}
Оператор: {4}
Комментарий: {5}
", paymentId, client.Name + " (" + client.Id + ") ", clientReceiver.Name + " (" + clientReceiver.Id + ") ",
					paymentSum.ToString("0.00"), GetCurrentEmployee().Name, comment);


				string emails = "InternetBilling@analit.net";
#if DEBUG
#else
						EmailSender.SendEmail(emails, "Переведен платеж", appealText);
#endif


				SuccessMessage("Платеж успешно переведен и ожидает обработки");
			}
			else {
				ErrorMessage("Платеж не был переведен: платежа с данным номером в базе нет");
			}

			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}

		[HttpPost]
		public ActionResult RemovePayment([EntityBinder] Client client, int paymentId = 0, string comment = "",
			string subViewName = "")
		{
			if (string.IsNullOrEmpty(comment)) {
				ErrorMessage($"Платеж {paymentId} не был отменен: не указана причина отмены");
				return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
					new {@Id = client.Id, @subViewName = subViewName});
			}
			decimal paymentSum = -1;
			if (paymentId != 0) {
				var payment = client.Payments.FirstOrDefault(s => s.Id == paymentId);
				if (!payment.BillingAccount) {
					ErrorMessage($"Платеж {paymentId} не был отменен: платеж ожидает обработки");
					return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
						new {@Id = client.Id, @subViewName = subViewName});
				}
				paymentSum = payment.Sum;
				var appeal = payment.Cancel(comment, GetCurrentEmployee());
				client.Appeals.Add(appeal);
				client.Payments.Remove(payment);
				DbSession.Save(client);
				DbSession.Delete(payment);
			}

			if (paymentSum != -1) {
				SuccessMessage($"Платеж {paymentId} успешно отменен!");

				var str = ConfigHelper.GetParam("PaymentNotificationMail");
				if (str == null)
					throw new Exception("Параметр приложения SaleUpdateMail должен быть задан в config");
				var appealText = string.Format(@"
Отменен платеж №{0}
Клиент: №{1} - {2}
Сумма: {3:C}
Оператор: {4}
Комментарий: {5}
", paymentId, client.Id, client.Name, paymentSum, GetCurrentEmployee().Name, comment);


				var emails = str.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
#if DEBUG
#else
						EmailSender.SendEmail(emails, "Уведомление об отмене платежа", appealText);
#endif
			}
			else {
				ErrorMessage($"Платеж {paymentId} не был удален: платежа по данному номеру не существует");
			}
			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}

		[HttpPost]
		public ActionResult AddWriteOff([EntityBinder] Client client, string sum = "", string comment = "",
			string subViewName = "")
		{
			decimal realSum = 0m;
			Decimal.TryParse(sum.ToString().Replace(".", ","), out realSum);
			if (realSum > 0 && comment != "") {
				realSum = Decimal.Round(realSum, 2);
				var writeOff = new UserWriteOff()
				{
					Comment = comment,
					Sum = realSum,
					Date = SystemTime.Now(),
					Client = client,
					Employee = GetCurrentEmployee(),
					IsProcessedByBilling = false
				};
				client.UserWriteOffs.Add(writeOff);
				DbSession.Save(client);
				SuccessMessage("Списание успешно добавлено и ожидает обработки");
			}
			else {
				ErrorMessage("Списание не было добавлено: данные введены неверно");
			}

			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}

		[HttpPost]
		public ActionResult DeleteWriteOff([EntityBinder] Client client, int writeOffId = 0, int user = 0, string comment = "",
			string subViewName = "")
		{
			decimal writeOffSum = -1;
			if (string.IsNullOrEmpty(comment)) {
				ErrorMessage($"Списание {writeOffId} не было удалено: не указана причина отмены");
				return RedirectToAction("InfoPhysical", new {@Id = client.Id, @subViewName = subViewName});
			}
			if (user == 1) {
				var writeOff = client.UserWriteOffs.FirstOrDefault(s => s.Id == writeOffId);
				if (!writeOff.IsProcessedByBilling) {
					ErrorMessage($"Списание {writeOffId} не было удалено: списание ожидает обработки");
					return RedirectToAction("InfoPhysical", new {@Id = client.Id, @subViewName = subViewName});
				}
				writeOffSum = writeOff.Sum;
				var appeal = writeOff.Cancel(GetCurrentEmployee(), comment);
				client.Appeals.Add(appeal);
				client.UserWriteOffs.Remove(writeOff);
				DbSession.Save(client);
			}
			else {
				var writeOff = client.WriteOffs.FirstOrDefault(s => s.Id == writeOffId);
				writeOffSum = writeOff.WriteOffSum;
				var appeal = writeOff.Cancel(GetCurrentEmployee(), comment);
				client.Appeals.Add(appeal);
				client.WriteOffs.Remove(writeOff);
				DbSession.Save(client);
			}

			if (writeOffSum != -1) {
				SuccessMessage($"Списание {writeOffId} успешно удалено!");

				var str = ConfigHelper.GetParam("WriteOffNotificationMail");
				if (str == null)
					throw new Exception("Параметр приложения WriteOffNotificationMail должен быть задан в config");
				var appealText = string.Format(@"
Отменено списание №{0}
Клиент: №{1} - {2}
Сумма: {3}
Оператор: {4}
Комментарий: {5}
", writeOffId, client.Id, client.Name, writeOffSum, GetCurrentEmployee().Name, comment);

				var emails = str.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
#if DEBUG
#else
						EmailSender.SendEmail(emails, "Уведомление об удалении списания", appealText);
#endif
			}
			else {
				ErrorMessage($"Списание {writeOffId} не было удалено: списания по данному номеру не существует");
			}

			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}

		[HttpPost]
		public ActionResult UpdateConnection([EntityBinder] Client client, int endpointId,
			Inforoom2.Helpers.ConnectionHelper connection, StaticIp[] staticAddress, string connectSum = "",
			string subViewName = "")
		{
			if (staticAddress != null)
				foreach (var item in staticAddress) item.Mask = item.Mask.HasValue && item.Mask.Value == 0 ? 32 : item.Mask;
			string errorMessage = "";
			if (connection.Switch == 0) {
				ErrorMessage("Ошибка при обновлении подключения. Коммутатор не выбран!");
				return RedirectToAction("InfoPhysical", new {@Id = client.Id, @subViewName = subViewName});
			}
			if (connection.Port == "0") {
				ErrorMessage("Ошибка при обновлении подключения. Укажите порт!");
				return RedirectToAction("InfoPhysical", new {@Id = client.Id, @subViewName = subViewName});
			}
			client.PhysicalClient.SaveSwitchForClient(DbSession, endpointId, connectSum, connection, staticAddress,
				out errorMessage, GetCurrentEmployee());
			if (!string.IsNullOrEmpty(errorMessage)) {
				ErrorMessage(errorMessage);
			}

			return RedirectToAction("InfoPhysical", new {@Id = client.Id, @subViewName = subViewName});
		}


		[HttpPost]
		public ActionResult RemoveEndpoint([EntityBinder] Client client, int endpointId,
			string subViewName = "")
		{
			var endPoint = client.Endpoints.FirstOrDefault(s => s.Id == endpointId);
			if (endPoint != null) {
				//TODO: важно! SQL запрос необходим для удаления элемента (прежний вариант с отчисткой списка удалял клиентов у endpoint(ов))
				if (!client.RemoveEndpoint(endPoint, DbSession))
					ErrorMessage("Последняя точка подключения не может быть удалена!");
			}
			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}


		[HttpPost]
		public JsonResult GetSubnet(int mask)
		{
			return Json(SubnetMask.CreateByNetBitLength(mask).ToString(), JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// получение ФИО по ЛС
		/// </summary>
		[HttpPost]
		public JsonResult getClientName(int id)
		{
			var client = DbSession.Get<Client>(id);
			if (client != null) {
				return Json(client.GetName(), JsonRequestBehavior.AllowGet);
			}
			return Json("Данного ЛС в базе нет", JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// получение ФИО по ЛС
		/// </summary>
		[HttpPost]
		public JsonResult getBusyPorts(int id)
		{
			var switchItem = DbSession.Get<Switch>(id);
			if (switchItem != null) {
				var ports =
					switchItem.Endpoints.Select(
						s => new {@endpoint = s.Port, @client = s.Client.Id, @type = s.Client.PhysicalClient != null ? 0 : 1}).ToList();
				var data = new {Ports = ports, Comment = switchItem.Description};
				return Json(data, JsonRequestBehavior.AllowGet);
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// получение Коммутаторов по зоне
		/// </summary>
		[HttpPost]
		public JsonResult getSwitchesByZone(string name)
		{
			var switchList = name == String.Empty
				? DbSession.Query<Switch>()
					.Where(s => s.Name != null)
					.Select(s => s.Name)
					.OrderBy(s => s)
					.ToList()
					.Distinct()
					.ToList()
				: DbSession.Query<Switch>()
					.Where(s => s.Zone.Name == name && s.Name != null)
					.Select(s => s.Name)
					.OrderBy(s => s)
					.ToList()
					.Distinct()
					.ToList();
			return Json(switchList, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// получение Ip для фиксирования
		/// </summary>
		[HttpPost]
		public JsonResult GetStaticIp(int id)
		{
			var lease = DbSession.Query<Lease>().FirstOrDefault(l => l.Endpoint.Id == id);
			return Json(lease != null && lease.Ip != null ? lease.Ip.ToString() : "", JsonRequestBehavior.AllowGet);
		}
	}
}