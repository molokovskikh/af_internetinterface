using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
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
		public ISession DbSession
		{
			get { return MvcApplication.SessionFactory.GetCurrentSession(); }
		}

		protected List<City> Cities
		{
			get { return GetAllSafe<City>(); }
		}

		protected List<Street> Streets
		{
			get { return GetAllSafe<Street>(); }
		}

		protected IList<Region> Regions
		{
			get { return GetAllSafe<Region>(); }
		}

		protected IList<House> Houses
		{
			get { return GetAllSafe<House>(); }
		}

		protected IList<Address> Addresses
		{
			get { return GetAllSafe<Address>(); }
		}

		protected IList<SwitchAddress> SwitchAddresses
		{
			get { return GetAllSafe<SwitchAddress>(); }
		}

		protected IList<Switch> Switches
		{
			get { return GetAllSafe<Switch>(); }
		}

		protected IList<Plan> Plans
		{
			get { return GetAllSafe<Plan>(); }
		}

		protected ValidationRunner ValidationRunner;

		protected BaseController()
		{
			ValidationRunner = new ValidationRunner();
			ViewBag.Validation = ValidationRunner;
			ViewBag.JavascriptParams = new Dictionary<string, string>();
			ViewBag.Cities = new string[] { "Воронеж", "Борисоглебск", "Белгород" };
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

		protected Client CurrentClient
		{
			get
			{
				if (User == null) {
					return null;
				}
				int id = 0;
				int.TryParse(User.Identity.Name, out id);
				return DbSession.Query<Client>().FirstOrDefault(k => k.Id == id );
			}
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
		/*	if (filterContext.ExceptionHandled) {
				return;
			}
			  filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                    {{"controller", "Home"}, {"action", "Index"}});
			filterContext.ExceptionHandled = true;*/
		}

		protected override void OnActionExecuted(ActionExecutedContext filterContext)
		{
			base.OnActionExecuted(filterContext);
			ProcessRegionPanel();
			if (CurrentClient != null) {
				ViewBag.ClientInfo = string.Format("{0}, Баланс: {1} ", CurrentClient.PhysicalClient.FullName, CurrentClient.PhysicalClient.Balance);
			}
		}


		public void ProcessRegionPanel()
		{
			var cookieCity = GetCookie("userCity");
			if (User == null) {
				//Анонимный посетитель. Определяем город.
				if (!string.IsNullOrEmpty(cookieCity)) {
					userCity = cookieCity;
					ViewBag.UserCity = cookieCity;
				}
				else {
					GetVisitorCityByGeoBase();
				}
			}
			else {
				if (!string.IsNullOrEmpty(cookieCity)) {
					userCity = cookieCity;
					ViewBag.UserCity = cookieCity;
				}
				else {
					//Куков нет, пытаемся достать город из базы, иначе определяем по геобазе
					var user = DbSession.Query<PhysicalClient>().FirstOrDefault(k => k.Id == Convert.ToInt32(User.Identity.Name) );
					if (user != null) {
						userCity = user.Address.House.Street.Region.City.Name;
						ViewBag.UserCity = user.Address.House.Street.Region.City.Name;
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
				geoAnswer = new IpAnswer { City = "Борисоглебск" };
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

		protected string GetCookie(string cookieName)
		{
			var cookie = Request.Cookies.Get(cookieName);
			if (cookie == null) {
				return string.Empty;
			}
			var s = Uri.UnescapeDataString(cookie.Value);
			return HttpUtility.UrlDecode(s);
		}

		protected void SetCookie(string name, string value)
		{
			Response.Cookies.Add(new HttpCookie(name, value) { Path = "/" });
		}

		public static Encoding DetectEncoding(String fileName, out String contents)
		{
			// open the file with the stream-reader:
			using (StreamReader reader = new StreamReader(fileName, true)) {
				// read the contents of the file into a string
				contents = reader.ReadToEnd();

				// return the encoding.
				return reader.CurrentEncoding;
			}
		}
	}
}