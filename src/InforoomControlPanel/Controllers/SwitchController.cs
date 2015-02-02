using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	public class SwitchController : AdminController
	{
		public ActionResult SwitchList()
		{
			ViewBag.Switches = DbSession.Query<Switch>().ToList();
			return View();
		}

		public ActionResult EditSwitch(int id)
		{
			ViewBag.Switch = DbSession.Get<Switch>(id);
			return View();
		}

		[HttpPost]
		public ActionResult EditSwitch([EntityBinder] Switch Switch)
		{
			var errors = ValidationRunner.ValidateDeep(Switch);
			if (errors.Length == 0) {
				DbSession.Save(Switch);
				SuccessMessage("Коммутатор успешно изменнен");
				return RedirectToAction("EditSwitch", new {id = Switch.Id });
			}
			EditSwitch(Switch.Id);
			ViewBag.Switch = Switch;
			return View();
		}

		public ActionResult DeleteSwitchAdress(int id)
		{
			var address = DbSession.Get<SwitchAddress>(id);
			DbSession.Delete(address);
			SuccessMessage("Адрес успешно удален");
			return RedirectToAction("EditSwitch", new { id = address.Switch.Id });
		}
	}
}