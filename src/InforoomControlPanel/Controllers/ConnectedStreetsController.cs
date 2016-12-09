using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;
using System.Security.Cryptography;
using System.Text;
using Castle.MonoRail.Framework.Adapters;
using InforoomControlPanel.Helpers;
using InforoomControlPanel.Models;
using NHibernate.Validator.Engine;
using Remotion.Linq.Clauses;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления списком подключенных домов
	/// </summary>
	public class ConnectedStreetsController : ControlPanelController
	{
		/// <summary>
		/// Список улиц
		/// </summary>
		public ActionResult Index(int regionId = 0)
		{
			// формирование фильтра
			var pager = new InforoomModelFilter<ConnectedStreet>(this);
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Name", OrderingDirection.Asc);
			if (regionId != 0) {
				var region = DbSession.Query<Region>().FirstOrDefault(s => s.Id == regionId);
				if (region != null) {
					pager.ParamDelete("filter.Equal.Region.Name");
					pager.ParamSet("filter.Equal.Region.Name", region.Name);
				}
			}
			//получение критерия для Hibernate запроса из класса ModelFilter
			var criteria = pager.GetCriteria();
			ViewBag.pager = pager;
			return View();
		}

		/// <summary>
		/// Добавление улицы
		/// </summary>
		[HttpGet]
		public ActionResult ConnectedStreetAdd(int regionId = 0)
		{
			var model = new ConnectedStreet();
			if (regionId != 0) {
				model.Region = DbSession.Query<Region>().FirstOrDefault(s => s.Id == regionId);
			}
			ViewBag.RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			return View(model);
		}

		/// <summary>
		/// Добавление улицы
		/// </summary>
		[HttpPost]
		public ActionResult ConnectedStreetAdd(ConnectedStreet model)
		{
			if (model.Region != null && model.Region.Id != 0) {
				model.Region = DbSession.Query<Region>().FirstOrDefault(s => s.Id == model.Region.Id);
			}
			if (model.AddressStreet != null && model.AddressStreet.Id != 0) {
				model.AddressStreet = DbSession.Query<Street>().FirstOrDefault(s => s.Id == model.AddressStreet.Id);
			} else {
				model.AddressStreet = null;
			}
			var errors = ValidationRunner.Validate(model);
			if (errors.Length == 0 && !string.IsNullOrEmpty(model.Name) && model.Region != null
				&& DbSession.Query<ConnectedStreet>().Any(s => s.Region.Id == model.Region.Id && s.Name.ToLower() == model.Name.ToLower())) {
				ErrorMessage($"Подключенная улица '{model.Name}' не может быть добавлена, т.к. указанное наименование уже присвоено другой улице в данном регионе.");
				errors.Add(new InvalidValue("дубль",typeof(ConnectedStreet), "Name", model.Name, model,null));
			}
			if (errors.Length == 0)
			{
				DbSession.Save(model);
				SuccessMessage("Подключенная улица успешно добавлена");
				return RedirectToAction("Index", new { @regionId = model.Region.Id });
			}
			PreventSessionUpdate();
			ViewBag.RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			return View(model);
		}

		/// <summary>
		/// Редактирование улицы
		/// </summary>
		[HttpGet]
		public ActionResult ConnectedStreetEdit(int id)
		{
			var model = DbSession.Query<ConnectedStreet>().FirstOrDefault(s => s.Id == id);
			ViewBag.RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.OldRegionId = model.Region.Id;
			return View(model);
		}

		/// <summary>
		/// Редактирование улицы
		/// </summary>
		[HttpPost]
		public ActionResult ConnectedStreetEdit(ConnectedStreet model)
		{
			var oldModel = DbSession.Query<ConnectedStreet>().FirstOrDefault(s => s.Id == model.Id);
			ViewBag.OldRegionId = oldModel.Region.Id;
			oldModel.Name = model.Name;
			oldModel.Disabled = model.Disabled;
			if (model.Region != null && model.Region.Id != 0) {
				oldModel.Region = DbSession.Query<Region>().FirstOrDefault(s => s.Id == model.Region.Id);
			}
			if (model.AddressStreet != null && model.AddressStreet.Id != 0) {
				oldModel.AddressStreet = DbSession.Query<Street>().FirstOrDefault(s => s.Id == model.AddressStreet.Id);
			} else {
				model.AddressStreet = null;
			}
			var errors = ValidationRunner.Validate(model);
			if (errors.Length == 0 && !string.IsNullOrEmpty(model.Name) && model.Region != null
				&& DbSession.Query<ConnectedStreet>().Any(s => s.Region.Id == model.Region.Id && s.Id != model.Id && s.Name.ToLower() == model.Name.ToLower())){
				ErrorMessage($"Подключенная улица '{model.Name}' не может быть добавлена, т.к. указанное наименование уже присвоено другой улице в данном регионе.");
				errors.Add(new InvalidValue("дубль", typeof(ConnectedStreet), "Name", model.Name, model, null));
			}
			if (errors.Length == 0) {
				DbSession.Save(oldModel);
				SuccessMessage("Подключенная улица успешно отредактирована");
				return RedirectToAction("Index", new {@regionId = model.Region.Id});
			}
			PreventSessionUpdate();
			ViewBag.RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			return View(model);
		}
		/// <summary>
		/// Удаление улицы
		/// </summary>
		public ActionResult ConnectedStreetDelete(int id)
		{
			var model = DbSession.Query<ConnectedStreet>().FirstOrDefault(s => s.Id == id);
			if (model != null) {
				if (model.HouseList.Count > 0) {
					ErrorMessage($"Подключенная улица №{id} не может быть удалена, т.к. на ней находятся дома");
				} else {
					DbSession.Delete(model);
					SuccessMessage($"Подключенная улица №{id} удалена успешно");
				}
			}
			return RedirectToAction("Index", new { regionId = model.Region.Id });
		}

	}
}