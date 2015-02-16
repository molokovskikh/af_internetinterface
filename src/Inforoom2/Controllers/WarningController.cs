using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Xml.Linq;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate.Linq;
using SceHelper = Inforoom2.Helpers.SceHelper;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Страница управления аутентификацией
	/// </summary>
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

		public ActionResult Index(int disable = 0, string ip ="")
		{
			var ipstring = Request.UserHostAddress;
#if DEBUG
			ipstring = ip;
#endif
			var endpoint = ClientEndpoint.GetEndpointForIp(ipstring, DbSession);
			if (endpoint == null)
			{
				var lease = Lease.GetLeaseForIp(ipstring,DbSession);
				if(!ipstring.Contains("172.25.0")) //Остановим спам от непонятных
					EmailSender.SendEmail("asarychev@analit.net", "Редидеркт с варнинга на главную: "+ipstring+(lease != null ? ", есть аренда:"+lease.Id:""),CollectDebugInfo().ToString());
				return RedirectToAction("Index", "Home");
			}
			var client = endpoint.Client;

			if (disable != 0) {
				if (client.Status.Type == StatusType.BlockedForRepair) {
					client.SetStatus(StatusType.Worked, DbSession);
				}
				else if (client.Disabled) {
					return RedirectToAction("Index","Home");
				}
				else if (client.ShowBalanceWarningPage) {
					client.ShowBalanceWarningPage = false;
					var appeal = new Appeal("Отключена страница Warning, клиент отключил со страницы", client, AppealType.Statistic) {
						Employee = GetCurrentEmployee()
					};
					DbSession.Save(appeal);
				}
				DbSession.Save(client);
				DbSession.Flush();

				SceHelper.UpdatePackageId(DbSession, client);
				DbSession.Save(client);

				if (!client.HasPassportData())
					return RedirectToAction("FirstVisit","Personal");

				return RedirectToAction("Index","Home");
			}
			var services = DbSession.Query<Service>().Where(s => s.IsActivableFromWeb);
			var blockAccountService = services.OfType<BlockAccountService>().FirstOrDefault();
			ViewBag.Client = client;
			ViewBag.BlockAccountService = blockAccountService;
			return View("Index");
		}
	}
}