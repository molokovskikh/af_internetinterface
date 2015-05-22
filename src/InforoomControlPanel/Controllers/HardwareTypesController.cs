using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Класс-контроллер для просмотра/редактирования оборудования, предназначенного для аренды
	/// </summary>
	public class HardwareTypesController : AdminController
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
		public ActionResult EditHardware([EntityBinder] RentalHardware hardware)
		{
			var errors = ValidationRunner.Validate(hardware);
			if (errors.Length == 0) {
				var isRepeated = DbSession.Query<RentalHardware>().ToList()
						.Exists(hw => hw.Name == hardware.Name && hw.Id != hardware.Id);
				if (isRepeated) {
					ErrorMessage("Такое оборудование уже существует!");
					return RedirectToAction("EditHardware", new {id = hardware.Id});
				}
				DbSession.Update(hardware);
				SuccessMessage("Оборудование успешно изменено");
				return RedirectToAction("ShowHardware");
			}
			ViewBag.Hardware = hardware;
			return View();
		}
	}
}
