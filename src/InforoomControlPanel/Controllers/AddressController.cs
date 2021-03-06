﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using Switch = Inforoom2.Models.Switch;

namespace InforoomControlPanel.Controllers
{
	public class AddressController : ControlPanelController
	{
		public AddressController()
		{
			ViewBag.BreadCrumb = "Адреса";
		}

		public ActionResult Index()
		{
			return SwitchAddressList();
		}

		/// <summary>
		/// Удаление адреса дома
		/// </summary>
		public ActionResult DeleteHouse(int? id)
		{
			var city = DbSession.Get<City>(id);
			SuccessMessage("Улица успешно удален");
			DbSession.Delete(city);
			return RedirectToAction("HouseList");
		}

		/// <summary>
		/// Удаление адреса улицы
		/// </summary>
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

		/// <summary>
		/// Удаление адреса коммутатора
		/// </summary>
		public ActionResult DeleteSwitchAddress(int id)
		{
			var switchAddress = DbSession.Get<SwitchAddress>(id);
			SuccessMessage("Адрес успешно удален");
			DbSession.Delete(switchAddress);
			return RedirectToAction("SwitchAddressList");
		}


		/// <summary>
		/// Страница списка адресов коммутаторов
		/// </summary>
		public ActionResult SwitchAddressList()
		{
			ViewBag.Addresses = GetList<SwitchAddress>();
			return View("SwitchAddressList");
		}

		/// <summary>
		/// Создание нового адреса коммутатора
		/// </summary>
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


		/// <summary>
		/// Создание нового адреса коммутатора
		/// </summary>
		public ActionResult CreateSwitchAddress(int regionId = 0, int streetId = 0, int id = 0)
		{
			var SwitchAddress = new SwitchAddress();
			if (regionId > 0)
				ViewBag.Region = DbSession.Get<Region>(regionId);

			if (streetId > 0)
				ViewBag.Street = DbSession.Get<Street>(streetId);
			if (id > 0)
				ViewBag.Switch = DbSession.Get<NetworkNode>(id);

			var regions = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			var streets = DbSession.Query<Street>().ToList().OrderBy(s => s.PublicName()).ToList();
			var houses = DbSession.Query<House>().OrderBy(s => s.Number).ToList();
			var NetworkNodes = DbSession.Query<NetworkNode>().OrderBy(i => i.Name).ToList();
			ViewBag.Streets = streets;
			ViewBag.Houses = houses;
			ViewBag.Regions = regions;
			ViewBag.NetworkNodes = NetworkNodes;
			ViewBag.SwitchAddress = SwitchAddress;
			return View("CreateSwitchAddress");
		}

		/// <summary>
		/// Создание нового адреса коммутатора
		/// </summary>
		[HttpGet]
		public ActionResult ConnectedHouses()
		{
			var regions = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			var result = new List<SwitchAddress>();
			ViewBag.Regions = regions;
			ViewBag.Result = result;
			ViewBag.CurrentRegion = 0;
			ViewBag.CurrentStreet = 0;
			ViewBag.CurrentHouse = 0;
			return View();
		}

		[HttpPost]
		public ActionResult ConnectedHouses(int? regionId, int? streetId, int? houseId)
		{
			var result = new List<SwitchAddress>();
			if (houseId.HasValue && houseId != 0) {
				result.AddRange(DbSession.Query<SwitchAddress>().Where(s => s.House.Id == houseId));
			}
			else if (streetId.HasValue && streetId != 0) {
				result.AddRange(DbSession.Query<SwitchAddress>().Where(s => s.House.Street.Id == streetId));
			}

			var regions = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.Regions = regions;
			ViewBag.Result = result;
			ViewBag.CurrentRegion = regionId ?? 0;
			ViewBag.CurrentStreet = streetId ?? 0;
			ViewBag.CurrentHouse = houseId ?? 0;

			return View();
		}


		/// <summary>
		/// Изменение адреса коммутатора
		/// </summary>
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

