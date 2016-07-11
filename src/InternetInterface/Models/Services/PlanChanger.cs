﻿using System;
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
				if (changer.TargetPlan == clientService.Client.PhysicalClient.Tariff.Id) {
					// добавление услуги
					if (!clientService.Client.ClientServices.Any(s => s.Service.HumanName == "PlanChanger")) {
						var planChanger = session.Query<Service>().FirstOrDefault(s => s.HumanName == "PlanChanger");
						clientService.Client.ClientServices.Add(new ClientService(clientService.Client, planChanger));
					}
					else {
						// если услуга существует, проверка, не подошел ли срок отключения клиента.
						if (clientService.Client.ClientServices.Any(s => s.Service.HumanName == "PlanChanger")
						    && clientService.Client.PhysicalClient.LastTimePlanChanged != Convert.ToDateTime("01.01.0001")
						    && (clientService.Client.PhysicalClient.LastTimePlanChanged.AddDays(changer.Timeout) < SystemTime.Now())) {
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
			}
		}

		// проверка, если клиент уже заблокирован
		public static bool CheckPlanChangerWarningPage(ISession session, Client client)
		{
			if (client.PhysicalClient == null)
				return false;
			// получение сведения об изменении тарифов
			var planChangerList = session.Query<PlanChangerData>().ToList();
			foreach (var changer in planChangerList) {
				//поиск целевого тарифа
				if (changer.TargetPlan == client.PhysicalClient.Tariff.Id) {
					// добавление услуги
					if (!client.ClientServices.Any(s => s.Service.HumanName == "PlanChanger")) {
						var planChanger = session.Query<Service>().FirstOrDefault(s => s.HumanName == "PlanChanger");
						client.ClientServices.Add(new ClientService(client, planChanger));
					}
					else {
						// если услуга существует, проверка, не подошел ли срок отключения клиента.
						if (client.ClientServices.Any(s => s.Service.HumanName == "PlanChanger")
						    && client.PhysicalClient.LastTimePlanChanged != Convert.ToDateTime("01.01.0001")
						    && (client.PhysicalClient.LastTimePlanChanged.AddDays(changer.Timeout) < SystemTime.Now())) {
							// клиент забокирован PlanChanger
							return client.ShowBalanceWarningPage;
						}
					}
				}
			}
			return false;
		}
	}
}