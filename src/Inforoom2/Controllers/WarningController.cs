using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Страница управления аутентификацией
	/// </summary>
	/// <summary>
	/// Контроллер для вывода блокирующих сообщений юр.лицу 
	/// здесь не должно быть никаких манипуляций с клиентом (эта логика отрабатывает для контроллера WarningController),
	/// Только вывод подходящего сообщения или переадресация.
	/// </summary>
	public class WarningLawController : BaseController
	{
	    protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);

			var currentController = ControllerContext.RouteData.Values["controller"].ToString().ToLower();
			var currentAction = ControllerContext.RouteData.Values["action"].ToString().ToLower();


			var client = WarningController.GetClientIfExists(this);
			if (client == null || client.IsLegalClient == false ||
			    /// Переадресация юр.лица на главную страницу 
			    !((client.Disabled && currentController == "warninglaw" && currentAction == "lawdisabled")
			      || (client.ShowBalanceWarningPage && currentController == "warninglaw" && currentAction == "lawlowbalance"))) {
				var resultUrl = Url.Action("Index", "Home");
				filterContext.Result = new RedirectResult(resultUrl);
				return;
			}

			//выставляем нужные значения для корректного отображения страницы
			ViewBag.Title = "Инфорум";
			var cityList = DbSession.Query<Region>().Where(s => s.ShownOnMainPage).Select(s => s.Name).OrderBy(s => s).ToArray();
			ViewBag.Cities = cityList;
			var currentRegion = client.GetRegion();
			if (currentRegion != null) {
				SetCookie("userCity", currentRegion.Name);
				ViewBag.RegionOfficePhoneNumber = currentRegion.RegionOfficePhoneNumber;
				ViewBag.UserCityBelongsToUs = true;
				ViewBag.UserCity = currentRegion.Name;
				ViewBag.CurrentRegion = currentRegion;
			}
#if DEBUG
			//тестовое значение ip адреса 
			//Можем авторизоваться по лизе за клиента 
			ViewBag.IpForTests = Request.QueryString["ip"] ?? null;
