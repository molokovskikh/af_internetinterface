using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
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
			var streets = DbSession.Query<Street>().OrderBy(s => s.Name).ToList();
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
				criteria.Add(Restrictions.Like("Name", "%"+pager.GetParam("Name")+"%"));
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
			if (errors.Length == 0)
			{
				DbSession.Save(Сity);
				SuccessMessage("Город успешно добавлен");
				return RedirectToAction("CityList");
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
		public ActionResult CreateRegion([EntityBinder] Region Region)
		{
			var errors = ValidationRunner.ValidateDeep(Region);
			if (errors.Length == 0)
			{
				DbSession.Save(Region);
				SuccessMessage("Регион успешно добавлен");
				return RedirectToAction("RegionList");
			}

			CreateRegion();
			ViewBag.Region = Region;
			return View("CreateRegion");
		}

		/// <summary>
		/// Удаление города
		/// </summary>
		/// <param name="id">Идентификатор города</param>
		/// <returns></returns>
		public ActionResult RemoveCity(int id)
		{
			SafeDelete<City>(id);
			return RedirectToAction("CityList");
		}

		/// <summary>
		/// Удаление региона
		/// </summary>
		/// <param name="id">Идентификатор региона</param>
		/// <returns></returns>
		public ActionResult RemoveRegion(int id)
		{
			SafeDelete<Region>(id);
			return RedirectToAction("RegionList");
		}

		/// <summary>
		/// Удаление улицы
		/// </summary>
		/// <param name="id">Идентификатор улицы</param>
		/// <returns></returns>
		public ActionResult RemoveStreet(int id)
		{
			SafeDelete<Street>(id);
			return RedirectToAction("StreetList");
		}

		/// <summary>
		/// Удаление дома
		/// </summary>
		/// <param name="id">Идентификатор дома</param>
		/// <returns></returns>
		public ActionResult RemoveHouse(int id)
		{
			SafeDelete<House>(id);
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

		/// <summary>
		/// Изменение адреса улицы
		/// </summary>
		public ActionResult EditStreet(int id)
		{
			var Street = DbSession.Get<Street>(id);
			var regions = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
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

		/// <summary>
		/// Страница списка домов
		/// </summary>
		public ActionResult HouseList()
		{
			var pager = new InforoomModelFilter<House>(this);
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
				.OrderBy(s => s.Name).Distinct().ToList();
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
				DbSession.Save(House);
				SuccessMessage("Дом успешно добавлен");
				return RedirectToAction("HouseList");
			}

			CreateHouse(House.Id);
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
		public ActionResult EditCity([EntityBinder] City City)
		{
			var errors = ValidationRunner.Validate(City);
			if (errors.Length == 0)
			{
				DbSession.Save(City);
				SuccessMessage("Город успешно изменен");
				return RedirectToAction("CityList");
			}

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
			return View();
		}

		/// <summary>
		/// Изменение региона
		/// </summary>
		[HttpPost]
		public ActionResult EditRegion([EntityBinder] Region Region)
		{
			var errors = ValidationRunner.Validate(Region);
			if (errors.Length == 0)
			{
				DbSession.Save(Region);
				SuccessMessage("Регион успешно изменен");
				return RedirectToAction("RegionList");
			}

			EditCity(Region.Id);
			ViewBag.Region = Region;
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
				DbSession.Save(House);
				SuccessMessage("Дом успешно изменен");
				return RedirectToAction("HouseList");
			}

			EditHouse(House.Id);
			ViewBag.House = House;
			return View("CreateHouse");
		}


		/// <summary>
		/// Возвращение списка улиц по региону.
		/// </summary>
		/// <param name="regionId">Id региона</param>
		/// <param name="count">кол-во улиц</param>
		/// <returns>Изменение произошло</returns>
		[HttpPost]
		public JsonResult GetStreetNumberChangedFlag(int regionId, int count)
		{
			var equals = DbSession.Query<Street>()
				.Count(s => s.Region.Id == regionId || s.Houses.Any(a => a.Region.Id == regionId)) != count;
			return Json(equals, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Возвращение списка улиц по региону.
		/// </summary>
		/// <param name="regionId">Id региона</param>
		/// <param name="streetId">Id улицы</param>
		/// <param name="count">кол-во улиц</param>
		/// <returns>Изменение произошло</returns>
		[HttpPost]
		public JsonResult GetHouseNumberChangedFlag(int streetId, int count, int regionId = 0)
		{
			var equals = false;
			if (regionId != 0) {
				equals = DbSession.Query<House>().Count(s => (s.Region == null || regionId == s.Region.Id) &&
				                                             ((s.Street.Region.Id == regionId && s.Street.Id == streetId) || (s.Street.Id == streetId && s.Region.Id == regionId)) &&
				                                             (s.Street.Region.Id == regionId && s.Region == null || (s.Street.Id == streetId && s.Region.Id == regionId))
					) != count;
			}
			else {
				equals = DbSession.Query<House>().Count(s => s.Street.Id == streetId) != count;
			}
			return Json(equals, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Возвращение списка улиц по региону.
		/// </summary>
		/// <param name="regionId">Id региона</param>
		/// <returns>Json* Список в форме: Id, Name, Geomark, Confirmed, Region (Id), Houses (кол-во)</returns>
		[HttpPost]
		public JsonResult GetStreetList(int regionId)
		{
			var streets = DbSession.Query<Street>().
				Where(s => s.Region.Id == regionId || s.Houses.Any(a => a.Region.Id == regionId)).
				Select(s => new {
					Id = s.Id,
					Name = s.Name,
					Geomark = s.Geomark,
					Confirmed = s.Confirmed,
					Region = s.Region.Id,
					Houses = s.Houses.Count
				}).OrderBy(s => s.Name).ToList();
			return Json(streets, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Возвращение списка домов по улице.
		/// </summary>
		/// <param name="regionId">Id региона</param>
		/// <param name="streetId">Id улицы</param>
		/// <returns>Json* Список в форме: Id, Number, Geomark, Confirmed, Street (Id), EntranceAmount ,ApartmentAmount</returns>
		[HttpPost]
		public JsonResult GetHouseList(int streetId, int regionId = 0)
		{
			var query = DbSession.Query<House>();
			if (regionId != 0) {
				query = query.Where(s => (s.Region == null || regionId == s.Region.Id) &&
				                         ((s.Street.Region.Id == regionId && s.Street.Id == streetId) || (s.Street.Id == streetId && s.Region.Id == regionId)) &&
				                         (s.Street.Region.Id == regionId && s.Region == null || (s.Street.Id == streetId && s.Region.Id == regionId))
					);
			}
			else {
				query = query.Where(s => s.Street.Id == streetId);
			}
			var houses = query.Select(s => new {
				Id = s.Id,
				Number = s.Number,
				Geomark = s.Geomark,
				Confirmed = s.Confirmed,
				Street = s.Street.Id,
				EntranceAmount = s.EntranceAmount,
				ApartmentAmount = s.ApartmentAmount
			}).OrderBy(s => s.Number).ToList();
			return Json(houses, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Получение тарифов по региону
		/// </summary>
		/// <param name="regionId">Id региона</param>
		/// <returns>Json* Список в форме: Id, Name, Price</returns>
		[HttpPost]
		public JsonResult GetPlansListForRegion(int regionId)
		{
			var planList = DbSession.Query<Plan>()
				.Where(s => s.Disabled == false && s.AvailableForNewClients && s.RegionPlans.Any(d => d.Region.Id == regionId))
				.Select(d => new {
					Id = d.Id,
					Name = d.Name,
					Price = d.Price
				}).OrderBy(s => s.Name).ToList();
			return Json(planList, JsonRequestBehavior.AllowGet);
		}
	}
}