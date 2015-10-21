using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate;
using NHibernate.Linq;

namespace Inforoom2.Helpers
{
	/// <summary>
	/// Наследование необходимо, чтобы избежать создания лишних интерфейсов у Inforoom2Controller, 
	/// лишние интерфейсы WarningHelper(а) не так важны т.к. он будет использоваться лишь один раз в Inforoom2Controller.
	/// </summary>
	public class WarningHelper
	{
		public WarningHelper(ActionExecutingContext filterContext)
		{
			if (filterContext.Controller == null) return;
			if (filterContext.Controller as Inforoom2Controller == null) {
				throw new Exception("Контроллер должен быть наследником Inforoom2Controller!");
			}
			InforoomExecutingContext = filterContext;
			InforoomController = (Inforoom2Controller)filterContext.Controller;
			_currentController = InforoomController.ControllerContext.RouteData.Values["controller"].ToString().ToLower();
			_currentAction = InforoomController.ControllerContext.RouteData.Values["action"].ToString().ToLower();

			_requestHost = InforoomController.Request.QueryString["host"];
			_requestUrl = InforoomController.Request.QueryString["url"];
			_requestParams = InforoomController.Request.QueryString["params"];
		}

		[Description("Исключения, экшены, для которых мы не выводим варнинг для запроса POST")] private readonly WarningRedirect[] _exceptionsForWarningMethodPost = new WarningRedirect[] {
			new WarningRedirect() { Action = "TryToDisableWarning".ToLower(), Controller = "warning" },
			new WarningRedirect() { Action = "DeactivateAccountBlocking".ToLower(), Controller = "service" },
			new WarningRedirect() { Action = "InternetPlanChanger".ToLower(), Controller = "service" },
			new WarningRedirect() { Action = "InternetPlanChanger".ToLower(), Controller = "service" },
			new WarningRedirect() { Action = "RepairCompleted".ToLower(), Controller = "warning" },
			new WarningRedirect() { Action = "FirstVisit".ToLower(), Controller = "personal" },
			new WarningRedirect() { Action = "SubmitCallMeBackTicket".ToLower(), Controller = "warning" }
		};

		[Description("Исключения, экшены, для которых мы не выводим варнинг для запроса GET")] private readonly WarningRedirect[] _exceptionsForWarningMethodGet = new WarningRedirect[] {
			new WarningRedirect() { Action = "FirstVisit".ToLower(), Controller = "personal" },
			new WarningRedirect() { Action = "InternetPlanChanger".ToLower(), Controller = "service" },
			new WarningRedirect() { Action = "Logout".ToLower(), Controller = "account" },
			new WarningRedirect() { Action = "SubmitCallMeBackTicket".ToLower(), Controller = "warning" }
		};

		[Description("Контроллер, наследующийся от Inforoom2Controller")]
		protected Inforoom2Controller InforoomController { get; set; }

		[Description("Клиент")]
		protected Client InforoomClient { get; set; }

		[Description("Контекст исполняемого экшена")]
		protected ActionExecutingContext InforoomExecutingContext { get; set; }

		[Description("Текущий контроллер")] private readonly string _currentController = "";
		[Description("Текущий экшен")] private readonly string _currentAction = "";

		[Description("Параметр из запроса пользователя для редиректа - Хост")] private readonly string _requestHost = "";
		[Description("Параметр из запроса пользователя для редиректа - Url")] private readonly string _requestUrl = "";
		[Description("Параметр из запроса пользователя для редиректа - Params")] private readonly string _requestParams = "";


