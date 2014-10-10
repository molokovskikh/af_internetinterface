using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using Inforoom2.Components;
using Inforoom2.Helpers.IpGeoBaseNET;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Engine;


namespace Inforoom2.Controllers
{
	/// <summary>
	/// Базовый контроллер от которого все наследуются
	/// </summary>
	public abstract class BaseController : Controller
	{
		protected ISession DBSession;
		protected ValidationRunner ValidationRunner;

		protected string VisitorRegion
		{
			get { return HttpContext.Session["VisitorRegion"].ToString(); }
			set { HttpContext.Session["VisitorRegion"] = value; }
		}

		public BaseController()
		{
			DBSession = MvcApplication.OpenSession();
			ValidationRunner = new ValidationRunner();
			ViewBag.Validation = ValidationRunner;
		}

		public void SuccessMessage(string message)
		{
			Response.Cookies.Add(new HttpCookie("SuccessMessage", message) {Path = "/"});
		}

		public void ErrorMessage(string message)
		{
			Response.Cookies.Add(new HttpCookie("ErrorMessage", message) {Path = "/"});
		}

		public void WarningMessage(string message)
		{
			Response.Cookies.Add(new HttpCookie("WarningMessage", message) {Path = "/"});
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
						geoAnswer = new IpAnswer {City = "Воронеж"};
					}
					VisitorRegion = geoAnswer.City;
					ViewBag.UserCity = geoAnswer.City;
				}
			}
			else {
				var user = new User {
					City = "Воронеж"
				}; // TODO stub DBSession.Query<User>().FirstOrDefault(k => k.Username == User.Identity.Name);
				VisitorRegion = user.City;
				ViewBag.UserCity = user.City;
				//Добавляем куки, чтобы не показывать попап залогиненому пользователю
				if (cookie == null) {
					Response.Cookies.Add(new HttpCookie("userCity", user.City) {Path = "/"});
				}
				else {
					VisitorRegion = cookie.Value;
					ViewBag.UserCity = cookie.Value;
				}
			}
		}
	}
}