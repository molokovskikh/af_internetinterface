using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class ClientRequestController : Inforoom2Controller
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
		public ActionResult Index([EntityBinder] ClientRequest clientRequest)
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
			ViewBag.IsCityValidated = false;
			ViewBag.IsStreetValidated = false;
			ViewBag.IsHouseValidated = false;
			ViewBag.ClientRequest = clientRequest;
			return View("Index");
		}

		[HttpPost]
		public JsonResult CheckForUnusualAddress(string city, string street, string house, string address)
		{
			object result = new { };
			var dbCity = DbSession.Query<City>().FirstOrDefault(i => i.Name.ToLower().Contains(city.ToLower()));
			if (dbCity == null || string.IsNullOrEmpty(street))
				return Json(result);

			//Псевдоним улицы приоритетнее улицы
			var dbStreetAlias = StreetAlias.FindAlias(address, DbSession);
			Street dbStreet;
			if (dbStreetAlias == null)
				dbStreet = DbSession.Query<Street>().FirstOrDefault(i => i.Region.City == dbCity && i.Name.ToLower().Contains(street.ToLower()));
			else
				dbStreet = dbStreetAlias.Street;

			if (dbStreet == null)
				return Json(result);
			house = house ?? "";
			var dbHouse = DbSession.Query<House>().FirstOrDefault(i => i.Street == dbStreet && i.Number.ToLower().Contains(house.ToLower()));
			if (dbHouse != null && !string.IsNullOrEmpty(house)) {
				//Если дом найден то ищем, имеется ли там коммутатор
				var addr = DbSession.Query<SwitchAddress>().FirstOrDefault(i => i.House == dbHouse);
				var availible = addr != null;
				result = new { streetAlias = dbStreetAlias != null, city = dbCity.Name, street = dbStreet.Name, house = dbHouse.Number, geomark = dbHouse.Geomark, available = availible };
			}
			else
				result = new { streetAlias = dbStreetAlias != null, city = dbCity.Name, street = dbStreet.Name, geomark = dbStreet.Geomark };

			return Json(result);
		}

		private void InitClientRequest(Plan plan = null, string city = "", string street = "", string house = "")
		{
			ViewBag.IsRedirected = false;
			ViewBag.IsCityValidated = false;
			ViewBag.IsStreetValidated = false;
			ViewBag.IsHouseValidated = false;
			var clientRequest = new ClientRequest();

			if (!string.IsNullOrEmpty(UserCity)) {
				clientRequest.City = UserCity;
			}

			if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(street) && !string.IsNullOrEmpty(house)) {
				clientRequest.Street = street;
				clientRequest.City = city;
				ViewBag.IsCityValidated = true;
				ViewBag.IsStreetValidated = true;

				int housen = 0;
				int.TryParse(house, out housen);
				if (housen != 0) {
					ViewBag.IsHouseValidated = true;
					clientRequest.HouseNumber = housen;
				}
			}

			if (plan != null) {
				clientRequest.Plan = plan;
				ViewBag.IsRedirected = true;
			}
			ViewBag.ClientRequest = clientRequest;
			InitRequestPlans();
		}

		private List<Plan> InitRequestPlans()
		{
			var plans = DbSession.Query<Plan>().Where(p => !p.IsArchived).ToList();
			ViewBag.Plans = plans;
			return plans;
		}

		protected Address GetAddressByYandexData(ClientRequest clientRequest)
		{
			var city = GetList<City>().FirstOrDefault(c => c.Name.Equals(clientRequest.YandexCity, StringComparison.InvariantCultureIgnoreCase));

			if (city == null || !clientRequest.IsYandexAddressValid()) {
				return null;
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
			                                                     && a.Entrance == clientRequest.Entrance.ToString()
			                                                     && a.Floor == clientRequest.Floor
			                                                     && a.Apartment == clientRequest.Apartment.ToString());

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

		public ActionResult RequestFromConnectionCheck(string city, string street, string house)
		{
			InitClientRequest(null, city, street, house);
			return View("Index");
		}
	}
}