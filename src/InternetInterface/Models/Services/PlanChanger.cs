using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(DiscriminatorValue = "PlanChanger")]
	public class PlanChanger : Service
	{ 
		public override void OnTimer(ISession session, ClientService clientService)
		{
			// получение сведения об изменении тарифов
			var planChangerList = session.Query<PlanChangerData>().ToList();

			foreach (var changer in planChangerList) {
				//поиск целевого тариф
				if (changer.TargetPlan == clientService.Client.PhysicalClient.Tariff.Id) {
					// добавление услуги
					if (!clientService.Client.ClientServices.Any(s => s.Service.HumanName == "PlanChanger")) {
						var planChanger = session.Query<Service>().FirstOrDefault(s => s.HumanName == "PlanChanger");
						clientService.Client.ClientServices.Add(new ClientService(clientService.Client, planChanger));
					}
					else {
						// если услуга существует,проверка, не подошел ли срок отключения клиента.
						if (clientService.Client.ClientServices.Any(s => s.Service.HumanName == "PlanChanger")
						    && clientService.Client.BeginWork.HasValue
						    && (clientService.Client.BeginWork.Value.AddDays(changer.Timeout) < SystemTime.Now())) {
							clientService.Client.SetStatus(Status.Get(StatusType.NoWorked, session));
							clientService.Client.CreareAppeal("Клиент был заблокирован в связи с прекращением действия тарифа '"
							                                  + clientService.Client.PhysicalClient.Tariff.Name + "'.", AppealType.Statistic);
						}
					}
					// сохранение изменений
					session.Update(clientService.Client);
				}
			}
		}
	}
}