		/// <summary>
		/// Изменение адреса коммутатора
		/// </summary>
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

		/// <summary>
		/// Страница списка городов
		/// </summary>
		public ActionResult CityList()
		{
			var cities = DbSession.Query<City>().OrderBy(s => s.Name).ToList();
			ViewBag.Cities = cities;
			return View();
		}

		/// <summary>
		/// Страница списка регионов
		/// </summary>
		public ActionResult RegionList()
		{
			var regions = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.Regions = regions;
			return View();
		}

		/// <summary>
		/// Страница списка адресов улиц
		/// </summary>
		public ActionResult StreetList()
		{
			var pager = new InforoomModelFilter<Street>(this);
			var criteria = pager.GetCriteria();
			criteria.SetResultTransformer(new DistinctRootEntityResultTransformer());
			if (!string.IsNullOrEmpty(pager.GetParam("Name")))
				criteria.Add(Restrictions.Like("Name", "%" + pager.GetParam("Name") + "%"));
			if ((!string.IsNullOrEmpty(pager.GetParam("Region.Id"))) && pager.GetParam("Region.Id") != "0")
				criteria.Add(Restrictions.Eq("Region.Id", Int32.Parse(pager.GetParam("Region.Id"))));
			pager.Execute();
			ViewBag.Regions = DbSession.Query<Street>().Select(s => s.Region)
				.OrderBy(s => s.Name).Distinct().ToList();
			ViewBag.Pager = pager;
			return View();
		}

		/// <summary>
		/// Создание нового города
		/// </summary>
		/// <returns></returns>
		public ActionResult CreateCity()
		{
			var City = new City();
			ViewBag.City = City;
			return View("CreateCity");
		}

		/// <summary>
		/// Создание нового города
		/// </summary>
		[HttpPost]
		public ActionResult CreateCity([EntityBinder] City Сity)
		{
			var errors = ValidationRunner.ValidateDeep(Сity);
			if (errors.Length == 0) {
				if (DbSession.Query<City>().Any(s => s.Name == Сity.Name)) {
					ErrorMessage($"Город с названием '{Сity.Name}' уже существует!");
				}
				else {
					DbSession.Save(Сity);
					SuccessMessage("Город успешно добавлен");
					return RedirectToAction("CityList");
				}
			}

			CreateCity();
			ViewBag.Сity = Сity;
			return View("CreateCity");
		}

		/// <summary>
		/// Создание нового региона
		/// </summary>
		/// <returns></returns>
		public ActionResult CreateRegion(int cityId = 0, int parentId = 0)
		{
			var Region = new Region();
			ViewBag.Region = Region;

			if (cityId > 0)
				ViewBag.City = DbSession.Get<City>(cityId);
			if (parentId > 0)
				ViewBag.Parent = DbSession.Get<Region>(parentId);

			var citys = DbSession.Query<City>().OrderBy(s => s.Name).ToList();
			var parents = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.Citys = citys;
			ViewBag.Parents = parents;
			return View("CreateRegion");
		}

		/// <summary>
		/// Создание нового региона
		/// </summary>
		[HttpPost]
		public ActionResult CreateRegion([EntityBinder] Region Region, int regionParentId = 0)
		{
			Region parentRegion = null;
			if (regionParentId != 0 && regionParentId != Region.Id) {
				parentRegion = DbSession.Query<Region>().FirstOrDefault(s => s.Id == regionParentId);
			}
			Region.Parent = parentRegion;
			var errors = ValidationRunner.Validate(Region);
			if (errors.Length == 0) {
				if (DbSession.Query<Region>().Any(s => s.Name == Region.Name)) {
					ErrorMessage($"Регион с названием '{Region.Name}' уже существует!");
				}
				else {
					DbSession.Save(Region);
					SuccessMessage("Регион успешно добавлен");
					return RedirectToAction("RegionList");
				}
			}
			CreateRegion();
			PreventSessionUpdate();
			ViewBag.Region = Region;
			ViewBag.City = Region.City;
			ViewBag.Parent = Region?.Parent;
			return View("CreateRegion");
		}

