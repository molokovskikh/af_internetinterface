using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using Switch = Inforoom2.Models.Switch;

namespace InforoomControlPanel.Controllers
{
	public class AddressController : AdminController
	{
		public ActionResult DeleteHouse(int? id)
		{
			var city = DbSession.Get<City>(id);
			SuccessMessage("Улица успешно удален");
			DbSession.Delete(city);
			return RedirectToAction("HouseList");
		}

		public ActionResult DeleteStreet(int id)
		{
			var street = DbSession.Get<Street>(id);
			SuccessMessage("Улица успешно удалена");
			DbSession.Delete(street);
			return RedirectToAction("StreetList");
		}

		/// <summary>
		/// Не реализована страница адресов пользователей 30.01.15
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public ActionResult DeleteAddress(int id)
		{
			var address = DbSession.Get<Address>(id);
			SuccessMessage("Адрес успешно удален");
			DbSession.Delete(address);
			return RedirectToAction("AddressList");
		}

		public ActionResult DeleteSwitchAddress(int id)
		{
			var switchAddress = DbSession.Get<SwitchAddress>(id);
			SuccessMessage("Адрес успешно удален");
			DbSession.Delete(switchAddress);
			return RedirectToAction("SwitchAddressList");
		}


		public ActionResult SwitchAddressList()
		{
			ViewBag.Addresses = GetList<SwitchAddress>();
			return View();
		}

		[HttpPost]
		public ActionResult CreateSwitchAddress([EntityBinder] SwitchAddress SwitchAddress, bool? noEntrances)
		{
			if (noEntrances.HasValue) {
				SwitchAddress.Entrance = null;
			}
			var errors = ValidationRunner.ValidateDeep(SwitchAddress);
			if (errors.Length == 0) {
				DbSession.Save(SwitchAddress);
				SuccessMessage("Адрес коммутатора успешно добавлен");
				return RedirectToAction("SwitchAddressList");
			}

			CreateSwitchAddress(SwitchAddress.House.Street.Region.Id, SwitchAddress.House.Street.Id);
			ViewBag.SwitchAddress = SwitchAddress;
			ViewBag.House = SwitchAddress.House;
			return View("CreateSwitchAddress");
		}


		public ActionResult CreateSwitchAddress(int regionId = 0, int streetId = 0, int id = 0)
		{
			var SwitchAddress = new SwitchAddress();
			if (regionId > 0)
				ViewBag.Region = DbSession.Get<Region>(regionId);

			if (streetId > 0)
				ViewBag.Street = DbSession.Get<Street>(streetId);
			if (id > 0)
				ViewBag.Switch = DbSession.Get<NetworkNode>(id);

			var regions = DbSession.Query<Region>().ToList();
			var streets = DbSession.Query<Street>().ToList();
			var houses = DbSession.Query<House>().ToList();
			var NetworkNodes = DbSession.Query<NetworkNode>().OrderBy(i => i.Name).ToList();
			ViewBag.Streets = streets;
			ViewBag.Houses = houses;
			ViewBag.Regions = regions;
			ViewBag.NetworkNodes = NetworkNodes;
			ViewBag.SwitchAddress = SwitchAddress;
			return View("CreateSwitchAddress");
		}

		[HttpPost]
		public ActionResult EditSwitchAddress([EntityBinder] SwitchAddress SwitchAddress, bool? noEntrances)
		{
			if (noEntrances.HasValue) {
				SwitchAddress.Entrance = null;
			}
			var errors = ValidationRunner.Validate(SwitchAddress);
			if (errors.Length == 0) {
				DbSession.Save(SwitchAddress);
				SuccessMessage("Адрес коммутатора успешно изменен");
				return RedirectToAction("SwitchAddressList");
			}

			CreateSwitchAddress(SwitchAddress.House.Street.Region.Id, SwitchAddress.House.Street.Id);
			ViewBag.SwitchAddress = SwitchAddress;
			ViewBag.House = SwitchAddress.House;
			return View("CreateSwitchAddress");
		}

		public ActionResult EditSwitchAddress(int id)
		{
			var SwitchAddress = DbSession.Get<SwitchAddress>(id);
			CreateSwitchAddress();

			ViewBag.Region = SwitchAddress.House.Street.Region;
			ViewBag.Street = SwitchAddress.House.Street;
			ViewBag.NetworkNode = SwitchAddress.NetworkNode;
			ViewBag.House = SwitchAddress.House;
			ViewBag.SwitchAddress = SwitchAddress;
			return View("CreateSwitchAddress");
		}

		public ActionResult CityList()
		{
			var cities = DbSession.Query<City>().ToList();
			ViewBag.Cities = cities;
			return View();
		}

		public ActionResult RegionList()
		{
			var regions = DbSession.Query<Region>().ToList();
			ViewBag.Regions = regions;
			return View();
		}

		public ActionResult StreetList()
		{
			var streets = DbSession.Query<Street>().ToList();
			ViewBag.Streets = streets;
			return View();
		}

