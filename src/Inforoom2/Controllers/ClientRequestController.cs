﻿using System;
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
			var errors = ValidationRunner.ValidateDeep(clientRequest);

			if (errors.Length == 0 && clientRequest.IsContractAccepted) {
				clientRequest.Address = GetAddressByYandexData(clientRequest);
				DbSession.Save(clientRequest);
				SuccessMessage(string.Format("Спасибо, Ваша заявка принята. Номер заявки {0}", clientRequest.Id));
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
			var plans = DbSession.Query<Plan>().Where(p => !p.IsArchived && !p.IsServicePlan && !p.Hidden).ToList();
			ViewBag.Plans = plans;
			return plans;
		}


		protected Address GetAddressByYandexData(ClientRequest clientRequest)
		{
			var city = Cities.FirstOrDefault(c => c.Name.Equals(clientRequest.YandexCity, StringComparison.InvariantCultureIgnoreCase));

			if (city == null || !clientRequest.IsYandexAddressValid()) {
				var badAddress = new Address { IsCorrectAddress = false };
				return badAddress;
			}
			var region = Regions.FirstOrDefault(r => r.City == city);

			var street = Streets.FirstOrDefault(s => s.Name.Equals(clientRequest.YandexStreet, StringComparison.InvariantCultureIgnoreCase)
			                                         && s.Region.Equals(region));

			if (street == null) {
				street = new Street(clientRequest.YandexStreet);
			}

			var house = Houses.FirstOrDefault(h => h.Number.Equals(clientRequest.YandexHouse, StringComparison.InvariantCultureIgnoreCase)
			                                       && h.Street.Name.Equals(clientRequest.YandexStreet, StringComparison.InvariantCultureIgnoreCase)
			                                       && h.Street.Region.Equals(region));

			if (house == null) {
				house = new House(clientRequest.YandexHouse);
			}
			var address = Addresses.FirstOrDefault(a => a.IsCorrectAddress
			                                            && a.House.Equals(house)
			                                            && a.House.Street.Equals(street)
			                                            && a.House.Street.Region.Equals(region)
			                                            && a.Entrance == clientRequest.Entrance
			                                            && a.Floor == clientRequest.Floor
			                                            && a.Apartment == clientRequest.Apartment);

			if (address == null) {
				address = new Address();
				address.House = house;
				address.Apartment = clientRequest.Apartment;
				address.Floor = clientRequest.Floor;
				address.Entrance = clientRequest.Entrance;
				address.House.Street = street;
				address.House.Street.Region = region;
				address.IsCorrectAddress = true;
			}
			return address;
		}

		public ActionResult RequestFromTariff(string planName)
		{
			var plan = DbSession.Query<Plan>().FirstOrDefault(p => p.Name == planName);
			InitClientRequest(plan);
			return View("Index");
		}
	}
}