		/// <summary>
		/// Удаление города
		/// </summary>
		/// <param name="id">Идентификатор города</param>
		/// <returns></returns>
		public ActionResult RemoveCity(int id)
		{
			var itemToDelete = DbSession.Query<City>().FirstOrDefault(s => s.Id == id);
			if (itemToDelete != null) {
				var listOfRegions = DbSession.Query<Region>().Where(s => s.City.Id == id).ToList();
				var reasonNotToRemove = "";

				if (listOfRegions.Count > 0) {
					reasonNotToRemove += $" <p>- Регионы ({listOfRegions.Count}): {string.Join(", ", listOfRegions.Select(s => s.Name).ToList()).CutAfter(500)}.</p> ";
				}
				if (reasonNotToRemove == string.Empty) {
					DbSession.Delete(itemToDelete);
					SuccessMessage("Объект успешно удален!");
				}
				else {
					ErrorMessage($"<p>Невозможно удалить город, т.к. существуют зависимые </p> {reasonNotToRemove}");
				}
			}
			else {
				ErrorMessage("Объект уже был удален!");
			}
			return RedirectToAction("CityList");
		}

		/// <summary>
		/// Удаление региона
		/// </summary>
		/// <param name="id">Идентификатор региона</param>
		/// <returns></returns>
		public ActionResult RemoveRegion(int id)
		{
			var itemToDelete = DbSession.Query<Region>().FirstOrDefault(s => s.Id == id);
			if (itemToDelete != null) {
				var reasonNotToRemove = "";

				if (itemToDelete.Children.Count > 0) {
					reasonNotToRemove += $" <p>- Дочернии регионы ({itemToDelete.Children.Count}): {string.Join(", ", itemToDelete.Children.Select(s => s.Name).ToList()).CutAfter(500)}.</p> ";
				}
				if (itemToDelete.Streets.Count > 0) {
					reasonNotToRemove += $" <p>- Улицы ({itemToDelete.Streets.Count}): {string.Join(", ", itemToDelete.Streets.Select(s => s.PublicName()).ToList()).CutAfter(500)}.</p> ";
				}
				if (reasonNotToRemove == string.Empty) {
					SafeDelete<Region>(id);
				}
				else {
					ErrorMessage($"<p>Невозможно удалить регион, т.к. существуют зависимые </p> {reasonNotToRemove}");
				}
			}
			else {
				ErrorMessage("Объект уже был удален!");
			}
			return RedirectToAction("RegionList");
		}

		/// <summary>
		/// Удаление улицы
		/// </summary>
		/// <param name="id">Идентификатор улицы</param>
		/// <returns></returns>
		public ActionResult RemoveStreet(int id)
		{
			var itemToDelete = DbSession.Query<Street>().FirstOrDefault(s => s.Id == id);
			if (itemToDelete != null) {
				var reasonNotToRemove = "";

				var childrenElementsSwitch = DbSession.Query<SwitchAddress>().Where(s => s.Street.Id == id).ToList();

				var houseList = DbSession.Query<House>().Where(s => (s.Region == null || itemToDelete.Region.Id == s.Region.Id) &&
				                                                    ((s.Street.Region.Id == itemToDelete.Region.Id && s.Street.Id == itemToDelete.Id) || (s.Street.Id == itemToDelete.Id && s.Region.Id == itemToDelete.Region.Id)) &&
				                                                    (s.Street.Region.Id == itemToDelete.Region.Id && s.Region == null || (s.Street.Id == itemToDelete.Id && s.Region.Id == itemToDelete.Region.Id))
					).Select(s => s.Number).OrderBy(s => s).ToList();

				if (itemToDelete.Houses.Count > 0) {
					reasonNotToRemove += $" <p>- Дома ({houseList.Count}): {string.Join(", ", houseList).CutAfter(500)}.</p> ";
				}

				if (childrenElementsSwitch.Count > 0) {
					reasonNotToRemove += $" <p>- На улице находятся коммутаторы ({childrenElementsSwitch.Count}) :" +
					                     $" {string.Join(", ", childrenElementsSwitch.Select(s => $"<a class='inline' target='_blank' href='{Url.Action("EditSwitchAddress", "Address", new { @id = s.Id })}'>{s.NetworkNode.Name}</a>").ToList()).CutAfter(500)}.</p> ";
				}
				if (reasonNotToRemove == string.Empty) {
					DbSession.Delete(itemToDelete);
					SuccessMessage("Улица успешно удалена!");
				}
				else {
					ErrorMessage($"<p>Невозможно удалить улицу {itemToDelete.Name}, т.к. существуют зависимые </p> {reasonNotToRemove}");
				}
			}
			else {
				ErrorMessage("Улица уже была удалена!");
			}

			return RedirectToAction("StreetList");
		}

