using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Mvc;
using System.Xml.Linq;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate.Linq;
using SceHelper = Inforoom2.Helpers.SceHelper;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Страница управления аутентификацией
	/// </summary>
	public class WarningBlockController : Controller
	{
		public JsonResult CheckRedirect()
		{
			var url = WarningHelper.GetForwardUrl(this);
			return Json(url);
		}
	}

	public class WarningController : Inforoom2Controller
	{
		public ActionResult RepairCompleted()
		{
			var client = CurrentClient;
			if (client.Status.Type == StatusType.BlockedForRepair)
				client.SetStatus(StatusType.Worked, DbSession);

			DbSession.Save(client);
			SuccessMessage("Работа возобновлена");
			return RedirectToAction("Index", "Home");
		}

		public ActionResult Index(int disable = 0, string ip = "")
		{
			ViewBag.Client = CurrentClient;
			return View();
		}


		public ActionResult LawDisabled()
		{
			ViewBag.Client = CurrentClient;
			return View();
		}

		public ActionResult LawLowBalance()
		{
			ViewBag.Client = CurrentClient;
			return View();
		}

		public ActionResult PhysBlockedForRepair()
		{
			ViewBag.Client = CurrentClient;
			return View();
		}

		public ActionResult PhysLowBalance()
		{
			ViewBag.Client = CurrentClient;
			return View();
		}

		public ActionResult PhysVoluntaryBlocking()
		{
			var services = DbSession.Query<Service>().Where(s => s.IsActivableFromWeb);
			var blockAccountService = services.FirstOrDefault(s => s is BlockAccountService);
			ViewBag.BlockAccountService = blockAccountService.Unproxy();
			ViewBag.Client = CurrentClient;

			return View();
		}

		public ActionResult PhysFirstPayment()
		{
			ViewBag.Client = CurrentClient;
			return View();
		}

		public ActionResult PhysPassportData()
		{
			ViewBag.Client = CurrentClient;
			return View();
		}

		[HttpPost]
		public ActionResult TryToDisableWarning()
		{
			return null;
		}
	}
}