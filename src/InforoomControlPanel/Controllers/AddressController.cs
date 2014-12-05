using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	public class AddressController : AdminController
	{
		public ActionResult AddressCity()
		{
			ViewBag.CitiesList = Cities;
			return View();
		}

		public ActionResult EditCity(int? cityId)
		{
			City city;
			city = cityId != null ? DbSession.Get<City>(cityId) : new City();
			ViewBag.City = city;
			return View();
		}

		public ActionResult DeleteCity(int? cityId)
		{
			var city = DbSession.Get<City>(cityId);
			DbSession.Delete(city);
			return RedirectToAction("AddressCity");
		}

		[HttpPost]
		public ActionResult UpdateCity(City city)
		{
			ViewBag.City = city;
			var errors = ValidationRunner.ValidateDeep(city);
			if (errors.Length == 0) {
				DbSession.SaveOrUpdate(city);
				SuccessMessage("Город успешно отредактирован");
			}
			else {
				return View("EditCity");
			}

			return RedirectToAction("AddressCity");
		}

		public ActionResult AddressRegion()
		{
			ViewBag.Regions = Regions;

			return View();
		}

		public ActionResult EditRegion(int? regionId)
		{
			Region region;
			if (regionId != null) {
				region = DbSession.Get<Region>(regionId);
			}
			else {
				region = new Region();
				region.City = new City();
				region.Plans = new List<Plan>();
			}
			var plans = DbSession.Query<Plan>().ToList();
			ViewBag.Region = region;
			ViewBag.CitiesList = Cities;
			ViewBag.Plans = plans;

			return View();
		}

		public ActionResult DeleteRegion(int? regionId)
		{
			var region = DbSession.Get<Region>(regionId);
			DbSession.Delete(region);
			return RedirectToAction("AddressRegion");
		}


		[HttpPost]
		public ActionResult UpdateRegion(Region region, string[] checkedPlans)
		{
			region.Plans.Clear();
			if (checkedPlans != null) {
				foreach (var checkedPlan in checkedPlans) {
					var plan = DbSession.Get<Plan>(Convert.ToInt32(checkedPlan));
					region.Plans.Add(plan);
				}
			}
			var city = Cities.FirstOrDefault(c => c.Id == region.City.Id);
			region.City = city;
			ViewBag.Region = region;
			var errors = ValidationRunner.ValidateDeep(region);
			if (errors.Length == 0) {
				DbSession.SaveOrUpdate(region);
				SuccessMessage("Регион успешно отредактирован");
			}
			else {
				return View("EditRegion");
			}

			return RedirectToAction("AddressRegion");
		}


		public ActionResult AddressStreet()
		{
			ViewBag.Streets = Streets;

			return View();
		}

		public ActionResult EditStreet(int? streetId)
		{
			Street street;

			if (streetId != null) {
				street = DbSession.Get<Street>(streetId);
			}
			else {
				street = new Street();
				street.Region = new Region();
			}
			ViewBag.CitiesList = Cities;
			ViewBag.Street = street;
			return View();
		}

		public ActionResult DeleteStreet(int? streetId)
		{
			var street = DbSession.Get<Street>(streetId);
			DbSession.Delete(street);
			return RedirectToAction("AddressStreet");
		}

		[HttpPost]
		public ActionResult UpdateStreet(Street street)
		{
			ViewBag.Street = street;
			var region = Regions.FirstOrDefault(r => r.Name == street.Region.Name);
			street.Region = region;
			var errors = ValidationRunner.ValidateDeep(street);
			if (errors.Length == 0) {
				DbSession.SaveOrUpdate(street);
				SuccessMessage("Улица успешно отредактирована");
			}
			else {
				return View("EditStreet");
			}

			return RedirectToAction("AddressStreet");
		}

		public ActionResult AddressHouse()
		{
			ViewBag.Houses = Houses;

			return View();
		}

		public ActionResult EditHouse(int? houseid)
		{
			House house;
			if (houseid != null) {
				house = DbSession.Get<House>(houseid);
			}
			else {
				house = new House();
				house.Street = new Street();
				house.Street.Region = new Region();
				house.Street.Region.City = new City();
			}

			ViewBag.House = house;
			ViewBag.Regions = Regions;
			ViewBag.Streets = Streets;
			return View();
		}

		public ActionResult DeleteHouse(int? houseid)
		{
			var house = DbSession.Get<House>(houseid);
			DbSession.Delete(house);
			return RedirectToAction("AddressHouse");
		}

		[HttpPost]
		public ActionResult UpdateHouse(House house)
		{
			ViewBag.House = house;
			var street = Streets.FirstOrDefault(r => r.Id == house.Street.Id);
			house.Street = street;
			var errors = ValidationRunner.ValidateDeep(house);
			if (errors.Length == 0) {
				DbSession.SaveOrUpdate(house);
				SuccessMessage("Улица успешно отредактирована");
			}
			else {
				return View("EditHouse");
			}

			return RedirectToAction("AddressHouse");
		}

		public ActionResult AddressAddress()
		{
			ViewBag.Addresses = Addresses;
			return View();
		}

		public ActionResult EditAddress(int? addressId)
		{
			Address address;
			if (addressId != null) {
				address = DbSession.Get<Address>(addressId);
			}
			else {
				address = new Address();
				address.House = new House();
			}

			ViewBag.Address = address;
			ViewBag.Houses = Houses;
			ViewBag.Regions = Regions;
			ViewBag.Streets = Streets;
			return View();
		}

		public ActionResult DeleteAddress(int? addressId)
		{
			var address = DbSession.Get<Address>(addressId);
			DbSession.Delete(address);
			return RedirectToAction("AddressAddress");
		}

		[HttpPost]
		public ActionResult UpdateAddress(Address address)
		{
			ViewBag.Address = address;
			ViewBag.Houses = Houses;
			ViewBag.Regions = Regions;
			ViewBag.Streets = Streets;
			var house = Houses.FirstOrDefault(r => r.Id == address.House.Id);
			address.House = house;
			var errors = ValidationRunner.ValidateDeep(address);
			if (errors.Length == 0) {
				DbSession.SaveOrUpdate(address);
				SuccessMessage("Адрес успешно сохранен");
			}
			else {
				return View("EditAddress");
			}
			return RedirectToAction("AddressAddress");
		}

		public ActionResult AddressSwitchAddress()
		{
			ViewBag.SwitchAddresses = SwitchAddresses;
			return View();
		}

		public ActionResult EditSwitchAddress(int? switchAddressId)
		{
			SwitchAddress switchAddress;
			if (switchAddressId != null) {
				switchAddress = DbSession.Get<SwitchAddress>(switchAddressId);
			}
			else {
				switchAddress = new SwitchAddress();
				switchAddress.House = new House();
				switchAddress.House.Street = new Street();
				switchAddress.House.Street.Region = new Region();
				switchAddress.Switch = new Switch();
			}

			ViewBag.SwitchAddress = switchAddress;
			ViewBag.Switches = Switches;
			ViewBag.Houses = Houses;
			ViewBag.Regions = Regions;
			ViewBag.Streets = Streets;
			return View();
		}

		public ActionResult EditPrivateSwitchAddress(int? switchAddressId)
		{
			SwitchAddress switchAddress;
			if (switchAddressId != null) {
				switchAddress = DbSession.Get<SwitchAddress>(switchAddressId);
			}
			else {
				switchAddress = new SwitchAddress();
				switchAddress.Street = new Street();
				switchAddress.Street.Region = new Region();
				switchAddress.Switch = new Switch();
			}

			ViewBag.SwitchAddress = switchAddress;
			ViewBag.Switches = Switches;
			ViewBag.Houses = Houses;
			ViewBag.Regions = Regions;
			ViewBag.Streets = Streets;
			return View();
		}

		
		[HttpPost]
		public ActionResult DeleteSwitchAddress([EntityBinder]SwitchAddress switchAddress)
		{
			//var switchAddress = DbSession.Get<SwitchAddress>(switchAddressId);
			DbSession.Delete(switchAddress);
			return RedirectToAction("AddressSwitchAddress");
		}

		[HttpPost]
		public ActionResult UpdateSwitchAddress([EntityBinder] SwitchAddress switchAddress)
		{
			ViewBag.Address = switchAddress;
			ViewBag.Houses = Houses;
			ViewBag.Regions = Regions;
			ViewBag.Streets = Streets;
			ViewBag.Switches = Switches;

			var errors = ValidationRunner.ValidateDeep(switchAddress);
			if (errors.Length == 0) {
				DbSession.SaveOrUpdate(switchAddress);
				SuccessMessage("Адрес успешно сохранен");
			}
			else {
				return View("EditAddress");
			}
			return RedirectToAction("AddressSwitchAddress");
		}
	}
}