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
			ViewBag.IsRedirected = false;
			ViewBag.ClientRequest = clientRequest;
			return View("Index");
		}


		private void InitClientRequest(Plan plan = null, string street = "", string house = "")
		{
			var clientRequest = new ClientRequest();

			if (!string.IsNullOrEmpty(UserCity)) {
				clientRequest.City = UserCity;
			}

			if (!string.IsNullOrEmpty(street) && !string.IsNullOrEmpty(house)) {
				clientRequest.Street = street;
				int housen = 0;
				int.TryParse(house, out housen);
				if (housen != 0)
					clientRequest.HouseNumber = housen;
			}
			ViewBag.IsRedirected = false;
			if (plan != null) {
				clientRequest.Plan = plan;
				ViewBag.IsRedirected = true;
			}
			ViewBag.ClientRequest = clientRequest;
			InitRequestPlans();
		}

		private List<Plan> InitRequestPlans()
		{
			var plans = DbSession.Query<Plan>().Where(p => !p.IsArchived && !p.IsServicePlan).ToList();
			ViewBag.Plans = plans;
			return plans;
		}

		protected Address GetAddressByYandexData(ClientRequest clientRequest)
		{
			var city = GetList<City>().FirstOrDefault(c => c.Name.Equals(clientRequest.YandexCity, StringComparison.InvariantCultureIgnoreCase));

			if (city == null || !clientRequest.IsYandexAddressValid()) {
				var badAddress = new Address { IsCorrectAddress = false };
				return badAddress;
			}
			var region = GetList<Region>().FirstOrDefault(r => r.City == city);

			var street = GetList<Street>().FirstOrDefault(s => s.Name.Equals(clientRequest.YandexStreet, StringComparison.InvariantCultureIgnoreCase)
			                                                   && s.Region.Equals(region));

			if (street == null) {
				street = new Street(clientRequest.YandexStreet);
			}

			var house = GetList<House>().FirstOrDefault(h => h.Number.Equals(clientRequest.YandexHouse, StringComparison.InvariantCultureIgnoreCase)
			                                                 && h.Street.Name.Equals(clientRequest.YandexStreet, StringComparison.InvariantCultureIgnoreCase)
			                                                 && h.Street.Region.Equals(region));

			if (house == null) {
				house = new House(clientRequest.YandexHouse);
			}
			var address = GetList<Address>().FirstOrDefault(a => a.IsCorrectAddress
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

		public ActionResult RequestFromTariff(int? id)
		{
			if (id != null) {
				var plan = DbSession.Get<Plan>(id);
				InitClientRequest(plan);
			}
			else {
				InitClientRequest();
			}
			return View("Index");
		}

		public ActionResult RequestFromConnectionCheck(string street, string house)
		{
			InitClientRequest(null, street, house);
			return View("Index");
		}
	}
}