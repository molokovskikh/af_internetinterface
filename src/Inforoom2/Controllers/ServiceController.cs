using System;
using System.Collections.Generic;
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
	public class ServiceController : Inforoom2Controller
	{
		/// <summary>
		/// Функция дублирована в PersonalController
		/// Нужно удалить ее отовсюду, потому что она говно полное
		/// </summary>
		protected void InitServices()
		{
			var client = CurrentClient;
			var services = DbSession.Query<Service>().Where(s => s.IsActivableFromWeb);
			var blockAccountService = services.FirstOrDefault(i => i is BlockAccountService);
			blockAccountService = BaseModel.UnproxyOrDefault(blockAccountService) as BlockAccountService;

			var deferredPayment = services.FirstOrDefault(i => i is DeferredPayment);
			deferredPayment = BaseModel.UnproxyOrDefault(deferredPayment) as DeferredPayment;
			var inforoomServices = new List<Service> { blockAccountService, deferredPayment };

			ViewBag.Client = client;
			//@todo Убрать исключения для статического IP, когда будет внедрено ручное включение сервиса
			ViewBag.ClientServices = client.ClientServices.Where(cs => (cs.Service.Name == "Фиксированный ip-адрес" || cs.Service.IsActivableFromWeb) && cs.IsActivated).ToList();
			ViewBag.AvailableServices = inforoomServices;

			ViewBag.BlockAccountService = blockAccountService;
			ViewBag.DeferredPayment = deferredPayment;
		}

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
			var oldPlan = client.PhysicalClient.Plan;
			client.PhysicalClient.LastTimePlanChanged = SystemTime.Now();
			client.PhysicalClient.Plan = plan;
			client.SetStatus(StatusType.Worked, DbSession);
			DbSession.Save(client);
			SuccessMessage("Тариф успешно изменен.");
			// добавление записи в историю тарифов пользователя
			var planHistory = new PlanHistoryEntry{
				Client =  CurrentClient,
				DateOfChange = SystemTime.Now(),
				PlanAfter = plan,
				PlanBefore = oldPlan,
				Price = oldPlan.GetTransferPrice(plan)
			};
			DbSession.Save(planHistory);
			var msg = string.Format("Изменение тарифа был изменен с '{0}'({1}) на '{2}'({3}). Стоимость перехода: {4} руб.", oldPlan.Name, oldPlan.Price, plan.Name, plan.Price, 0);
			var appeal = new Appeal(msg, client, AppealType.User) {
				Employee = GetCurrentEmployee()
			};
			DbSession.Save(appeal);
			return RedirectToAction("Profile", "Personal");
		}
	}
}