		/// <summary>
		/// Удаление дома
		/// </summary>
		/// <param name="id">Идентификатор дома</param>
		/// <returns></returns>
		public ActionResult RemoveHouse(int id)
		{
			var itemToDelete = DbSession.Query<House>().FirstOrDefault(s => s.Id == id);
			var childrenElements = DbSession.Query<Client>().Where(s => s.PhysicalClient.Address.House.Id == id).ToList();
			var childrenElementsSwitch = DbSession.Query<SwitchAddress>().Where(s => s.House.Id == id).ToList();

			if (itemToDelete != null) {
				var reasonNotToRemove = "";

				if (childrenElements.Count > 0) {
					reasonNotToRemove += $" <p>- В доме зарегистрированы клиенты ({childrenElements.Count}) :" +
					                     $" {string.Join(", ", childrenElements.Select(s => $"<a class='inline' target='_blank' href='{Url.Action((s.PhysicalClient != null ? "InfoPhysical" : "InfoLegal"), "Client", new { @id = s.Id })}'>{s.Id}</a>").ToList()).CutAfter(500)}.</p> ";
				}

				if (childrenElementsSwitch.Count > 0) {
					reasonNotToRemove += $" <p>- В доме находятся коммутаторы ({childrenElementsSwitch.Count}) :" +
					                     $" {string.Join(", ", childrenElementsSwitch.Select(s => $"<a class='inline' target='_blank' href='{Url.Action("EditSwitchAddress", "Address", new { @id = s.Id })}'>{s.NetworkNode.Name}</a>").ToList()).CutAfter(500)}.</p> ";
				}
				if (reasonNotToRemove == string.Empty) {
					DbSession.Delete(itemToDelete);
					SuccessMessage("Объект успешно удален!");
				}
				else {
					ErrorMessage($"<p>Невозможно удалить дом '{itemToDelete.Number}' по улице '{itemToDelete.Street.Name}', потому что </p> {reasonNotToRemove}");
				}
			}
			else {
				ErrorMessage("Объект уже был удален!");
			}
			return RedirectToAction("HouseList");
		}


		/// <summary>
		/// Создание нового адреса улицы
		/// </summary>
		public ActionResult CreateStreet(int regionId = 0)
		{
			var Street = new Street();
			Street.Confirmed = true;
			if (regionId > 0)
				ViewBag.Region = DbSession.Get<Region>(regionId);

			var regions = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.Regions = regions;
			ViewBag.Street = Street;
			return View("CreateStreet");
		}

