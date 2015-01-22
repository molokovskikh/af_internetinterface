using System;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class IsMyHouseConnectedController : BaseController	
	{
		public ActionResult Index()
		{
			var address = new Address { House = new House { Street = new Street { Region = new Region() } } };
			var clientRequest = new ClientRequest();
			clientRequest.Address = address;
			ViewBag.Regions = DbSession.Query<Region>().Where(i => i.City != null).ToList();
			ViewBag.ClientRequest = clientRequest;
			ViewBag.IsConnected = null;
			return View();
		}

		[HttpPost]
		public ActionResult Index(ClientRequest clientRequest)
		{
			var switchAddress = DbSession.Query<SwitchAddress>();
			ViewBag.IsConnected = false;
			if (clientRequest.IsAddressConnected(switchAddress.ToList())) {
				ViewBag.IsConnected = true;
			}
			var regions =GetList<Region>();
			ViewBag.Regions = regions;
			ViewBag.ClientRequest = clientRequest;
			var selectedRegion = regions.FirstOrDefault(r => r.Name.Equals(clientRequest.YandexCity, StringComparison.InvariantCultureIgnoreCase));
			if(selectedRegion != null)
				ViewBag.SelectedRegionOfficePhoneNumber = selectedRegion.RegionOfficePhoneNumber;
			else
				ViewBag.SelectedRegionOfficePhoneNumber = regions.First().RegionOfficePhoneNumber;
			return View();
		}

	}
}