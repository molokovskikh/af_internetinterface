using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
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
	public abstract class BaseController : Controller
	{
		public ISession DbSession
		{
			get { return MvcApplication.SessionFactory.GetCurrentSession(); }
		}

		protected static readonly ILog log = LogManager.GetLogger(typeof (BaseController));

		protected ValidationRunner ValidationRunner;

		protected BaseController()
		{
			ValidationRunner = new ValidationRunner();
			ViewBag.Validation = ValidationRunner;
			ViewBag.Title = "Инфорум";
			ViewBag.JavascriptParams = new Dictionary<string, string>();
			ViewBag.Cities = new string[] {"Борисоглебск", "Белгород"};
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

		protected new virtual CustomPrincipal User
		{
			get { return HttpContext.User as CustomPrincipal; }
		}

		protected Employee CurrentEmployee
		{
			get { return DbSession.Query<Employee>().FirstOrDefault(k => k.Name == User.Identity.Name); }
		}

		protected Client CurrentClient
		{
			get
			{
				if (User == null) {
					return null;
				}
				int id = 0;
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
			bool showErrorPage = false;
			bool.TryParse(ConfigurationManager.AppSettings["ShowErrorPage"], out showErrorPage);
			DeleteCookie("SuccessMessage");
			if (showErrorPage) {
				filterContext.Result = new RedirectToRouteResult(
					new RouteValueDictionary
					{{"controller", "StaticContent"}, {"action", "Error"}});
				filterContext.ExceptionHandled = true;
			}
			log.ErrorFormat("{0} {1}", filterContext.Exception.Message, filterContext.Exception.StackTrace);
			EmailSender.SendError(filterContext.Exception.ToString());
		}

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			var cookieCity = GetCookie("userCity");
			if (!string.IsNullOrEmpty(cookieCity)) {
				userCity = cookieCity;
			}
			base.OnActionExecuting(filterContext);
		}

		protected override void OnResultExecuting(ResultExecutingContext filterContext)
		{
			if (CurrentRegion != null) {
				ViewBag.RegionOfficePhoneNumber = CurrentRegion.RegionOfficePhoneNumber;
			}
			base.OnResultExecuting(filterContext);
		}

		protected override void OnActionExecuted(ActionExecutedContext filterContext)
		{
			base.OnActionExecuted(filterContext);
			if (filterContext.Exception != null) {
			}
			ViewBag.ActionName = filterContext.RouteData.Values["action"].ToString();
			ViewBag.ControllerName = filterContext.RouteData.Values["controller"].ToString();
			ProcessCallMeBackTicket();
			ProcessRegionPanel();
			if (CurrentClient != null) {
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("Здравствуйте, {0}. Ваш баланс: {1} руб.", CurrentClient.PhysicalClient.Name,
					CurrentClient.PhysicalClient.Balance);
				ViewBag.ClientInfo = sb.ToString();
			}
		}

		private void ProcessCallMeBackTicket()
		{
			ViewBag.CallMeBackTicket = new CallMeBackTicket();
			var binder = new EntityBinderAttribute("callMeBackTicket.Id", typeof (CallMeBackTicket));
			CallMeBackTicket callMeBackTicket = (CallMeBackTicket) binder.MapModel(Request);
			if (Request.Params["callMeBackTicket.Name"] == null)
				return;
			var client = CurrentClient;
			if (client != null) {
				callMeBackTicket.Client = client;
			}
			var errors = ValidationRunner.ValidateDeep(callMeBackTicket);
			if (errors.Length == 0) {
				DbSession.Save(callMeBackTicket);
				SuccessMessage("Заявка отправлена. В течении для вам перезвонят.");
				return;
			}

			ViewBag.CallMeBackTicket = callMeBackTicket;
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
					int userId;
					int.TryParse(User.Identity.Name, out userId);
					var user = DbSession.Query<PhysicalClient>().FirstOrDefault(k => k.Id == userId);
					if (user != null) {
						userCity = user.Address.House.Street.Region.City.Name;
					}
					else {
						userCity = GetVisitorCityByGeoBase();
					}
				}
			}
			ViewBag.UserCityBelongsToUs = IsUserCityBelongsToUs(UserCity);
			ViewBag.UserCity = UserCity;
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
			catch (WebException e) {
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
			if (cookie == null) {
				return string.Empty;
			}
			var s = Uri.UnescapeDataString(cookie.Value);
			return HttpUtility.UrlDecode(s);
		}

		public void SetCookie(string name, string value)
		{
			Response.Cookies.Add(new HttpCookie(name, value) {Path = "/"});
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
	}
}