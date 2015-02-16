using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.UI.WebControls;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using log4net;
using NHibernate;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Базовый контроллер от которого все наследуются
	/// </summary>
	[NHibernateActionFilter]
	public class BaseController : Controller
	{
		public ISession DbSession;
		protected static readonly ILog log = LogManager.GetLogger(typeof(BaseController));

		protected ValidationRunner ValidationRunner;

		public BaseController()
		{
			ValidationRunner = new ValidationRunner();
			ViewBag.Validation = ValidationRunner;
			ViewBag.Title = "Инфорум";
			ViewBag.JavascriptParams = new Dictionary<string, string>();
			ViewBag.Cities = new [] {"Борисоглебск", "Белгород"};
		}

		public void AddJavascriptParam(string name, string value)
		{
			ViewBag.JavascriptParams[name] = value;
		}

		public string GetJavascriptParam(string name)
		{
			string val = null;
			ViewBag.JavascriptParams.TryGetValue(name, out val);
			return val;
		}

		public virtual Employee GetCurrentEmployee()
		{
			if (Session == null || DbSession == null || Session["employee"] == null)
				return null;
			var employeeId = Convert.ToInt32(Session["employee"]);
			return DbSession.Query<Employee>().FirstOrDefault(k => k.Id == employeeId);
		}

		protected new virtual CustomPrincipal User
		{
			get { return HttpContext.User as CustomPrincipal; }
		}

		protected Client CurrentClient
		{
			get
			{
				if (User == null || DbSession == null || !DbSession.IsConnected) {
					return null;
				}
				int id;
				int.TryParse(User.Identity.Name, out id);
				return DbSession.Get<Client>(id);
			}
		}

		private static string userCity;

		public static string UserCity
		{
			get { return userCity; }
		}

		public Region CurrentRegion
		{
			get
			{
				return DbSession.Query<Region>().FirstOrDefault(r => r.Name == UserCity)
				       ?? DbSession.Query<Region>().FirstOrDefault();
			}
		}


		public HttpSessionStateBase HttpSession
		{
			get { return Session; }
		}

		public void SuccessMessage(string message)
		{
			SetCookie("SuccessMessage", message);
		}

		public void ErrorMessage(string message)
		{
			SetCookie("ErrorMessage", message);
		}

		public void WarningMessage(string message)
		{
			SetCookie("WarningMessage", message);
		}


		protected override void OnException(ExceptionContext filterContext)
		{
			//Формируем сообщение об ошибке
			var builder = CollectDebugInfo();
			var msg = filterContext.Exception.ToString();
			builder.Append(msg);
			EmailSender.SendError(builder.ToString());

			var showErrorPage = false;
			bool.TryParse(ConfigurationManager.AppSettings["ShowErrorPage"], out showErrorPage);
			DeleteCookie("SuccessMessage");

			// Иногда транзакции надо закрывать отдельно, так как метод OnResultExecuted не будет вызван
			if (DbSession.Transaction.IsActive) {
				EmailSender.SendEmail("asarychev@analit.net", "Rollback транзакции в OnException", "");
				DbSession.Transaction.Rollback();
			}
			if(DbSession.IsOpen)
				DbSession.Close();

			if (showErrorPage) {
				filterContext.Result = new RedirectToRouteResult(
					new RouteValueDictionary
					{ { "controller", "StaticContent" }, { "action", "Error" } });
				filterContext.ExceptionHandled = true;
			}

			log.ErrorFormat("{0} {1}", filterContext.Exception.Message, filterContext.Exception.StackTrace);
		}

		protected StringBuilder CollectDebugInfo()
		{
			var builder = new StringBuilder(1000);
			if(CurrentClient != null)
				builder.Append("Клиент: " + CurrentClient.Id + " \n ");

			//Не должно случаться, но добавил, так как боюсь циклических исключений
			//Получаем ip, ловим исключение, собираем инфо, получаем ip и так до бесконечности
			try {
				var tryClient = Client.GetClientForIp(Request.UserHostAddress, DbSession);
				if (tryClient != null)
					builder.Append("Клиент (по аренде): " + tryClient.Id + " \n ");
			}
			catch (Exception e) {
				builder.Append("Поймали циклическое исключение на попытке получить ip клиента \n ");
			}

			builder.Append("Дата: "+DateTime.Now+" \n ");
			builder.Append("Referrer: " + Request.UrlReferrer + " \n ");
			builder.Append("Query: " + Request.QueryString + " \n ");
			builder.Append("Ip: " + Request.UserHostAddress + " \n ");
			builder.Append("Форма:] \n ");
			foreach (var key in Request.Form.AllKeys)
			{
				builder.Append(key);
				builder.Append(" : ");
				builder.Append(Request.Form[key]);
				builder.Append("\n");
			}
			builder.Append("]");
			builder.Append("Запрос: " +Request.FilePath+ " : "+ Request.QueryString + " \n ");
			builder.Append("Браузер: " +Request.Browser.Browser + " \n ");
			builder.Append("Куки:[ \n ");
			foreach (var key in Request.Cookies.AllKeys)
			{
				builder.Append(key);
				builder.Append(" : ");
				builder.Append(GetCookie(key) ?? "");
				builder.Append("\n");
			}
			builder.Append("]");
			return builder;
		}

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			ViewBag.JavascriptParams["baseurl"] = Request.Url.GetLeftPart(UriPartial.Authority);
			var cookieCity = GetCookie("userCity");
			if (!string.IsNullOrEmpty(cookieCity)) {
				userCity = cookieCity;
			}
			base.OnActionExecuting(filterContext);
		}

		//Авторизация клиента из сети
		private void TryAuthorizeNetworkClient()
		{
			if(string.IsNullOrEmpty(Request.UserHostAddress))
				return;
			var endpoint = ClientEndpoint.GetEndpointForIp(Request.UserHostAddress,DbSession);
			if (endpoint != null && endpoint.Client.PhysicalClient != null) //Юриков авторизовывать не нужно
			{
				SetCookie("networkClient","true");
				this.Authenticate(ViewBag.ActionName, ViewBag.ControllerName, endpoint.Client.Id.ToString(), true);
			}
		}

		protected override void OnResultExecuting(ResultExecutingContext filterContext)
		{
			if (CurrentRegion != null) {
				ViewBag.RegionOfficePhoneNumber = CurrentRegion.RegionOfficePhoneNumber;
				ViewBag.CurrentRegion = CurrentRegion;
			}
			base.OnResultExecuting(filterContext);
		}

		protected override void OnActionExecuted(ActionExecutedContext filterContext)
		{
			base.OnActionExecuted(filterContext);
			ViewBag.ActionName = filterContext.RouteData.Values["action"].ToString();
			ViewBag.ControllerName = filterContext.RouteData.Values["controller"].ToString();
			//todo куда это девать?
			ViewBag.CallMeBackTicket = new CallMeBackTicket();

			ProcessRegionPanel();
			if (!CheckNetworkClient())
				RedirectToAction("Index", "Home");
			else
				ViewBag.NetworkClientFlag = true;
			if (CurrentClient != null) {
				var sb = new StringBuilder();
				sb.AppendFormat("Здравствуйте, {0} {1}. Ваш баланс: {2} руб.", CurrentClient.PhysicalClient.Name, 
						CurrentClient.PhysicalClient.Patronymic, CurrentClient.PhysicalClient.Balance);
				ViewBag.ClientInfo = sb.ToString();
			}
			else {
				TryAuthorizeNetworkClient();
			}
		}

		private bool CheckNetworkClient()
		{
			//если нет куки значит клиент не из нутри сети - все впроядке
			var cookie = GetCookie("networkClient");
			if (cookie == null)
				return true;

			//если нет текущего клиента то снимаем флаг клиента из интернета
			//больше ничего делать не надо - он может продолжить работку
			if (CurrentClient == null || string.IsNullOrEmpty(Request.UserHostAddress))
			{
				SetCookie("networkClient", null);
				EmailSender.SendEmail("asarychev@analit.net", "Снимаем куку залогиненного автоматически клиента так как он не найден: " + Request.UserHostAddress,CollectDebugInfo().ToString());
				return true;
			}

			//Выкидываем юрика
			if (CurrentClient.PhysicalClient == null) {
				SetCookie("networkClient", null);
				var msg = "Выкидываем юридического клиента: " + CurrentClient.Id;
				EmailSender.SendEmail("asarychev@analit.net", msg, CollectDebugInfo().ToString());
				FormsAuthentication.SignOut();
				return false;
			}

			var endpoint = ClientEndpoint.GetEndpointForIp(Request.UserHostAddress,DbSession);
			if (endpoint != null)
			{
				if (endpoint.Client.Id != CurrentClient.Id)
				{
					//Оказывается, что точка подключения принадлежит другому клиенту и текущий сидит в чужом ЛК
					//Снимаем куку и выкидываем клиента из ЛК
					//Возможно нужен еще редирект
					SetCookie("networkClient", null);
					var msg = "Выкидываем неправильно залогиненного клиента: " + Request.UserHostAddress + "," + endpoint.Client.Id + ", " + CurrentClient.Id;
					EmailSender.SendEmail("asarychev@analit.net", msg,CollectDebugInfo().ToString());
					FormsAuthentication.SignOut();
					return false;
				}
				//был найден клиент по точке подключения и текущий клиент. они совпадают, так что все путем
				return true;
			}

			//Получается текущий клиент есть, флаг того, что мы его авторизовали есть, но точки подключения у него нет. Как так? Выкидываем
			SetCookie("networkClient", null);
			var str = "Выкидываем залогиненного клиента без аренды: " + Request.UserHostAddress + ", " + CurrentClient.Id;
			EmailSender.SendEmail("asarychev@analit.net",str, CollectDebugInfo().ToString());
			FormsAuthentication.SignOut();
			return false;
		}

		private void ProcessCallMeBackTicket()
		{
			var binder = new EntityBinderAttribute("callMeBackTicket.Id", typeof(CallMeBackTicket));
			var callMeBackTicket = (CallMeBackTicket)binder.MapModel(Request);
			ViewBag.CallMeBackTicket = callMeBackTicket;
			if (Request.Params["callMeBackTicket.Name"] == null)
				return;
			callMeBackTicket.Client = CurrentClient;

			var errors = ValidationRunner.ValidateDeep(callMeBackTicket);
			if (errors.Length == 0) {
				DbSession.Save(callMeBackTicket);
				if(callMeBackTicket.Client != null) {
					var appeal = new Appeal("Клиент создал запрос на обратный звонок № " + callMeBackTicket.Id, 
						callMeBackTicket.Client, AppealType.FeedBack) {
							Employee = GetCurrentEmployee()
						};
					DbSession.Save(appeal);
				}

				SuccessMessage("Заявка отправлена. В течении дня вам перезвонят.");
				return;
			}
			
			if (GetJavascriptParam("CallMeBack") == null)
				AddJavascriptParam("CallMeBack", "1");
		}

		public void ProcessRegionPanel()
		{
			var cookieCity = GetCookie("userCity");
			if (User == null) {
				//Анонимный посетитель. Определяем город.
				if (!string.IsNullOrEmpty(cookieCity)) {
					userCity = cookieCity;
				}
				else {
					userCity = GetVisitorCityByGeoBase();
				}
			}
			else {
				if (!string.IsNullOrEmpty(cookieCity)) {
					userCity = cookieCity;
				}
				else {
					//Куков нет, пытаемся достать город из базы, иначе определяем по геобазе
					PhysicalClient user = null;
					int userId;
					int.TryParse(User.Identity.Name, out userId);
					if(userId != 0)
						user = DbSession.Query<PhysicalClient>().FirstOrDefault(k => k.Id == userId);
					if (user != null && user.Address != null) {
						userCity = user.Address.House.Street.Region.City.Name;
					}
					else {
						userCity = GetVisitorCityByGeoBase();
					}
				}
			}
			ViewBag.UserCityBelongsToUs = IsUserCityBelongsToUs(UserCity);
			ViewBag.UserCity = UserCity;
			ViewBag.UserRegion = DbSession.Query<Region>().FirstOrDefault(i => i.Name == UserCity);
			if (ViewBag.UserRegion == null)
				ViewBag.UserRegion = DbSession.Query<Region>().First();
		}

		private bool IsUserCityBelongsToUs(string city)
		{
			if (city != null) {
				var region = DbSession.Query<Region>().FirstOrDefault(i => i.Name.Contains(city) && i.City != null);
				if (region != null)
					return true;
			}
			return false;
		}

		private string GetVisitorCityByGeoBase()
		{
			var geoService = new IpGeoBase();
			IpAnswer geoAnswer;
			try {
				geoAnswer = geoService.GetInfo();
			}
			catch (Exception) {
				return null;
			}

			if (geoAnswer == null) return null;
			return geoAnswer.City;
		}

		protected List<TModel> GetList<TModel>()
		{
			var entities = DbSession.Query<TModel>().ToList();
			if (entities.Count == 0) {
				entities = new List<TModel>();
			}
			return entities;
		}

		protected string GetCookie(string cookieName)
		{
			var cookie = Request.Cookies.Get(cookieName);
			if (cookie == null || cookie.Value.Length <= 1) {
				return null;
			}

			var base64EncodedBytes = Convert.FromBase64String(cookie.Value);
			return Encoding.UTF8.GetString(base64EncodedBytes);
		}

		public void SetCookie(string name, string value)
		{
			if (value == null) {
				Response.Cookies.Add(new HttpCookie(name, "false") { Path = "/",Expires = DateTime.Now});
				return;
			}
			var plainTextBytes = Encoding.UTF8.GetBytes(value);
			var text = Convert.ToBase64String(plainTextBytes);
			Response.Cookies.Add(new HttpCookie(name, text) { Path = "/" });
		}

		public void DeleteCookie(string name)
		{
			Response.Cookies.Remove(name);
		}

		protected ActionResult Authenticate(string action, string controller, string username, bool shouldRemember,
			string userData = "")
		{
			var ticket = new FormsAuthenticationTicket(
				1,
				username,
				DateTime.Now,
				DateTime.Now.AddMinutes(FormsAuthentication.Timeout.TotalMinutes),
				shouldRemember,
				userData,
				FormsAuthentication.FormsCookiePath);
			var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket));
			if (shouldRemember)
				cookie.Expires = DateTime.Now.AddMinutes(FormsAuthentication.Timeout.TotalMinutes);
			Response.Cookies.Set(cookie);
			return RedirectToAction(action, controller);
		}

		public void SubmitCallMeBackTicket(string actionString, string controllerString)
		{
			ProcessCallMeBackTicket();
			ForwardToAction(controllerString, actionString, new object[0]);
		}

		public void ForwardToAction(string controllerString, string actionString, object[] parameters)
		{
			var type = Assembly.GetExecutingAssembly().GetTypes().First(t => t.Name == controllerString + "Controller");
			var module = new UrlRoutingModule();
			var col = module.RouteCollection;
			HttpContext.RewritePath("/" + controllerString + "/" + actionString);
			var fakeRouteData = col.GetRouteData(HttpContext);

			var ctxt = new RequestContext(ControllerContext.HttpContext, fakeRouteData);
			var factory = ControllerBuilder.Current.GetControllerFactory();
			var iController = factory.CreateController(ctxt, controllerString);

			var controller = iController as BaseController;
			controller.DbSession = DbSession;
			controller.ControllerContext = new ControllerContext(ctxt, this);

			var methodTypes = parameters.Select(parameter => parameter.GetType()).ToList();
			var actionMethod = type.GetMethod(actionString, methodTypes.ToArray());
			var actionResult = (ActionResult)actionMethod.Invoke(controller, parameters);

			controller.ViewBag.ActionName = actionString;
			controller.ViewBag.ControllerName = controllerString;

			actionResult.ExecuteResult(controller.ControllerContext);
		}
	}
}