#endif
		}

	    /// <summary>
		/// страница уведомления о блокировки
		/// </summary>
		/// <returns></returns>
		public ActionResult LawDisabled()
		{
			return View();
		}

	    /// <summary>
		/// страница уведомления о низком балансе
		/// </summary>
		/// <returns></returns>
		public ActionResult LawLowBalance()
		{
			return View();
		}
	}

	/// <summary>
	/// Отображение блокирующих сообщений в зависимости от состояния клиента (здесь только логика отображения страницы)			 *(состояние клиента устанавливается и изменяется объектом "WarningHelper")
	/// </summary>
	public class WarningController : BaseController
	{
	    protected Client CurrentClient { get; set; }

	    protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			if (filterContext.HttpContext.Request.Browser.Browser == "Unknown" &&
			    filterContext.HttpContext.Request.Browser.Platform == "Unknown" &&
			    filterContext.HttpContext.Request.Browser.Version == "0.0" &&
			    filterContext.HttpContext.Request.Browser.ClrVersion == null) {
				filterContext.Result = new RedirectResult(ConfigurationManager.AppSettings["errorPath"]);
				return;
			}
			base.OnActionExecuting(filterContext);


			CurrentClient = GetClientIfExists(this);

			if (CurrentClient == null) {
				filterContext.Result = RedirectToAction("Index", "Home");
				return;
			}

#if DEBUG
			//тестовое значение ip адреса 
			//Можем авторизоваться по лизе за клиента 
			var endpoint = CurrentClient.Endpoints.FirstOrDefault();
			var lease = DbSession.Query<Lease>().FirstOrDefault(i => i.Endpoint == endpoint);
			ViewBag.IpForTests = lease?.Ip?.ToString();
#endif

            ViewBag.NetworkClientFlag = false;
            if (CurrentClient != null && CurrentClient.IsPhysicalClient && (User?.Identity == null || string.IsNullOrEmpty(User?.Identity.Name))) {
				//Это необходимо, чтобы авторизация срабатывала моментально. Так как метод authenticate требует перезагрузки страницы
				SetCookie("networkClient", "true");
				ViewBag.NetworkClientFlag = true;
				//Юр. лиц авторизовывать не нужно
				Authenticate(ViewBag.ActionName, ViewBag.ControllerName, CurrentClient.Id.ToString(), true);
			}
			else {
            ViewBag.NetworkClientFlag = false;
			}

			ViewBag.CallMeBackTicket = new CallMeBackTicket
			{
				Name = CurrentClient.Name,
				PhoneNumber = CurrentClient.PhoneNumber ?? ""
			};

			if (CurrentClient.PhysicalClient != null) {
				var sb = new StringBuilder();
				sb.AppendFormat("Здравствуйте, {0} {1}. Ваш баланс: {2} руб.", CurrentClient.PhysicalClient.Name,
					CurrentClient.PhysicalClient.Patronymic, CurrentClient.PhysicalClient.Balance);
				ViewBag.ClientInfo = sb.ToString();
			}

			ViewBag.Title = "Инфорум";
			ViewBag.Cities = DbSession.Query<Region>().Where(s => s.ShownOnMainPage).Select(s => s.Name).OrderBy(s => s).ToArray();

			var CurrentRegion = CurrentClient.GetRegion();
			if (CurrentRegion != null) {
				SetCookie("userCity", CurrentRegion.Name);
				ViewBag.RegionOfficePhoneNumber = CurrentRegion.RegionOfficePhoneNumber;
				ViewBag.UserCityBelongsToUs = true;
				ViewBag.UserCity = CurrentRegion.Name;
				ViewBag.CurrentRegion = CurrentRegion;
			}
			ViewBag.CurrentClient = CurrentClient;
		}

	    /// <summary>
		/// страница варнинга по умолчанию
		/// </summary>
		/// <param name="disable"></param>
		/// <param name="ip"></param>
		/// <returns></returns>
		public ActionResult Index(string ip = "")
		{
			var warningReason = CurrentClient.GetWarningState();
			if (warningReason == WarningState.NoWarning) {
				/////////////////////////////////////////////////////////////////////////////ПРОТЕСТИРОВАТЬ
				if (DbSession.Transaction.IsActive) {
				    CurrentClient.ShowBalanceWarningPage = false;
				    DbSession.Save(CurrentClient);
                    DbSession.Flush();
                    DbSession.Transaction.Commit();
				}
				SceHelper.UpdatePackageId(CurrentClient.Id);
			}
			else {
				if (CurrentClient.IsLegalClient) {
#if DEBUG
					return RedirectToAction(warningReason.ToString(), "WarningLaw", new {ip });
#endif
					return RedirectToAction(warningReason.ToString(), "WarningLaw");
				}
#if DEBUG
			    return RedirectToAction(warningReason.ToString(), "Warning", new {ip });
#endif
			    return RedirectToAction(warningReason.ToString(), "Warning");
			}
			return RedirectToAction("Index", "Home");
		}

	    /// <summary>
		/// Переход на страницу, подтверждающую возобновление работы, после ремонтных работ
		/// </summary>
		/// <returns></returns>
		public ActionResult RepairCompleted()
		{
			SuccessMessage("Работа возобновлена");
			return RedirectToAction("Index", "Home");
		}

	    /// <summary>
		/// Страница для клиента с ремонтными работами
		/// </summary>
		/// <returns></returns>
		public ActionResult PhysBlockedForRepair()
		{
			return View();
		}

	    /// <summary>
		/// Страница для клиента с низким балансом 
		/// </summary>
		/// <returns></returns>
		public ActionResult PhysLowBalance()
		{
			return View();
		}

	    /// <summary>
		///  Страница для клиента с добровольной блокировкой
		/// </summary>
		/// <returns></returns>
		public ActionResult PhysVoluntaryBlocking()
		{
			var services = DbSession.Query<Service>().Where(s => s.IsActivableFromWeb);
			var blockAccountService = services.FirstOrDefault(s => s is BlockAccountService);
			ViewBag.BlockAccountService = blockAccountService.Unproxy();

			return View();
		}

	    /// <summary>
		///  Страница для клиента без единого платежа
		/// </summary>
		/// <returns></returns>
		public ActionResult PhysFirstPayment()
		{
			return View();
		}

	    /// <summary>
		///  Страница для клиента без паспортных данных
		/// </summary>
		/// <returns></returns>
		public ActionResult PhysPassportData()
		{
			return View();
		}

	    /// <summary>
		/// Попытка отключить блокирующие сообщения
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public ActionResult TryToDisableWarning()
		{
			var controller = this;
			if (CurrentClient == null) {
				//может следует отправлять на почту уведомления об ip-адресах, попавших на варнинг ?
				return RedirectToAction("Index", "Home");
			}
			//УСЛОВИЯ СООТВЕТСТВУЮТ ПРИОРИТЕТУ
			var client = CurrentClient;
			//клиент - физ. лицо и у него незаполненны паспортные данные
			if (client.PhysicalClient != null && !client.HasPassportData())
				//если не заполнено все
				if (client.AbsentPassportData(true))
					//отправляем его на страницу Профиля, раздел Заполнения паспортных данных
					return RedirectToAction("FirstVisit", "Personal");


			//если клиент - физ. лицо и у него статус "Восстановление работ"
			if (client.PhysicalClient != null && client.Status.Type == StatusType.BlockedForRepair) {
				//выставляем статус "Подключен"
				client.SetStatus(StatusType.Worked, controller.DbSession);
				//обновляем скорость на кго коммутаторе
				foreach (var item in client.Endpoints) {
					item.SetStablePackgeId(client.PhysicalClient.Plan.PackageSpeed.PackageId);
					controller.DbSession.Save(item);
				}
				controller.DbSession.Save(client);
				controller.DbSession.Flush();
				/////////////////////////////////////////////////////////////////////////////ПРОТЕСТИРОВАТЬ
				if (DbSession.Transaction.IsActive) {
					DbSession.Transaction.Commit();
				}
				//логинимся в SCE
				SceHelper.UpdatePackageId(client.Id);
				//переадресация на страницу с сообщением
#if DEBUG
				return RedirectToAction("RepairCompleted", "Warning", new { ip = ViewBag.IpForTests });
#endif
				return RedirectToAction("RepairCompleted", "Warning");
			}

			//иначе, если клиент деактивирован 
			if (client.Disabled) {
				//он физ. лицо со статусом "Добровольная блокировка"
				if (client.PhysicalClient != null && client.Status.Type == StatusType.VoluntaryBlocking) {
					//переадресация на страницу Профиля, раздел Услуги
					return RedirectToAction("Service", "Personal");
				}
				//он физ. лицо и его баланс меньше 0
				if (client.PhysicalClient != null && client.Balance < 0) {
					//переадресация на страницу Профиля, раздел Услуги
					return RedirectToAction("Service", "Personal");
				}
				//если ничего не подошло - переадресация на главную
				return RedirectToAction("Index", "Home");
			}

			//если клиент не деактивирован , но у него выставлен флаг "Показ варнинга"
			if (client.ShowBalanceWarningPage) {
				//убираем этот флаг
				client.ShowBalanceWarningPage = false;
				//отправляем сообщение
				var appeal = new Appeal("Отключена страница Warning, клиент отключил со страницы", client, AppealType.Statistic) {
					Employee = controller.GetCurrentEmployee()
				};
                controller.DbSession.Save(client);
                controller.DbSession.Save(appeal);
				controller.DbSession.Flush();
				/////////////////////////////////////////////////////////////////////////////ПРОТЕСТИРОВАТЬ
				if (DbSession.Transaction.IsActive) {
					DbSession.Transaction.Commit();
				}
				//обновляем значение PackageId у CSE
				SceHelper.UpdatePackageId(client.Id);
			}

			//если ничего не подошло - переадресация на главную
			return RedirectToAction("Index", "Home");
		}

	    /// <summary>
		/// Попытка авторизовать юр. лицо
		/// </summary>
		/// <param name="controller"></param>
		/// <returns>авторизованный клиент</returns>
		public static Client GetClientIfExists(BaseController controller)
		{ 
	        int clientId = 0;

	        if (FormsAuthentication.CookiesSupported) {
	            var cookie = controller.Request.Cookies[FormsAuthentication.FormsCookieName];
	            if (cookie != null) {
	                var ticket = FormsAuthentication.Decrypt(cookie.Value);
	                if (ticket != null && !string.IsNullOrEmpty(ticket.UserData)) {
	                    var impersonatedClientId = ticket.UserData;
	                    int.TryParse(impersonatedClientId, out clientId);
	                }
	                var userName = ticket.Name;
	                if (clientId != 0) {
	                    userName = clientId.ToString();
	                }
	                var identity = new GenericIdentity(userName, "Forms");
	                System.Web.HttpContext.Current.User = new CustomPrincipal(identity, new List<Permission>(),
	                    new List<Role>());
	            }
	        }
	        if (controller?.User?.Identity?.Name != null && int.TryParse(controller.User.Identity.Name, out clientId)) {
	            var client = controller.DbSession.Query<Client>().FirstOrDefault(s => s.Id == clientId);
	            if (client != null) {
	                return client;
	            }
	        } 

            var ipstring = controller.HttpContext.Request.UserHostAddress;

#if DEBUG
			//Можем авторизоваться по лизе за клиента
			ipstring = !string.IsNullOrEmpty(controller.HttpContext.Request.QueryString["ip"]) ? controller.HttpContext.Request.QueryString["ip"] : ipstring;
#endif
			if (string.IsNullOrEmpty(ipstring))
				return null;

			var endpoint = ClientEndpoint.GetEndpointForIp(ipstring, controller.DbSession);

			var endpointIdString = controller.HttpContext.Request.QueryString["n"];
			if (endpoint == null && string.IsNullOrEmpty(endpointIdString) == false) {
				var endpointId = 0;
				if (!string.IsNullOrEmpty(endpointIdString)) {
					int.TryParse(endpointIdString, out endpointId);
				}
				if (endpointId != 0) {
					endpoint = controller.DbSession.Query<ClientEndpoint>().FirstOrDefault(s => s.Id == endpointId);
				}
			}
			return endpoint != null ? endpoint.Client : null;
		}
	}
}