		/// <summary>
		/// Выполняет редирект, если есть необходимость
		/// </summary>
		public void TryWarningToRedirect()
		{
			if (InforoomController == null) return;
			InforoomClient = InforoomController.GetCurrentClient();
			WarningRedirect warningRedirect;
			if (InforoomClient == null) {
				warningRedirect = GetWarningActionResult(RedirectTarget.DefaultPage);
				TryRedirect(warningRedirect);
				return;
			}
			//фильтрация запросов POST по списку исключений
			if (HasRequestWarningExceptionPost()) {
				//при запросе TryToDisableWarning выполняется попытка диактивации варнинга TODO:можно заменить событием
				if (_currentAction == "TryToDisableWarning".ToLower()) {
					warningRedirect = TryToDisableWarning();
					//попытка редиректа
					TryRedirect(warningRedirect);
				}
				return;
			}
			//фильтрация запросов GET по списку исключений
			if (HasRequestWarningExceptionGet()) {
				return;
			}
			//получение объекта с набором параметров для редиректа
			warningRedirect = CheckClientForWarning();
			//попытка редиректа
			TryRedirect(warningRedirect);
		}
		 /// <summary>
		 /// Фильтрация запроса POST, нужно ли переводить пользователя на целевой адрес
		 /// </summary>
		 /// <returns>Разрешение</returns>
		private bool HasRequestWarningExceptionPost()
		{
			var client = InforoomClient;
			if (InforoomExecutingContext.HttpContext.Request.HttpMethod == "POST"
				&& (_exceptionsForWarningMethodPost.Any(s => s.Action.ToLower() == _currentAction && s.Controller == _currentController)
					|| (client.PhysicalClient != null && client.Balance < 0 && "deferredpayment" == _currentAction && "service" == _currentController)
					|| (client.Status.Type == StatusType.VoluntaryBlocking && "blockaccount" == _currentAction && "service" == _currentController)))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Фильтрация запроса GET, нужно ли переводить пользователя на целевой адрес
		/// </summary>
		/// <returns>Разрешение</returns>
		private bool HasRequestWarningExceptionGet()
		{
			var client = InforoomClient;
			if (InforoomExecutingContext.HttpContext.Request.HttpMethod == "GET"
				&& (_exceptionsForWarningMethodPost.Any(s => s.Action.ToLower() == _currentAction && s.Controller == _currentController)
					|| (client.PhysicalClient != null && (client.Status.Type == StatusType.VoluntaryBlocking || client.Balance < 0)
						&& "service" == _currentAction && "personal" == _currentController)))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Редирект, если передан ненулевой объект с набором параметров для редиректа
		/// </summary>
		/// <param name="redirectResult">Объект с набором параметров для редиректа</param>
		private void TryRedirect(WarningRedirect redirectResult)
		{
			if (redirectResult != null) {
				var warningUrl = InforoomController.Url.Action(redirectResult.Action,
					redirectResult.Controller, redirectResult.Parameters);
				InforoomExecutingContext.Result = new RedirectResult(warningUrl);
			}
		}

		/// <summary>
		/// Попытка отменить варнинг
		/// </summary>
		/// <returns>Объект с набором параметров для редиректа</returns>
		private WarningRedirect TryToDisableWarning()
		{
			var routValues = (_requestHost != null && _requestUrl == null) || (_requestHost != null && _requestUrl != null) ?
				new { @host = _requestHost, @url = _requestUrl, @params = _requestParams } : null;

			var client = InforoomClient;
			if (client.PhysicalClient != null && client.Status.Type == StatusType.BlockedForRepair) {
				client.SetStatus(StatusType.Worked, InforoomController.DbSession);
			}
			else if (client.Disabled) {
				if (client.PhysicalClient != null && client.Status.Type == StatusType.VoluntaryBlocking) {
					return new WarningRedirect { Action = "Service", Controller = "Personal", Parameters = routValues };
				}
					//и баланс меньше 0
				else if (client.PhysicalClient != null && client.Balance < 0) {
					return new WarningRedirect { Action = "Service", Controller = "Personal", Parameters = routValues };
				}
				return new WarningRedirect { Action = "Index", Controller = "Home", Parameters = routValues };
			}
			else if (client.ShowBalanceWarningPage) {
				client.ShowBalanceWarningPage = false;
				var appeal = new Appeal("Отключена страница Warning, клиент отключил со страницы", client, AppealType.Statistic) {
					Employee = InforoomController.GetCurrentEmployee()
				};
				InforoomController.DbSession.Save(appeal);
			}
			InforoomController.DbSession.Save(client);
			InforoomController.DbSession.Flush();

			UpdateSce();

			if (!client.HasPassportData())
				return new WarningRedirect { Action = "FirstVisit", Controller = "Personal", Parameters = routValues };
			;
			return new WarningRedirect() { Action = "Index", Controller = "Home", Parameters = routValues };
		}

		/// <summary>
		/// Обновелние значений PackageId клиента у SCE
		/// </summary>
		private void UpdateSce()
		{
			SceHelper.UpdatePackageId(InforoomController.DbSession, InforoomClient);
		}

		/// <summary>
		/// Проверка клиента на необходимость редиректа
		/// </summary>
		/// <returns>Объект с набором параметров для редиректа</returns>
		private WarningRedirect CheckClientForWarning()
		{
			var client = InforoomClient;
			if (client == null) {
				return GetWarningActionResult(RedirectTarget.DefaultPage);
			}
			//Выставляем Флаг о блокировке варнингом в сессии
			if (InforoomController.Session["WarningBlocked"] == null) {
				InforoomController.Session.Add("WarningBlocked", true);
			}
			else {
				InforoomController.Session["WarningBlocked"] = true;
			}
			//Проверка юр.лица
			if (client.LegalClient != null) {
				//if (client.Disabled && client.Balance >= BalanceForWarningLegalPerson) {
				//	return GetWarningActionResult(RedirectTarget.LawDisabled);
				//}
				if (client.ShowBalanceWarningPage) {
					return GetWarningActionResult(RedirectTarget.LawLowBalance);
				}
				if (client.Disabled) {
					return GetWarningActionResult(RedirectTarget.LawLowBalance);
				}
			}
				//Проверка физ.лица
			else if (client.PhysicalClient != null) {
				//елси заблокирован
				if (client.Disabled) {
					//и статус VoluntaryBlocking
					if (client.Status.Type == StatusType.VoluntaryBlocking) {
						return GetWarningActionResult(RedirectTarget.PhysVoluntaryBlocking);
					}
						//и баланс меньше 0
					else if (client.Balance < 0) {
						return GetWarningActionResult(RedirectTarget.PhysLowBalance);
					}
						//и количество выплат (перечислений) = 0
					else if (client.Payments.Count == 0) {
						return GetWarningActionResult(RedirectTarget.PhysFirstPayment);
					}
				}
					//или отсутствуют паспортные данные
				else {
					if (!client.HasPassportData()) {
						return GetWarningActionResult(RedirectTarget.PhysPassportData);
					}
				}
				//если статус BlockedForRepair
				if (client.Status.Type == StatusType.BlockedForRepair) {
					return GetWarningActionResult(RedirectTarget.PhysBlockedForRepair);
				}
			}

			//Убираем Флаг о блокировке варнингом в сессии
			if (InforoomController.Session["WarningBlocked"] == null) {
				InforoomController.Session.Add("WarningBlocked", false);
			}
			else {
				InforoomController.Session["WarningBlocked"] = false;
			}

			//Редирект пользователя на главную, если он попал на варнинг случайно
			return GetWarningActionResult(RedirectTarget.DefaultPageFromWarning);
		}

		/// <summary>
		/// Формирование объекта с набором параметров для редиректа (если нет необходимости редиректить - null)
		/// </summary>
		/// <param name="targetAction">Название экшена Варнинга</param>
		/// <returns>Объект с набором параметров для редиректа (если нет необходимости редиректить, возвращает null)</returns>
		private WarningRedirect GetWarningActionResult(RedirectTarget targetAction)
		{
			const string warningController = "warning";
			const string homeController = "home";
			const string indexAction = "index";
			RedirectTarget redirectTargetEnum;
			bool isCurrenActionWarninig = Enum.TryParse(_currentAction, true, out redirectTargetEnum);

			//если пользователь находится на странице варнинга, но он не авторизован
			if (InforoomClient == null) {
				if (_currentController == warningController && (isCurrenActionWarninig || _currentAction == indexAction)) {
					return new WarningRedirect { Action = indexAction.ToString(), Controller = homeController };
				}
				return null;
			}

			var routValues = (_requestHost != null && _requestUrl == null) || (_requestHost != null && _requestUrl != null) ?
				new { @host = _requestHost, @url = _requestUrl, @params = _requestParams } : null;

			if (targetAction == RedirectTarget.DefaultPageFromWarning) {
				if (_currentController == warningController) {
					return new WarningRedirect { Action = indexAction, Controller = homeController, Parameters = routValues };
				}
				//возврат пустого значения, если пользователь не на стр. Варнинга
				return null;
			}

			if (targetAction == RedirectTarget.DefaultPage && _currentController != homeController && _currentAction != indexAction) {
				return new WarningRedirect { Action = indexAction, Controller = homeController, Parameters = routValues };
			}
			else if (redirectTargetEnum != targetAction) {
				return new WarningRedirect { Action = targetAction.ToString(), Controller = warningController, Parameters = routValues };
			}
			return null;
		}

		/// <summary>
		/// Объект с набором параметров для редиректа
		/// </summary>
		private class WarningRedirect
		{
			public string Action { get; set; }
			public string Controller { get; set; }
			public object Parameters { get; set; }
		}

		private enum RedirectTarget
		{
			[Description("Редирект на главную")] DefaultPage,
			[Description("Редирект на главную")] DefaultPageFromWarning,
			LawDisabled,
			LawLowBalance,
			PhysBlockedForRepair,
			PhysVoluntaryBlocking,
			PhysLowBalance,
			PhysFirstPayment,
			PhysPassportData
		}

		/// <summary>
		/// Получение из запроса адреса для редиректа, 
		/// если редирект предусмотрен разблокировкой Варнинга и запрос содержит необходимые параметры
		/// </summary>
		/// <param name="controller">Контроллер</param>
		/// <returns>Url-адрес</returns>
		public static string GetForwardUrl(Controller controller)
		{
			if (controller == null) return "";
			//Проверка, если редирект предусмотрен разблокировкой Варнинга
			if (controller.Session["WarningBlocked"] != null && ((bool)controller.Session["WarningBlocked"]) == false) {
				//Сбор необходимых составляющий адреса из запроса
				var parsedUrl = HttpUtility.ParseQueryString(controller.Request.UrlReferrer.Query);
				var requestHost = parsedUrl.Get("host");
				var requestUrl = parsedUrl.Get("url");
				var requestParams = parsedUrl.Get("params");
				//Формирование адреса, в случае наличии всех необходимых составляющих
				var returnUrl = (requestHost != null && requestUrl == null) || (requestHost != null && requestUrl != null) ?
					string.Format("http://{0}{1}{2}", requestHost, requestUrl, requestParams != null ? "?" + requestParams : "") : "";
				return returnUrl;
			}
			return "";
		}
	}
}