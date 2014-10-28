using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Filters;
using System.Web.Security;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
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
		public ISession DbSession { get; set; }
		protected ValidationRunner ValidationRunner;

		protected BaseController()
		{
			ValidationRunner = new ValidationRunner();
			ViewBag.Validation = ValidationRunner;
			ViewBag.JavascriptParams = new Dictionary<string, string>();
			ViewBag.Cities = new string[] { "Воронеж", "Борисоглебск", "Москва" };
		}

		public void AddJavascriptParam(string name, string value)
		{
			ViewBag.JavascriptParams[name] = value;
		}

		protected new virtual CustomPrincipal User
		{
			get { return HttpContext.User as CustomPrincipal; }
		}

		protected Employee CurrentEmployee
		{
			get { return DbSession.Query<Employee>().FirstOrDefault(k => k.Username == User.Identity.Name); }
		}

		private static string userCity;

		public static string UserCity
		{
			get { return userCity; }
		}


		public HttpSessionStateBase HttpSession
		{
			get { return Session; }
		}

		public void SuccessMessage(string message)
		{
			Response.Cookies.Add(new HttpCookie("SuccessMessage", message) { Path = "/" });
		}

		public void ErrorMessage(string message)
		{
			Response.Cookies.Add(new HttpCookie("ErrorMessage", message) { Path = "/" });
		}

		public void WarningMessage(string message)
		{
			Response.Cookies.Add(new HttpCookie("WarningMessage", message) { Path = "/" });
		}

		protected override void OnException(ExceptionContext filterContext)
		{
			if (filterContext.ExceptionHandled) {
				return;
			}
			filterContext.Result = new ViewResult {
				ViewName ="" //TODO some error view
			};
			filterContext.ExceptionHandled = true;
		}

		protected override void OnActionExecuted(ActionExecutedContext filterContext)
		{
			base.OnActionExecuted(filterContext);
			ProcessRegionPanel();
		}


		public void ProcessRegionPanel()
		{
			var cookie = Request.Cookies.Get("userCity");

			if (User == null) {
				//Анонимный посетитель. Определяем город.
				if (cookie != null) {
					userCity = cookie.Value;
					ViewBag.UserCity = cookie.Value;
				}
				else {
					GetVisitorCityByGeoBase();
				}
			}
			else {
				if (cookie != null) {
					userCity = cookie.Value;
					ViewBag.UserCity = cookie.Value;
				}
				else {
					//Куков нет, пытаемся достать город из базы, иначе определяем по геобазе
					var user = DbSession.Query<Client>().FirstOrDefault(k => k.Username == User.Identity.Name);
					if (user != null) {
						userCity = user.City;
						ViewBag.UserCity = user.City;
					}
					else {
						GetVisitorCityByGeoBase();
					}
				}
			}
		}

		private void GetVisitorCityByGeoBase()
		{
			var geoService = new IpGeoBase();
			IpAnswer geoAnswer;
			try {
				geoAnswer = geoService.GetInfo();
			}
			catch (WebException e) {
				geoAnswer = new IpAnswer { City = "Воронеж" };
			}
			userCity = geoAnswer.City;
			ViewBag.UserCity = geoAnswer.City;
		}

		protected List<TModel> GetAllSafe<TModel>()
		{
			var entities = DbSession.Query<TModel>().ToList();
				if (entities.Count == 0) {
					entities = new List<TModel>();
				}
				return entities;
		}
	}
}