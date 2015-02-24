using System.Linq;
using System.Net;
using System.Web.Mvc;
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

			return RedirectToAction("Index", "Home");
		}
		public ActionResult Index(int disable = 0, string ip ="")
		{
#if !DEBUG
		var addrs = Request.UserHostAddress;
		var address = IPAddress.Parse(addrs);
		var leases = DbSession.Query<Lease>().Where(l => l.Ip == address).ToList();
		var lease = leases.FirstOrDefault(l => l.Endpoint != null && l.Endpoint.Client != null);
		if (lease == null) {
			return RedirectToAction("Index","Home");
			}
		var endpoint = lease.Endpoint;
		var client = endpoint.Client;
#else
		Client client;
		ClientEndpoint endpoint;
		if(string.IsNullOrEmpty(ip))
		{
			 client = CurrentClient;
			 endpoint = client.Endpoints.FirstOrDefault();
		}
		else
		{
			var address = IPAddress.Parse(ip);
			var leases = DbSession.Query<Lease>().Where(l => l.Ip == address).ToList();
			var lease = leases.FirstOrDefault(l => l.Endpoint != null
		                                       && l.Endpoint.Client != null);
			endpoint = lease.Endpoint;
			if (lease.Endpoint == null || lease.Endpoint.Client == null) {
				return RedirectToAction("Index","Home");
			}
			 client = endpoint.Client;
		}
#endif

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

				SceHelper.UpdatePackageId(DbSession, client);
				DbSession.Save(client);
				return RedirectToAction("Index","Home");
			}
			var services = DbSession.Query<Service>().Where(s => s.IsActivableFromWeb);
			var blockAccountService = services.OfType<BlockAccountService>().FirstOrDefault();
			ViewBag.Client = client;
			ViewBag.BlockAccountService = blockAccountService;
			return View();
		}
	}
}