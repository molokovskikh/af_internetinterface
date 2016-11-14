using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Helpers;
using InternetInterface.Services;
using MonoRail.Debugger.Toolbar;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(DiscriminatorValue = "PlanChanger")]
	public class PlanChanger : Service
	{
		public const string MessagePatternDaysRemained = "Акционный период на тарифе 'Народный (300)' заканчивается {0}";

		/// <summary>
		/// Проверка надобности отключения клиента, связанной со сметой тарифа
		/// </summary>
		/// <param name="session">Сессия БД</param>
		/// <param name="clientService">Клиентский сервис</param>
		public override void OnTimer(ISession session, ClientService clientService)
		{
			// получение сведения об изменении тарифов
			var planChangerList = session.Query<PlanChangerData>().ToList();
			foreach (var changer in planChangerList) {
				//поиск целевого тарифа
				if (changer.TargetPlan == clientService.Client.PhysicalClient.Tariff.Id
					&& clientService.Client.Disabled == false
					&& clientService.Client.Endpoints.Count > 0) {
					// добавление услуги
					if (!clientService.Client.ClientServices.Any(s => s.Service.HumanName == "PlanChanger")) {
						var planChanger = session.Query<Service>().FirstOrDefault(s => s.HumanName == "PlanChanger");
						clientService.Client.ClientServices.Add(new ClientService(clientService.Client, planChanger));
					} else {
						// если услуга существует, проверка, не подошел ли срок отключения клиента.
						if (clientService.Client.ClientServices.Any(s => s.Service.HumanName == "PlanChanger")
							&& clientService.Client.PhysicalClient.LastTimePlanChanged != Convert.ToDateTime("01.01.0001")
							&& (clientService.Client.PhysicalClient.LastTimePlanChanged.AddDays(changer.Timeout).Date < SystemTime.Now())) {
							if (!clientService.Client.ShowBalanceWarningPage) {
								clientService.Client.ShowBalanceWarningPage = true;
								SceHelper.UpdatePackageId(session, clientService.Client);
								clientService.Client.CreareAppeal("Клиент был заблокирован в связи с прекращением действия тарифа '"
									+ clientService.Client.PhysicalClient.Tariff.Name + "'.", AppealType.Statistic);
							}
						}
					}
					// сохранение изменений
					session.Update(clientService.Client);
				}
				if (changer.TargetPlan == clientService.Client.PhysicalClient.Tariff.Id &&
					clientService.Client.Endpoints.Count > 0) {
					ChangeClientTarrifIfNeeds(session, clientService.Client, changer);
					AddClientAppealIfNeeds(clientService.Client, changer);
					session.Update(clientService.Client);
				}
			}
		}

		public static void AddClientAppealIfNeeds(Client client, PlanChangerData changer)
		{
			if (changer.TargetPlan != client.PhysicalClient.Tariff.Id || changer.NotifyDays == null)
				return;
			var date = PlanchangerTimeOffDate(client, changer);
			if (!date.HasValue)
				return;
			if (date.Value.Date != SystemTime.Now().Date &&
				date.Value.Date <= SystemTime.Now().AddDays(changer.NotifyDays.Value).Date) {

				var message = string.Format(MessagePatternDaysRemained, date.Value.ToShortDateString());
				var appeals =
					client.ClientAppeals(date.Value.AddDays(-changer.NotifyDays.Value), date.Value)
						.Where(s => s.Appeal == message).ToList();

				if (appeals.Count == 0) {
					client.Appeals.Add(new Appeals() {
						Date = SystemTime.Now(),
						Appeal = message,
						Client = client,
						AppealType = AppealType.ClientToRead,
						FromNewAdminPanel = true
					});
					if (client.Disabled != true) {
						client.ShowBalanceWarningPage = true;
					}
				}
			}
		}

		public static void ChangeClientTarrifIfNeeds(ISession dbSession, Client client, PlanChangerData changer)
		{
			if (changer.TargetPlan != client.PhysicalClient.Tariff.Id || changer.NotifyDays == null)
				return;
			var date = PlanchangerTimeOffDate(client, changer);
			if (!date.HasValue)
				return;
			if (date.Value.Date >= SystemTime.Now().Date ||
				(client.Status.Type != StatusType.Worked && client.Status.Type != StatusType.VoluntaryBlocking) || date.Value.Date<=client.StatusChangedOn) return;

			var plan = dbSession.Query<Tariff>().First(s => s.Id == changer.FastPlan);
			var oldPlan = client.PhysicalClient.Tariff;
			client.PhysicalClient.LastTimePlanChanged = SystemTime.Now();
			client.PhysicalClient.Tariff = plan;
			if (client.Internet.ActivatedByUser)
				client.Endpoints.Where(s => !s.Disabled).Each(e => {
					//если включен варнинг или клиент неактивен, задается только резервный PackageId, по которому в момент активации будет задан рабочий PackageId 
					if (client.ShowBalanceWarningPage) {
						e.StableTariffPackageId = plan.PackageId;
					} else {
						e.SetStablePackgeId(plan.PackageId);
					}
				});
			dbSession.Save(client);
			// добавление записи в историю тарифов пользователя
			var planHistory = new PlanHistoryEntry {
				Client = client,
				DateOfChange = SystemTime.Now(),
				PlanAfter = plan,
				PlanBefore = oldPlan,
				Price = 0
			};
			dbSession.Save(planHistory);
			var msg =
				string.Format(
					"В связи с тем, что клиентом не был сделан выбор тарифа в течении 3 суток,</ br> а действие акционного тарифа закончилось, клиент автоматически был переведен с '{0}'({1}) на '{2}'({3}). Стоимость перехода: {4} руб.",
					oldPlan.Name, oldPlan.Price, plan.Name, plan.Price, 0);
			var appeal = new Appeals(msg, client, AppealType.Statistic) {FromNewAdminPanel = true};
			client.Appeals.Add(appeal);
			client.IsNeedRecofiguration = true;
			dbSession.Save(client);
		}

		/// <summary>
		/// Дата завершения работы по акционному тарифу (когда клиенту начтен отображаться варнинг)
		/// </summary>
		/// <param name="client"></param>
		/// <param name="changer"></param>
		/// <returns></returns>
		public static DateTime? PlanchangerTimeOffDate(Client client, PlanChangerData changer)
		{
			if (client.PhysicalClient == null || changer == null || changer.TargetPlan != client.PhysicalClient.Tariff.Id)
				return null;
			if (client.PhysicalClient.LastTimePlanChanged == DateTime.MinValue) {
				return null;
			}
				// добавление услуги
				if (!client.ClientServices.Any(s => s.Service.HumanName == "PlanChanger")) {
					var planChanger = Service.GetByType(typeof (PlanChanger));
					client.ClientServices.Add(new ClientService(client, planChanger));
				}
				return client.PhysicalClient.LastTimePlanChanged.AddDays(changer.Timeout);
		}

		/// <summary>
		/// Проверка, если клиенту уже отображается варнинг
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public static bool CheckPlanChangerWarningPage(ISession dbSession, Client client)
		{
			var changer = dbSession.Query<PlanChangerData>().FirstOrDefault(s => s.TargetPlan == client.PhysicalClient.Tariff.Id);
			var dateOff = PlanchangerTimeOffDate(client, changer);
			return dateOff.HasValue && dateOff.Value < Convert.ToDateTime(SystemTime.Now()) && client.ShowBalanceWarningPage;
		}
	}
}