using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class ClientRequestController : BaseController
	{
		/// <summary>
		/// Отображает  форму новой заявки
		/// </summary>
		public ActionResult Index()
		{
			var clientRequest = new ClientRequest();
			clientRequest.Address = new Address();
			clientRequest.Address.House = new House();
			clientRequest.Address.House.Street = new Street();
			clientRequest.Address.House.Street.Region = new Region();
			clientRequest.Address.House.Street.Region.City = new City();
			if (!string.IsNullOrEmpty(UserCity)) {
				clientRequest.Address.House.Street.Region.City.Name = UserCity;
			}
			ViewBag.ClientRequest = clientRequest;
			SetPlans();
			return View();
		}

		private List<Plan> SetPlans()
		{
			var tariffs = DbSession.Query<Plan>();
			List<SelectListItem> selectListItems = tariffs.Select(k => new SelectListItem {
				Value = k.Name,
				Text = k.Name
			}).ToList();
			ViewBag.Tariffs = selectListItems;
			return tariffs.ToList();
		}

		[HttpPost]
		public ActionResult Create(ClientRequest clientRequest)
		{
			var tariff = SetPlans().FirstOrDefault(k => k.Name == clientRequest.Plan.Name);
			clientRequest.Plan = tariff;
			var errors = ValidationRunner.ValidateDeep(clientRequest);
			if (errors.Length == 0) {
				//TODO Нужно придумать что делать с заполненой заявкой
				DbSession.Save(clientRequest);
				
				return RedirectToAction("Index", "Home");
			}
			ViewBag.ClientRequest = clientRequest;
			return View("Index");
		}

		public bool CheckSwitchAddress(string city, string street, string house)
		{
			string houseNumber = string.Empty;
			string housing = string.Empty;
			AddressHelper.SplitHouseAndHousing(house, ref houseNumber, ref housing);

			if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(street) || string.IsNullOrEmpty(houseNumber)) {
				return false;
			}

			var switchAddress = DbSession.Query<SwitchAddress>()
				.FirstOrDefault((sa => (sa.House.Street.Region.City.Name.ToLower() == city
				                        && street.Contains(sa.House.Street.Name.ToLower())
				                        && sa.House.Number.ToLower() == houseNumber
				                        && sa.House.Housing.ToLower() == housing)
					//проверка частного сектора (частный сектор содержит только улицу) 
				                       || (sa.Street.Region.City.Name.ToLower() == city && street.Contains(sa.Street.Name.ToLower()))));

			return switchAddress != null;
		}
	}
}