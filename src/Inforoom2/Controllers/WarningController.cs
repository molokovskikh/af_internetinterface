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

	public class WarningLawController : BaseController
	{
		private bool RedirectToMain(Client client)
		{
			var currentController = ControllerContext.RouteData.Values["controller"].ToString().ToLower();
			var currentAction = ControllerContext.RouteData.Values["action"].ToString().ToLower();
			if (client.Disabled && currentController == "warninglaw" && currentAction == "lawdisabled") {
				return false;
			}
			if (client.ShowBalanceWarningPage && currentController == "warninglaw" && currentAction == "lawlowbalance") {
				return false;
			}
			return true;
		}

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.Title = "Инфорум";
			var cityList = DbSession.Query<Region>().Where(s => s.ShownOnMainPage).Select(s => s.Name).OrderBy(s => s).ToArray();
			ViewBag.Cities = cityList;
			var client = WarningHelper.GetLegalClientIfExists(this);
			if (client == null || RedirectToMain(client)) {
				var resultUrl = Url.Action("Index", "Home");
				filterContext.Result = new RedirectResult(resultUrl);
				return;
			}
			var currentRegion = client.GetRegion();
			if (currentRegion != null) {  
				SetCookie("userCity", currentRegion.Name);
				ViewBag.RegionOfficePhoneNumber = currentRegion.RegionOfficePhoneNumber;
				ViewBag.UserCityBelongsToUs = true;
				ViewBag.UserCity = currentRegion.Name;
			}

			var ipstring = Request.UserHostAddress;
			#if DEBUG
						//Можем авторизоваться по лизе за клиента
						ipstring = Request.QueryString["ip"] ?? null;
			#endif
			ViewBag.CurrentIp = ipstring;
		}

		public ActionResult LawDisabled()
		{
			return View();
		}

		public ActionResult LawLowBalance()
		{
			return View();
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