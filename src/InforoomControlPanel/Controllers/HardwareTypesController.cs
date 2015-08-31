using System;
using System.Linq;
using System.Web.Mvc;
using Common.MySql;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.validators;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.SqlCommand;
using NHibernate.Transform;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Класс-контроллер для просмотра/редактирования оборудования, предназначенного для аренды
	/// </summary>
	public class HardwareTypesController : ControlPanelController
	{
		/// <summary>
		/// Отображает список оборудования с данными по нему
		/// </summary>
		public ActionResult ShowHardware()
		{
			ViewBag.HardwareList = DbSession.Query<RentalHardware>().ToList();
			return View();
		}

		/// <summary>
		/// Отображает поля данных по оборудованию для редактирования
		/// </summary>
		public ActionResult EditHardware(int id)
		{
			ViewBag.Hardware = DbSession.Get<RentalHardware>(id);
			return View();
		}

		/// <summary>
		/// Сохраняет данные с формы для указанного оборудования
		/// </summary>
		[HttpPost]
		public ActionResult EditHardware([EntityBinder] RentalHardware rentalHardware)
		{
			var errors = ValidationRunner.Validate(rentalHardware);
			if (errors.Length == 0) {
				var isRepeated = DbSession.Query<RentalHardware>().ToList()
					.Exists(hw => hw.Name == rentalHardware.Name && hw.Id != rentalHardware.Id);
				if (isRepeated) {
					ErrorMessage("Такое оборудование уже существует!");
					return RedirectToAction("EditHardware", new { id = rentalHardware.Id });
				}
				DbSession.Update(rentalHardware);
				SuccessMessage("Оборудование успешно изменено");
				return RedirectToAction("EditHardware", new { id = rentalHardware.Id });
			}
			ViewBag.Hardware = rentalHardware;
			return View();
		}

		/// <summary>
		/// Создание эл-та комплектации
		/// </summary>
		[HttpPost]
		public ActionResult CreateHardwarePart(int id, string name)
		{
			var rentalHardware = DbSession.Query<RentalHardware>().FirstOrDefault(s => s.Id == id);
			if (rentalHardware != null) {
				var hardwarePart = new HardwarePart() {
					Name = name,
					RentalHardware = rentalHardware
				};

				var errors = ValidationRunner.Validate(hardwarePart, hardwarePart.Validate(null));

				if (errors.Length == 0) {
					rentalHardware.HardwareParts.Add(hardwarePart);
					DbSession.Update(rentalHardware);
					return RedirectToAction("EditHardware", new { id = rentalHardware.Id });
				}

				ViewBag.HardwarePart = hardwarePart;
				ViewBag.Hardware = rentalHardware;
			}
			return View("EditHardware");
		}

		/// <summary>
		/// Удаление эл-та комплектации
		/// </summary>
		public ActionResult DeleteHardwarePart(int id)
		{
			var hardwarePart = DbSession.Query<HardwarePart>().FirstOrDefault(s => s.Id == id);
			if (hardwarePart != null) {
				bool deleted = DbSession.AttemptDelete(hardwarePart);
				if (deleted) {
					SuccessMessage("Элемент '" + hardwarePart.Name + "' был успешно удален из комплектации!");
				}
				else {
					ErrorMessage("Элемент '" + hardwarePart.Name + "' не может быть удален из комплектации! *(вероятно он используется)");
				}
				return RedirectToAction("EditHardware", new { id = hardwarePart.RentalHardware.Id });
			}
			else {
				return RedirectToAction("ShowHardware");
			}
		}

		/// <summary>
		/// Список клиентов, у которых подключено арендованное оборудование
		/// </summary>
		/// <returns></returns>
		public ActionResult ClientList()
		{
			var pager = new InforoomModelFilter<Client>(this);
			if (!pager.IsExecutedByUser()) {
				pager.ParamSet("filter.Equal.RentalHardwareList.First().IsActive", "true");
			}
			pager.GetCriteria(i => i.PhysicalClient != null);
			ViewBag.Pager = pager;
			return View();
		}
	}
}