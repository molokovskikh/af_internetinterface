using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Common.MySql;
using Inforoom2.Components;
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
			InitClientRequest();
			return View();
		}

		[HttpPost]
		public ActionResult Index(ClientRequest clientRequest)
		{
			var tariff = InitRequestPlans().FirstOrDefault(k => k.Id == clientRequest.Plan.Id);
			clientRequest.Plan = tariff;
			clientRequest.ActionDate = clientRequest.RegDate = DateTime.Now;
			//ConvertRequestToOldModel(clientRequest);
			var errors = ValidationRunner.ValidateDeep(clientRequest);
			if (errors.Length == 0 && clientRequest.IsContractAccepted) {
				clientRequest.Address = null;
				DbSession.Save(clientRequest);
				SuccessMessage(string.Format("Спасибо, Ваша заявка принята. Номер заявки {0}", clientRequest.Id)) ;
				return RedirectToAction("Index", "Home");
			}
			if (!clientRequest.IsContractAccepted) {
				ErrorMessage("Пожалуйста, подтвердите, что Вы согласны с договором-офертой");
			}
			ViewBag.ClientRequest = clientRequest;
			return View("Index");
		}

		private void InitClientRequest(Plan plan = null)
		{
			var clientRequest = new ClientRequest();
			/*
			clientRequest.Address = new Address();
			clientRequest.Address.House = new House();
			clientRequest.Address.House.Street = new Street();
			clientRequest.Address.House.Street.Region = new Region();
			clientRequest.Address.House.Street.Region.City = new City();*/
			if (!string.IsNullOrEmpty(UserCity)) {
				clientRequest.City = UserCity;
			}
			if (plan != null) {
				clientRequest.Plan = plan;
			}
			ViewBag.ClientRequest = clientRequest;
			InitRequestPlans();
		}

		private List<Plan> InitRequestPlans()
		{
			var plans = DbSession.Query<Plan>().Where(p=>!p.IsArchived && !p.IsServicePlan && !p.Hidden).ToList();
			ViewBag.Plans = plans;
			return plans;
		}

		private void ConvertRequestToOldModel(ClientRequest clientRequest)
		{
			clientRequest.City = clientRequest.Address.House.Street.Region.City.Name;
			clientRequest.Street = clientRequest.Address.House.Street.Name;
			int house;
			int.TryParse(clientRequest.Address.House.Number, out house);
			clientRequest.HouseNumber = house;
			clientRequest.Housing = clientRequest.Address.House.Housing;
			clientRequest.Entrance = clientRequest.Address.Entrance;
			clientRequest.Floor = clientRequest.Address.Floor;
			clientRequest.Apartment = clientRequest.Address.Apartment;
			//TODO необходимо реализовать единый механизм управления адресом
			
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
				                       ||
				                       (sa.Street.Region.City.Name.ToLower() == city && street.Contains(sa.Street.Name.ToLower()))));

			return switchAddress != null;
		}

		public ActionResult RequestFromTariff(string planName)
		{
			var plan = DbSession.Query<Plan>().FirstOrDefault(p => p.Name == planName);
			InitClientRequest(plan);
			return View("Index");
		}
	}
}