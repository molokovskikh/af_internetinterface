using System;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
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
					return RedirectToAction("EditHardware", new {id = rentalHardware.Id});
				}
				DbSession.Update(rentalHardware);
				SuccessMessage("Оборудование успешно изменено");
				return RedirectToAction("ShowHardware");
			}
			ViewBag.Hardware = rentalHardware;
			return View();
		}

		/// <summary>
		/// Список клиентов, у которых подключено арендованное оборудование
		/// </summary>
		/// <returns></returns>
		public ActionResult ClientList()
		{
			var pager = new ModelFilter<Client>(this);
			var criteria = pager.GetCriteria(i => i.PhysicalClient != null);
			//Это эквивалентно Group By по Id. Нельзя использовать Group By в проекциях, так как это сужает селект до 1го поля
			criteria.SetResultTransformer(new DistinctRootEntityResultTransformer());
			var joined = criteria.CreateCriteria("RentalHardwareList", JoinType.InnerJoin);

			if (!string.IsNullOrEmpty(pager.GetParam("RentBegins")))
				joined.Add(Restrictions.Gt("BeginDate", DateTime.Parse(pager.GetParam("RentBegins"))));
			if (!string.IsNullOrEmpty(pager.GetParam("RentEnds")))
				joined.Add(Restrictions.Lt("BeginDate", DateTime.Parse(pager.GetParam("RentEnds"))));

			if (!string.IsNullOrEmpty(pager.GetParam("HardwareType")) && pager.GetParam("HardwareType") != "0")
				joined.CreateCriteria("Hardware").Add(Restrictions.Eq("Id", int.Parse(pager.GetParam("HardwareType"))));

			pager.Execute();
			
			var hardware = DbSession.Query<RentalHardware>().ToList();
			ViewBag.Hardware = hardware;
			ViewBag.Pager = pager;
			return View();
		}
	}
}
