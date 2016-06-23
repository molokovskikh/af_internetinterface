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
			var region = DbSession.Query<Region>().FirstOrDefault(s => s.Name == CurrentRegion.City.Name);
			var curentRegion = CurrentRegion;
			ViewBag.CurrentRegion = curentRegion.City.Name;

			var houses = DbSession.Query<Client>().Where(s =>
				((s.PhysicalClient.Address.House.Region != null && s.PhysicalClient.Address.House.Region.City.Id == region.City.Id)
				 || (s.PhysicalClient.Address.House.Street.Region.City.Id == region.City.Id && s.PhysicalClient.Address.House.Region == null))).ToList()
				.Where(s => s.Status.Type != StatusType.NoWorked && s.Status.Type != StatusType.Dissolved)
				.Select(s => s.Address.House)
				.Distinct().ToList();

			var streets = houses.Select(s => s.Street).ToList().Select(s => s.Name).Distinct().OrderBy(s => s).ToList();

			ViewBag.Houses = houses;
			ViewBag.Streets = streets;

			return View();
		}
	}
}