		/// <summary>
		/// Создание нового адреса улицы
		/// </summary>
		[HttpPost]
		public ActionResult CreateStreet([EntityBinder] Street street, string yandexStreet, string yandexPosition)
		{
			//По возможности используем формализацию яндекса
			if (street.Confirmed) {
				street.Name = yandexStreet;
				street.Geomark = yandexPosition;
			}

			var errors = ValidationRunner.ValidateDeep(street);
			if (errors.Length == 0) {
				if (DbSession.Query<Street>().Any(s => s.Name == street.Name && s.Region.Id == street.Region.Id)) {
					ErrorMessage($"Улица с названием '{street.Name}' В регионе '{street.Region.Name}' уже существует!");
				}
				else {
					DbSession.Save(street);
					SuccessMessage("Улица успешно добавлена");
					return RedirectToAction("StreetList");
				}
			}

			CreateStreet();
			PreventSessionUpdate();
			ViewBag.Street = street;
			return View("CreateStreet");
		}

		/// <summary>
		/// Изменение адреса улицы
		/// </summary>
		public ActionResult EditStreet(int id)
		{
			var street = DbSession.Get<Street>(id);
			var regions = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.Region = street.Region;
			ViewBag.Regions = regions;
			ViewBag.Street = street;
			return View("CreateStreet");
		}

		[HttpPost]
		public ActionResult EditStreet([EntityBinder] Street street, string yandexStreet, string yandexPosition)
		{
			//По возможности используем формализацию яндекса
			if (street.Confirmed) {
				street.Name = yandexStreet;
				street.Geomark = yandexPosition;
			}

			var errors = ValidationRunner.ValidateDeep(street);
			if (errors.Length == 0) {
				if (DbSession.Query<Street>().Any(s => s.Name == street.Name && s.Region.Id == street.Region.Id && s.Id != street.Id)) {
					ErrorMessage($"Улица с названием '{street.Name}' В регионе '{street.Region.Name}' уже существует!");
				}
				else {
					DbSession.Save(street);
					SuccessMessage("Улица успешно изменена");
					return RedirectToAction("StreetList");
				}
			}

			EditStreet(street.Id);
			PreventSessionUpdate();
			ViewBag.Street = street;
			return View("CreateStreet");
		}

		/// <summary>
		/// Страница списка домов
		/// </summary>
		public ActionResult HouseList()
		{
			var pager = new InforoomModelFilter<House>(this);
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Number");

			var criteria = pager.GetCriteria();
			criteria.SetResultTransformer(new DistinctRootEntityResultTransformer());
			if (!string.IsNullOrEmpty(pager.GetParam("Number")))
				criteria.Add(Restrictions.Like("Number", "%" + pager.GetParam("Number") + "%"));
			if ((!string.IsNullOrEmpty(pager.GetParam("Street.Id"))) && pager.GetParam("Street.Id") != "0")
				criteria.Add(Restrictions.Eq("Street.Id", Int32.Parse(pager.GetParam("Street.Id"))));
			if ((!string.IsNullOrEmpty(pager.GetParam("Region.Id"))) && pager.GetParam("Region.Id") != "0") {
				criteria.CreateCriteria("Street", "StreetAl", JoinType.InnerJoin);
				criteria.Add(Restrictions.Or(Restrictions.Eq("Region.Id", Int32.Parse(pager.GetParam("Region.Id"))),
					Restrictions.And(Restrictions.IsNull("Region.Id"), Restrictions.Eq("StreetAl.Region.Id", Int32.Parse(pager.GetParam("Region.Id"))))));
			}
			ViewBag.Streets = DbSession.Query<House>().Select(s => s.Street)
				.OrderBy(s => s.Name).Distinct().ToList().OrderBy(s=>s.PublicName()).ToList();
			ViewBag.Regions = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.Pager = pager;
			return View();
		}

		/// <summary>
		/// Создание нового адреса дома
		/// </summary>
		public ActionResult CreateHouse(int streetId = 0)
		{
			var House = new House();
			House.Confirmed = true;
			if (streetId > 0)
				ViewBag.Street = DbSession.Get<Street>(streetId);

			var streets = DbSession.Query<Street>().OrderBy(s => s.Name).ToList();
			var regions = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.Streets = streets;
			ViewBag.House = House;
			ViewBag.Regions = regions;
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
				if (DbSession.Query<House>().Any(s => s.Number == House.Number && s.Street.Id == House.Street.Id && s.Id != House.Id)) {
					ErrorMessage($"Дом под номером '{House.Number}' на улице '{House.Street.Name}' уже существует!");
				}
				else {
					DbSession.Save(House);
					SuccessMessage("Дом успешно добавлен");
					return RedirectToAction("HouseList");
				}
			}

