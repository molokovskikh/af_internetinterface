using System;
using System.Linq;
using System.Web.Mvc;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class ServiceController : PersonalController
	{
		public ActionResult BlockAccount()
		{
			InitServices();
			AddJavascriptParam("FreeBlockDays", CurrentClient.FreeBlockDays.ToString());
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
					BeginDate = SystemTime.Now(),
					EndDate = blockingEndDate,
					Service = service,
					Client = client,
					ActivatedByUser = true
				};
				ActivateService(clientService, client);
				if (clientService.IsActivated) {
					var appealText = "Услуга \"{0}\" активирована на период с {1} по {2}. Баланс {3}.";
					var appeal = new Appeal(string.Format(appealText, service.Name, SystemTime.Now().ToShortDateString(),
						blockingEndDate.Value.ToShortDateString(), client.Balance),
						client, AppealType.User) {
							Employee = GetCurrentEmployee()
						};
					DbSession.Save(appeal);
				}
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
			SuccessMessage("Работа возобновлена");
			return RedirectToAction("Service", "Personal");
		}

		[HttpPost]
		public ActionResult ActivateDefferedPayment([EntityBinder] Service service)
		{
			var client = CurrentClient;
			if (client.CanUseService(service)) {
				var clientService = new ClientService {
					BeginDate = SystemTime.Now(),
					EndDate = SystemTime.Now().AddDays(3),
					Service = service,
					Client = client
				};
				ActivateService(clientService, client);
				if (clientService.IsActivated) {
					var appealText = "Услуга \"{0}\" активирована на период с {1} по {2}. Баланс {3}.";
					var appeal = new Appeal(string.Format(appealText, service.Name, SystemTime.Now().ToShortDateString(),
						SystemTime.Now().AddDays(3).ToShortDateString(), client.Balance), client, AppealType.User) {
							Employee = GetCurrentEmployee()
						};
					DbSession.Save(appeal);
				}
				return RedirectToAction("Service", "Personal");
			}
			ErrorMessage("Не удалось подключить услугу");
			InitServices();
			return RedirectToAction("Service", "Personal");
		}

		private void ActivateService(ClientService clientService, Client client)
		{
			var msg = clientService.ActivateFor(client, DbSession);
			if (clientService.IsActivated) {
				SuccessMessage(msg);
				if (client.IsNeedRecofiguration)
					SceHelper.UpdatePackageId(DbSession, client);
				DbSession.Update(client);
			}
			else
				ErrorMessage(msg);
			InitServices();
		}

		public ActionResult InternetPlanChanger()
		{
			if (CurrentClient == null) {
				return RedirectToAction("index", "home");
			}
			else {
				// получение сведения об изменении тарифов
				var planChangerList = DbSession.Query<PlanChangerData>().ToList();
				foreach (var changer in planChangerList) {
					//поиск целевого тариф
					if (changer.TargetPlan == CurrentClient.PhysicalClient.Plan) {
						ViewBag.CheapPlan = DbSession.Query<Plan>().FirstOrDefault(s => s == changer.CheapPlan);
						ViewBag.FastPlan = DbSession.Query<Plan>().FirstOrDefault(s => s == changer.FastPlan);
						ViewBag.InnerHtml = changer.Text;
						return View();
					}
				}
				return RedirectToAction("index", "home");
			}
		}

		/// <summary>
		/// Смена тарифного плана
		/// </summary>
		/// <param name="plan">Тарифный план</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult InternetPlanChanger([EntityBinder] Plan plan)
		{
			var client = CurrentClient;
			ViewBag.Client = client;
			//todo - наверно надо подумать как эти провеки засунуть куда следует
			var beginDate = client.WorkingStartDate ?? new DateTime();
			if (beginDate == DateTime.MinValue || beginDate.AddMonths(2) >= SystemTime.Now()) {
				ErrorMessage("Нельзя менять тариф, в первые 2 месяца после подключения");
				return View();
			}
			var oldPlan = client.PhysicalClient.Plan;
			var result = client.PhysicalClient.RequestChangePlan(plan);
			if (result == null) {
				ErrorMessage("Не достаточно средств для смены тарифного плана");
				return View();
			}
			DbSession.Save(client);
			DbSession.Save(result);
			var warning = (client.GetWorkDays() <= 3) ? " Обратите внимание, что у вас низкий баланс!" : "";
			SuccessMessage("Тариф успешно изменен." + warning);
			var msg = string.Format("Изменение тарифа был изменен с '{0}'({1}) на '{2}'({3}). Стоимость перехода: {4} руб.", oldPlan.Name, oldPlan.Price, plan.Name, plan.Price, result.Sum);
			var appeal = new Appeal(msg, client, AppealType.User) {
				Employee = GetCurrentEmployee()
			};
			DbSession.Save(appeal);
			return RedirectToAction("Profile", "Personal");
		}
	}
}