using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using Inforoom2.Models;
using NHibernate.Driver;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Отображает страницы оферты
	/// </summary>
	public class AboutController : Inforoom2Controller
	{
		/// <summary>
		/// Страница о компании
		/// </summary>
		[OutputCache(Duration = 600, Location = OutputCacheLocation.Server, VaryByCustom = "Cookies")]
		public ActionResult Index()
		{
			AddJavascriptParam("office_region", CurrentRegion.Name);
			AddJavascriptParam("office_address", CurrentRegion.Name + ", " + CurrentRegion.OfficeAddress);
			var geoCoords = CurrentRegion.OfficeGeomark.Split(',');
			AddJavascriptParam("office_geoX", geoCoords[0]);
			AddJavascriptParam("office_geoY", geoCoords[1]);
			return View();
		}

		/// <summary>
		/// Реквизиты компании
		/// </summary>
		[OutputCache(Duration = 600, Location = OutputCacheLocation.Server, VaryByCustom = "Cookies")]
		public ActionResult Details()
		{
			return View();
		}

		/// <summary>
		/// Страница оплаты, если пользователь не авторизован
		/// </summary>
		[OutputCache(Duration = 600, Location = OutputCacheLocation.Server, VaryByCustom = "Cookies")]
		public ActionResult Payment()
		{
			ViewBag.UserAccount = CurrentClient != null ? CurrentClient.Id.ToString() : "";
			ViewBag.PaymentSum = CurrentClient != null ? Math.Round(Math.Abs(CurrentClient.Balance - CurrentClient.PhysicalClient.Plan.Price) * 100).ToString() : "";
			return View();
		}

		/// <summary>
		/// Списки подключенных домов
		/// </summary>
		[OutputCache(Duration = 600, Location = OutputCacheLocation.Server, VaryByCustom = "Cookies")]
		public ActionResult ConnectedHousesLists()
		{
			var curentRegion = CurrentRegion;
			ViewBag.CurrentRegion = curentRegion.City.Name;
			ViewBag.ConnectedStreet =
				DbSession.Query<ConnectedStreet>()
					.Where(s => !s.Disabled && s.Region.Id == curentRegion.Id)
					.OrderBy(s => s.Name.Trim())
					.ToList();
			return View();
		}
	}
}