			CreateHouse(House.Id);
			PreventSessionUpdate();
			ViewBag.House = House;
			return View("CreateHouse");
		}

		/// <summary>
		/// Изменение города
		/// </summary>
		public ActionResult EditCity(int id = 0)
		{
			var City = DbSession.Get<City>(id);
			ViewBag.City = City;
			return View();
		}

		[HttpPost]
		public ActionResult EditCity(City City)
		{
			var currentCity = DbSession.Get<City>(City.Id);
			var errors = ValidationRunner.Validate(City);
			if (errors.Length == 0) {
				if (DbSession.Query<City>().Any(s => s.Name == City.Name && s.Id != City.Id)) {
					ErrorMessage($"Город с названием '{City.Name}' уже существует!");
				}
				else {
					if (currentCity != null) {
						currentCity.Name = City.Name;
					}
					DbSession.Save(currentCity);
					SuccessMessage("Город успешно изменен");
					return RedirectToAction("CityList");
				}
			}
			PreventSessionUpdate();
			EditCity(City.Id);
			ViewBag.City = City;
			return View();
		}

		/// <summary>
		/// Изменение региона
		/// </summary>
		public ActionResult EditRegion(int id = 0)
		{
			var Region = DbSession.Get<Region>(id);
			ViewBag.Region = Region;

			var citys = DbSession.Query<City>().OrderBy(s => s.Name).ToList();
			var parents = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.Citys = citys;
			ViewBag.Parents = parents;
			ViewBag.City = Region?.City;
			ViewBag.Parent = Region?.Parent;
			return View();
		}

		/// <summary>
		/// Изменение региона
		/// </summary>
		[HttpPost]
		public ActionResult EditRegion([EntityBinder] Region Region, int regionParentId = 0)
		{
			Region parentRegion = null;
			if (regionParentId != 0 && regionParentId != Region.Id) {
				parentRegion = DbSession.Query<Region>().FirstOrDefault(s => s.Id == regionParentId);
			}
			Region.Parent = parentRegion;
			var errors = ValidationRunner.Validate(Region);
			if (errors.Length == 0) {
				if (DbSession.Query<Region>().Any(s => s.Name == Region.Name && s.Id != Region.Id)) {
					ErrorMessage($"Регион с названием '{Region.Name}' уже существует!");
				} else {
					DbSession.Save(Region);
					SuccessMessage("Регион успешно изменен");
					return RedirectToAction("RegionList");
				}
			}
			EditRegion(Region.Id);
			PreventSessionUpdate();
			ViewBag.Region = Region;
			ViewBag.City = Region.City;
			return View();
		}

		/// <summary>
		/// Редактирование дома
		/// </summary>
		/// <param name="id">Идектификатор дома</param>
		public ActionResult EditHouse(int id = 0)
		{
			var house = DbSession.Get<House>(id);
			CreateHouse(house.Street.Id);
			ViewBag.House = house;
			return View("CreateHouse");
		}

		/// <summary>
		/// Редактирование дома
		/// </summary>
		/// <param name="id">Идектификатор дома</param>
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
				if (DbSession.Query<House>().Any(s => s.Number == House.Number && s.Street.Id == House.Street.Id && s.Id != House.Id)) {
					ErrorMessage($"Дом под номером '{House.Number}' на улице '{House.Street.Name}' уже существует!");
				}
				else {
					DbSession.Save(House);
					SuccessMessage("Дом успешно изменен");
					return RedirectToAction("HouseList");
				}
			}

			EditHouse(House.Id);
			PreventSessionUpdate();
			ViewBag.House = House;
			return View("CreateHouse");
		}
	}
}