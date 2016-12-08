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

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления списком подключенных домов
	/// </summary>
	public class ConnectedHousesController : ControlPanelController
	{
		/// <summary>
		/// Список домов
		/// </summary>
		/// <param name="regionId"></param>
		/// <returns></returns>
		public ActionResult Index(int regionId = 0)
		{
			ViewBag.Regions = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.CurrentRegion = regionId;
			var connectedHouses = new List<ViewModelConnectedHouses>();
			var streetList = new List<ConnectedStreet>();
			if (regionId!=0) {
				streetList = DbSession.Query<ConnectedStreet>().Where(s=>s.Region.Id == regionId && !s.Disabled).OrderBy(s => s.Name).ToList();
			}
			ViewBag.StreetList = streetList;
			ViewBag.ConnectedHouses = connectedHouses;
			return View();
		}

		/// <summary>
		/// Добавление дома
		/// </summary>
		/// <param name="model">Модель представления </param>
		/// <param name="regionId"></param>
		/// <returns></returns>
		public ActionResult HouseAdd(ViewModelConnectedHouse model, int regionId)
		{
			var existed = DbSession.Query<ConnectedHouse>().FirstOrDefault(s => s.ConnectedStreet.Id == model.Street
			                                                                    && s.Number.Replace(" ", "").ToLower() == model.House.Replace(" ", "").ToLower());
			if (existed != null) {
				ErrorMessage($"Дом '{existed.Number}' на улице '{existed.ConnectedStreet.Name}' в городе '{existed.ConnectedStreet.Region.Name}' уже был добавлен");
				return RedirectToAction("Index", new { @regionId = regionId });
			}
			var sHouse = model.Id == 0 ? new ConnectedHouse() : DbSession.Query<ConnectedHouse>().FirstOrDefault(s => s.Id == model.Id);
			sHouse.ConnectedStreet = DbSession.Query<ConnectedStreet>().FirstOrDefault(s => s.Id == model.Street);
			sHouse.Region = DbSession.Query<Region>().FirstOrDefault(s => s.Id == regionId);
			sHouse.Number = model.House;
			sHouse.Disabled = model.Disabled;
			sHouse.Comment = model.Comment;
			sHouse.IsCustom = true;
			var errors = ValidationRunner.Validate(sHouse);
			if (errors.Length == 0) {
				DbSession.Save(sHouse);
				SuccessMessage("Подключенный дом добавлен");
			}
			else {
				DbSession.Refresh(sHouse);
				ErrorMessage("Неверно заполнены поля. Подключенный дом добавлен не был");
			}
			return RedirectToAction("Index", new { @regionId = regionId });
		}

		/// <summary>
		/// Генерация домов
		/// </summary>
		/// <param name="regionId">Регион</param>
		/// <param name="streetId">Улица</param>
		/// <param name="numberFirst">Первый добавляемый номер дома</param>
		/// <param name="numberLast">Последний добавляемый номер дома</param>
		/// <param name="side">Сторона: обе/левая/правая</param>
		/// <param name="disabled">Отключение отображения домов на странице сайта</param>
		/// <param name="state">Состояние обработки: Добавление и Обновление/Добавление/Обновление/Удаление</param>
		/// <returns></returns>
		public ActionResult HouseGenerate(int regionId, int streetId, int numberFirst,
			int numberLast, ConnectedHouse.HouseStreetSide side, bool disabled, ConnectedHouse.HouseGenerationState state)
		{
			ConnectedHouse.ConnectionsGet(DbSession, regionId, streetId, numberFirst, numberLast, side, disabled, true, state);
			return RedirectToAction("Index", new { @regionId = regionId });
		}

		/// <summary>
		/// Синхронизация с адресами физ.лиц
		/// </summary>
		/// <param name="regionId"></param>
		/// <returns></returns>
		public ActionResult SynchronizeConnections(int regionId)
		{
			ConnectedHouse.SynchronizeConnections(DbSession);
			return RedirectToAction("Index", new { @regionId = regionId });
		}

		/// <summary>
		/// Редактирование дома/Удаление
		/// </summary>
		/// <param name="model"></param>
		/// <param name="regionId"></param>
		/// <param name="delete"></param>
		/// <returns></returns>
		public ActionResult HouseEdit(ViewModelConnectedHouse model, int regionId, bool delete)
		{
			var sHouse = DbSession.Query<ConnectedHouse>().FirstOrDefault(s => s.Id == model.Id);
			if (sHouse == null) {
				ErrorMessage("Редактирование не выполнено: подключенный дом не найден");
				return RedirectToAction("Index", new { @regionId = regionId });
			}
			sHouse.Region = DbSession.Query<Region>().FirstOrDefault(s => s.Id == regionId);
			sHouse.ConnectedStreet = DbSession.Query<ConnectedStreet>().FirstOrDefault(s => s.Id == model.Street);
			if (delete) {
				DbSession.Delete(sHouse);
				SuccessMessage($"Дом '{sHouse.Number}' по улице '{sHouse.ConnectedStreet.Name}' в городе {sHouse.Region.Name} был удален");
			}
			else {
				sHouse.Number = model.House;
				sHouse.Disabled = model.Disabled;
				sHouse.Comment = model.Comment;
				sHouse.IsCustom = true;
				var errors = ValidationRunner.Validate(sHouse);
				if (errors.Length == 0) {
					DbSession.Save(sHouse);
					SuccessMessage($"Дом '{sHouse.Number}' по улице '{sHouse.ConnectedStreet.Name}' в городе {sHouse.Region.Name} был изменен");
				}
				else {
					DbSession.Refresh(sHouse);
					ErrorMessage("Неверно заполнены поля. Подключенный дом изменен не был");
				}
			}
			return RedirectToAction("Index", new { @regionId = regionId });
		}

		public JsonResult HouseGet(int id = 0)
		{
			var sHouse = id == 0 ? new ConnectedHouse() : DbSession.Query<ConnectedHouse>().FirstOrDefault(s => s.Id == id);
			var result = new ViewModelConnectedHouse() {
				Street = sHouse.ConnectedStreet.Id,
				Id = sHouse.Id,
				House = sHouse.Number,
				Disabled = sHouse.Disabled,
				Comment = sHouse.Comment
			};
			return Json(result, JsonRequestBehavior.AllowGet);
		}
	}
}