		public ActionResult CreateStreet(int regionId = 0)
		{
			var Street = new Street();
			Street.Confirmed = true;
			if (regionId > 0)
				ViewBag.Region = DbSession.Get<Region>(regionId);

			var regions = DbSession.Query<Region>().ToList();
			ViewBag.Regions = regions;
			ViewBag.Street = Street;
			return View("CreateStreet");
		}

		[HttpPost]
		public ActionResult CreateStreet([EntityBinder] Street Street, string yandexStreet, string yandexPosition)
		{
			//По возможности используем формализацию яндекса
			if (Street.Confirmed) {
				Street.Name = yandexStreet;
				Street.Geomark = yandexPosition;
			}

			var errors = ValidationRunner.ValidateDeep(Street);
			if (errors.Length == 0) {
				DbSession.Save(Street);
				SuccessMessage("Улица успешно добавлена");
				return RedirectToAction("StreetList");
			}

			CreateStreet();
			ViewBag.Street = Street;
			return View("CreateStreet");
		}

		public ActionResult EditStreet(int id)
		{
			var Street = DbSession.Get<Street>(id);
			var regions = DbSession.Query<Region>().ToList();
			ViewBag.Region = Street.Region;
			ViewBag.Regions = regions;
			ViewBag.Street = Street;
			return View("CreateStreet");
		}

		[HttpPost]
		public ActionResult EditStreet([EntityBinder] Street Street, string yandexStreet, string yandexPosition)
		{
			//По возможности используем формализацию яндекса
			if (Street.Confirmed) {
				Street.Name = yandexStreet;
				Street.Geomark = yandexPosition;
			}

			var errors = ValidationRunner.ValidateDeep(Street);
			if (errors.Length == 0) {
				DbSession.Save(Street);
				SuccessMessage("Улица успешно изменена");
				return RedirectToAction("StreetList");
			}

			EditStreet(Street.Id);
			ViewBag.Street = Street;
			return View("CreateStreet");
		}

		public ActionResult HouseList()
		{
			var houses = DbSession.Query<House>().ToList();
			ViewBag.Houses = houses;
			return View();
		}

		public ActionResult CreateHouse(int streetId = 0)
		{
			var House = new House();
			House.Confirmed = true;
			if (streetId > 0)
				ViewBag.Street = DbSession.Get<Street>(streetId);

			var streets = DbSession.Query<Street>().ToList();
			ViewBag.Streets = streets;
			ViewBag.House = House;
			return View("CreateHouse");
		}

		[HttpPost]
		public ActionResult CreateHouse([EntityBinder] House House, string yandexHouse, string yandexPosition)
		{
			//По возможности используем формализацию яндекса
			if (House.Confirmed) {
				House.Number = yandexHouse;
				House.Geomark = yandexPosition;
			}

			var errors = ValidationRunner.Validate(House);
			if (errors.Length == 0) {
				DbSession.Save(House);
				SuccessMessage("Дом успешно добавлен");
				return RedirectToAction("HouseList");
			}

			CreateHouse(House.Id);
			ViewBag.House = House;
			return View("CreateHouse");
		}

		public ActionResult EditHouse(int id = 0)
		{
			var House = DbSession.Get<House>(id);
			var streets = DbSession.Query<Street>().ToList();
			ViewBag.Street = House.Street;
			ViewBag.Streets = streets;
			ViewBag.House = House;
			return View("CreateHouse");
		}

		[HttpPost]
		public ActionResult EditHouse([EntityBinder] House House, string yandexHouse, string yandexPosition)
		{
			//По возможности используем формализацию яндекса
			if (House.Confirmed) {
				House.Number = yandexHouse;
				House.Geomark = yandexPosition;
			}

			var errors = ValidationRunner.Validate(House);
			if (errors.Length == 0) {
				DbSession.Save(House);
				SuccessMessage("Дом успешно изменен");
				return RedirectToAction("HouseList");
			}

			EditHouse(House.Id);
			ViewBag.House = House;
			return View("CreateHouse");
		}

		[HttpPost]
		public JsonResult GetStreetList(int regionId)
		{
			var streets = DbSession.Query<Street>().
				Where(s => s.Region.Id == regionId).
				Select(s => new {
					Id = s.Id,
					Name = s.Name,
					Geomark = s.Geomark,
					Confirmed = s.Confirmed, Region = s.Region.Id, Houses = s.Houses.Count
				}).ToList();
			return Json(streets, JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public JsonResult GetHouseList(int streetId)
		{
			var houses = DbSession.Query<House>().
				Where(s => s.Street.Id == streetId).
				Select(s => new {
					Id = s.Id,
					Number = s.Number,
					Geomark = s.Geomark,
					Confirmed = s.Confirmed,
					Street = s.Street.Id,
					EntranceAmount = s.EntranceAmount,
					ApartmentAmount = s.ApartmentAmount
				}).ToList();
			return Json(houses, JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public JsonResult GetPlansListForRegion(int regionId)
		{
			var planList = DbSession.Query<Plan>()
				.Where(s => s.RegionPlans.Any(d => d.Region.Id == regionId))
				.Select(d => new {
					Id = d.Id,
					Name = d.Name,
					Price = d.Price
				}).ToList();
			return Json(planList, JsonRequestBehavior.AllowGet);
		}
	}
}