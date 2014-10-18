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
			ViewBag.Cities = new string[]{"Воронеж","Борисоглебск","Москва"};
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

		protected string VisitorRegion
		{
			get { return HttpSession["VisitorRegion"].ToString(); }
			set { HttpSession["VisitorRegion"] = value; }
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

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);

			ProcessRegionPanel();
		}


		public void ProcessRegionPanel()
		{
			var cookie = Request.Cookies.Get("userCity");

			if (User != null && !User.Identity.IsAuthenticated) {
				//Анонимный посетитель. Определяем город.
				if (cookie != null) {
					VisitorRegion = cookie.Value;
					ViewBag.UserCity = cookie.Value;
				}
				else {
					var geoService = new IpGeoBase();
					IpAnswer geoAnswer;
					try {
						geoAnswer = geoService.GetInfo();
					}
					catch (WebException e) {
						geoAnswer = new IpAnswer { City = "Воронеж" };
					}
					VisitorRegion = geoAnswer.City;
					ViewBag.UserCity = geoAnswer.City;
				}
			}
			else {
				var user = new Client {
					City = "Воронеж"
				}; // TODO stub DBSession.Query<Client>().FirstOrDefault(k => k.Username == Client.Identity.Name);
				VisitorRegion = user.City;
				ViewBag.UserCity = user.City;
				//Добавляем куки, чтобы не показывать попап залогиненому пользователю
				if (cookie == null) {
					Response.Cookies.Add(new HttpCookie("userCity", user.City) { Path = "/" });
				}
				else {
					VisitorRegion = cookie.Value;
					ViewBag.UserCity = cookie.Value;
				}
			}
		}
	}
}