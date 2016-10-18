using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Models;
using Inforoom2.Intefaces;
using NHibernate.Linq;
using NHibernate.Util;
using NPOI.HSSF.Record;
using PackageSpeed = Inforoom2.Models.PackageSpeed;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления баннерами
	/// </summary>
	public class PriceListController : ControlPanelController
	{
		/// <summary>
		/// Список прайс-листов
		/// </summary>
		public ActionResult PriceListIndex()
		{
			var model = DbSession.Query<PublicData>().OrderBy(s => s.PositionIndex).ToList();
			return View(model);
		}
		
		/// <summary>
		/// Редактировать прайс-лист
		/// </summary>
		[HttpGet]
		public ActionResult PriceListEdit(int? id)
		{
			var model = id.HasValue ? DbSession.Query<PublicData>().FirstOrDefault(s => s.Id == id) : new PublicData();
			ViewBag.RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.ViewModelPublicDataList =
				model.Items.OrderBy(s => s.PositionIndex).Select(s => s.ContextGet<ViewModelPublicDataPriceList>()).ToList();
			return View(model);
		}

		/// <summary>
		/// Редактировать прайс-лист
		/// </summary>
		[HttpPost]
		public ActionResult PriceListEdit(PublicData publicData)
		{
			var errors = ValidationRunner.Validate(publicData);
			if (errors.Length != 0) {
				ViewBag.RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
				ViewBag.ViewModelPublicDataList =
					publicData.Items.OrderBy(s => s.PositionIndex).Select(s => s.ContextGet<ViewModelPublicDataPriceList>()).ToList();
				return View(publicData);
			}
			var model = DbSession.Query<PublicData>().FirstOrDefault(s => s.Id == publicData.Id);
			model = model ?? new PublicData();

			model.Region = publicData.Region != null && publicData.Region.Id != 0
				? DbSession.Query<Region>().FirstOrDefault(s => s.Id == publicData.Region.Id)
				: null;
			model.Display = publicData.Display;
			model.Name = publicData.Name;
			model.ItemType = PublicDataType.PriceList;
			model.LastUpdate = SystemTime.Now();
			DbSession.Save(model);

			SuccessMessage("Прайс лист успешно сохранен.");
			return RedirectToAction("PriceListIndex");
		}


		/// <summary>
		/// Редактировать содержание прайс-листа
		/// </summary>
		[HttpGet]
		public ActionResult PriceListContextEdit(int id, int? contextId)
		{
			var model = contextId.HasValue ? DbSession.Query<PublicDataContext>().FirstOrDefault(s => s.Id == contextId) : null;
			model = model ?? new PublicDataContext();
			ViewBag.PublicDataId = model.Id != 0 ? model.PublicData.Id : id;

			ViewBag.RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			return View(model.ContextGet<ViewModelPublicDataPriceList>() ?? new ViewModelPublicDataPriceList());
		}

		/// <summary>
		/// Редактировать содержание прайс-листа
		/// </summary>
		[HttpPost]
		public ActionResult PriceListContextEdit(int id, ViewModelPublicDataPriceList publicDataContext)
		{
			if (string.IsNullOrEmpty(publicDataContext.Name)) {
				ErrorMessage("Элемент не может быть сохранен: не указано наименование.");
				ViewBag.RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
				ViewBag.PublicDataId = id;
				return View(publicDataContext);
			}

			var publicDataItemExisted = publicDataContext.Id != 0
				? DbSession.Query<PublicDataContext>().FirstOrDefault(s => s.Id == publicDataContext.Id)
				: null;

			publicDataItemExisted = publicDataItemExisted ?? new PublicDataContext();
			publicDataItemExisted.PublicData = DbSession.Query<PublicData>().First(s => s.Id == id);
			publicDataItemExisted.ContextSet(publicDataContext);

			DbSession.Save(publicDataItemExisted);
			publicDataContext = publicDataItemExisted.ContextGet<ViewModelPublicDataPriceList>();
			publicDataContext.Id = publicDataItemExisted.Id;
			publicDataItemExisted.ContextSet(publicDataContext);
			DbSession.Save(publicDataItemExisted);

			ViewBag.RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			return RedirectToAction("PriceListEdit", new {id});
		}

		/// <summary>
		/// Удаление прайс-листа
		/// </summary>
		public ActionResult PriceListDelete(int id)
		{
			var itemToDelete = DbSession.Query<PublicData>().FirstOrDefault(s => s.Id == id);
			if (itemToDelete == null) {
				ErrorMessage("Элемент не может быть удален: прайс-лист не найден");
			}
			DbSession.Delete(itemToDelete);
			SuccessMessage("Прайс-лист успешно удален");
			return RedirectToAction("PriceListIndex");
		}

		/// <summary>
		/// Удаление элемента содержания
		/// </summary>
		public ActionResult PriceListContextDelete(int id, int contextId)
		{
			var itemToDelete = DbSession.Query<PublicDataContext>().FirstOrDefault(s => s.Id == contextId);
			if (itemToDelete == null) {
				ErrorMessage("Элемент не может быть удален: элемент не найден");
			}
			DbSession.Delete(itemToDelete);
			SuccessMessage("Элемент содержания успешно удален");
			return RedirectToAction("PriceListEdit", new {id});
		}

		private bool updatePositionIndex<T>(List<T> list, int[] idList) where T : IPositionIndex
		{
			if (idList == null) {
				for (var i = 0; i < list.Count; i++) {
					list[i].PositionIndex = null;
					DbSession.Save(list[i]);
				}
			} else {
				if (idList.Length != list.Count) {
					return false;
				}
				for (int i = 0; i < idList.Length; i++) {
					var currentElement = list.FirstOrDefault(s => s.Id == idList[i]);
					if (currentElement != null) {
						currentElement.PositionIndex = i;
					} else {
						DbSession.Transaction.Rollback();
						return false;
					}
					DbSession.Save(currentElement);
				}
			}
			return true;
		}
		
		[HttpPost]
		public JsonResult PublicDataUpdateIndexValue(int[] idList)
		{
			const string error = "Изменяемый список был обновлен и не соответствует текущему.";
			if (idList == null) {
				return Json(error, JsonRequestBehavior.AllowGet);
			}
			var list = DbSession.Query<PublicData>().ToList();
			var result = updatePositionIndex(list, idList);
			return Json(result ? string.Empty : error, JsonRequestBehavior.AllowGet);
		}
		
		public ActionResult PublicDataUpdateIndexNull()
		{
			var list = DbSession.Query<PublicData>().ToList();
			var result = updatePositionIndex(list, null);
			return RedirectToAction("PriceListIndex");
		}

		[HttpPost]
		public JsonResult PublicDataContextUpdateIndexValue(int[] idList, int parentId)
		{
			const string error = "Изменяемый список был обновлен и не соответствует текущему.";
			if (idList == null)
			{
				return Json(error, JsonRequestBehavior.AllowGet);
			}
			var list = DbSession.Query<PublicDataContext>().Where(s => s.PublicData.Id == parentId).ToList();
			var result = updatePositionIndex(list, idList);
			return Json(result ? string.Empty : error, JsonRequestBehavior.AllowGet);
		}
		public ActionResult PublicDataContextUpdateIndexNull(int id)
		{
			var list = DbSession.Query<PublicDataContext>().Where(s => s.PublicData.Id == id).ToList();
			var result = updatePositionIndex(list, null);
			return RedirectToAction("PriceListEdit", new {id});
		}
	}
}