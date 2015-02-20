using System;
using System.Linq;
using System.Web.Mvc;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;

namespace Inforoom2.Controllers
{
	public class ServiceController : PersonalController
	{
		public ActionResult BlockAccount()
		{
			InitServices();
			return View();
		}

		public ActionResult DeferredPayment()
		{
			InitServices();
			return View();
		}

		[HttpPost]
		public ActionResult ActivateAccountBlocking([EntityBinder] Service service, DateTime? blockingEndDate)
		{
			var client = CurrentClient;
			if (client.CanUseService(service) && blockingEndDate != null) {
				var clientService = new ClientService {
					BeginDate = DateTime.Now,
					EndDate = blockingEndDate,
					Service = service,
					Client = client,
					ActivatedByUser = true
				};
				ActivateService(clientService, client);
				var appealText = "Услуга \"{0}\" активирована на период с {1} по {2}. Баланс {3}.";
				var appeal = new Appeal(string.Format(appealText, service.Name, DateTime.Now.ToShortDateString(),
						blockingEndDate.Value.ToShortDateString(), client.Balance),
						client, AppealType.User) {
					Employee = GetCurrentEmployee()
				};
				DbSession.Save(appeal);
				return RedirectToAction("Service", "Personal");
			}
			ErrorMessage("Не удалось подключить услугу");
			InitServices();
			return RedirectToAction("Service", "Personal");
		}

		[HttpPost]
		public ActionResult DeactivateAccountBlocking([EntityBinder] Service service)
		{
			var client = DbSession.Load<Client>(CurrentClient.Id);
			var clientService = client.ClientServices.First(c => c.Service.Id == service.Id);
			SuccessMessage(clientService.DeActivateFor(CurrentClient, DbSession));
			if (client.IsNeedRecofiguration)
				SceHelper.UpdatePackageId(DbSession, client);
			DbSession.Update(client);
			InitServices();
			var appealText = "Услуга \"{0}\" деактивирована. Баланс {1}.";
			var appeal = new Appeal(string.Format(appealText, service.Name, client.Balance), client, AppealType.User) {
				Employee = GetCurrentEmployee()
			};
			DbSession.Save(appeal);
			return RedirectToAction("Service", "Personal");
		}

		[HttpPost]
		public ActionResult ActivateDefferedPayment([EntityBinder] Service service)
		{
			var client = CurrentClient;
			if (client.CanUseService(service)) {
				var clientService = new ClientService {
					BeginDate = DateTime.Now,
					EndDate = DateTime.Now.AddDays(3),
					Service = service,
					Client = client
				};
				ActivateService(clientService, client);
				var appealText = "Услуга \"{0}\" активирована на период с {1} по {2}. Баланс {3}.";
				var appeal = new Appeal(string.Format(appealText, service.Name, DateTime.Now.ToShortDateString(),
						DateTime.Now.AddDays(3).ToShortDateString(), client.Balance), client, AppealType.User) {
					Employee = GetCurrentEmployee()
				};
				DbSession.Save(appeal);
				return RedirectToAction("Service", "Personal");
			}
			ErrorMessage("Не удалось подключить услугу");
			InitServices();
			return RedirectToAction("Service", "Personal");
		}

		private void ActivateService(ClientService clientService, Client client)
		{
			SuccessMessage(clientService.ActivateFor(client, DbSession));
			if (client.IsNeedRecofiguration)
				SceHelper.UpdatePackageId(DbSession, client);
			DbSession.Update(client);
			InitServices();
		}
	}
}