using System;
using System.Linq;
using System.Web.Mvc;
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
		public ActionResult Details()
		{
			return View();
		}

		/// <summary>
		/// Страница оплаты, если пользователь не авторизован
		/// </summary>
		public ActionResult Payment()
		{
			ViewBag.UserAccount = CurrentClient != null ? CurrentClient.Id.ToString() : "";
			ViewBag.PaymentSum = CurrentClient != null ? Math.Round(Math.Abs(CurrentClient.Balance - CurrentClient.PhysicalClient.Plan.Price)*100).ToString() : "";
			return View();
		}

		/// <summary>
		/// Списки подключенных домов
		/// </summary>
		public ActionResult ConnectedHousesLists()
		{
			var addresses = DbSession.Query<SwitchAddress>().Where(i => i.House != null).OrderBy(i => i.House.Street.Name).ToList();
			ViewBag.Addresses = addresses;

			var curentRegion = CurrentRegion;
			ViewBag.CurrentRegion = curentRegion.City.Name;

			var region = DbSession.Query<Region>().FirstOrDefault(s => s.Name == CurrentRegion.City.Name);
			var streets = DbSession.Query<Street>().Where(s => s.Region.City.Id == region.City.Id).ToList().OrderBy(o => o.Name);
			ViewBag.Streets = streets;

			return View();
		}
	}
}