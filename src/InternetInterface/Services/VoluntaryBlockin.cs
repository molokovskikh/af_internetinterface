using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Scopes;
using Common.Tools;
using InternetInterface.Models;

namespace InternetInterface.Services
{
	[ActiveRecord(DiscriminatorValue = "VoluntaryBlockin")]
	public class VoluntaryBlockin : Service
	{
		public override string GetParameters()
		{
			var builder = new StringBuilder();
			builder.Append("<tr>");
			builder.Append(
				string.Format(
					"<td><label for=\"endDate\" id=\"endDateLabel\"> Заблокировать до  </label><input type=text  name=\"endDate\" value=\"{0}\"  id=\"endDate\" class=\"date-pick dp-applied\"></td>",
					DateTime.Now.AddDays(1).ToShortDateString()));
			builder.Append("</tr>");
			return builder.ToString();
		}

		private static bool InternalCanActivate(Client client)
		{
			return client.PhysicalClient != null
				&& client.PhysicalClient.Balance >= 0
				&& !client.Disabled
				&& !client.HaveActiveService<DebtWork>()
				&& client.StartWork();
		}

		public override bool CanActivate(Client client)
		{
			return InternalCanActivate(client) && !client.HaveService<VoluntaryBlockin>();
		}

		//будь бдителен два вызова CanActivate похожи но не идентичные
		//CanActivate(Client client) - происхоидт если нужно узнать может ли услуга быть активирована
		//CanActivate(ClientService assignedService) - проиходит когда услуга активируется
		//разница в проверке дублей когда услуга активируется она уже будет в списке ClientService
		public override bool CanActivate(ClientService assignedService)
		{
			return SystemTime.Now() >= assignedService.BeginWorkDate.Value
				&& InternalCanActivate(assignedService.Client)
				&& !assignedService.Client.ClientServices
					.Except(new[] { assignedService })
					.Any(s => s.IsService(assignedService.Service));
		}

		public override void Activate(ClientService assignedService)
		{
			if (CanActivate(assignedService) && !assignedService.IsActivated) {
				var client = assignedService.Client;

				client.RatedPeriodDate = DateTime.Now;

				//Это должно быть на этом месте, иначе возможно списывать неправильную сумму
				var now = SystemTime.Now();
				if (!client.PaidDay && now.Hour < 22 && assignedService.BeginWorkDate.Value.Date == now.Date) {
					client.PaidDay = true;
					var comment = string.Format("Абонентская плата за {0} из-за добровольной блокировки клиента", DateTime.Now.ToShortDateString());
					var writeOff = new UserWriteOff {
						Client = client,
						Date = DateTime.Now,
						Sum = client.GetSumForRegularWriteOff(),
						Comment = comment
					};
					ActiveRecordMediator.Save(writeOff);
				}

				client.SetStatus(Status.Find((uint)StatusType.VoluntaryBlocking));
				client.Update();

				assignedService.IsActivated = true;
				assignedService.EndWorkDate = assignedService.EndWorkDate.Value.Date;
				ActiveRecordMediator.Save(assignedService);

				if (client.FreeBlockDays <= 0) {
					var comment = string.Format("Платеж за активацию услуги добровольная блокировка с {0} по {1}",
						assignedService.BeginWorkDate.Value.ToShortDateString(), assignedService.EndWorkDate.Value.ToShortDateString());
					var writeOff = new UserWriteOff {
						Client = client,
						Sum = 50m,
						Date = DateTime.Now,
						Comment = comment
					};
					ActiveRecordMediator.Save(writeOff);
				}
			}
		}

		public override void ForceDeactivate(ClientService assignedService)
		{
			var client = assignedService.Client;
			client.DebtDays = 0;
			client.RatedPeriodDate = DateTime.Now;
			client.ShowBalanceWarningPage = client.PhysicalClient.Balance < 0;
			client.SetStatus(client.Balance > 0 ? Status.Find((uint)StatusType.Worked) : Status.Find((uint)StatusType.NoWorked));

			if (!client.PaidDay && assignedService.IsActivated) {
				assignedService.IsActivated = false;

				//что бы правильно вычислить стоимость нам нужно активировать
				//услуги которые могут быть активированы после
				//отключения добровольной блокировки
				var forActivationCheck = client.ClientServices.Where(s => s.Service.Id != Id);
				foreach (var clientService in forActivationCheck) {
					clientService.TryActivate();
				}
				var sum = client.GetSumForRegularWriteOff();
				if (sum > 0) {
					client.PaidDay = true;
					var comment = string.Format("Абонентская плата за {0} из-за добровольной разблокировки клиента", DateTime.Now.ToShortDateString());
					ActiveRecordMediator.Save(new UserWriteOff(client, sum, comment));
				}
			}

			assignedService.IsActivated = false;
			ActiveRecordMediator.Update(assignedService);
			ActiveRecordMediator.Update(client);
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			return assignedService.Client.PhysicalClient.Balance < 0
				|| (assignedService.EndWorkDate != null && SystemTime.Now().Date >= assignedService.EndWorkDate.Value);
		}

		public override decimal GetPrice(ClientService assignedService)
		{
			if (assignedService.BeginWorkDate == null)
				return 0;

			if (assignedService.Client.RatedPeriodDate == null)
				return 0;

			if (assignedService.Client.FreeBlockDays > 0)
				return 0;

			if (assignedService.Client.PhysicalClient.Balance < 0)
				return 0;

			return assignedService.Client.GetInterval() * 3m;
		}

		public override void WriteOff(ClientService assignedService)
		{
			var client = assignedService.Client;
			if (assignedService.IsActivated
				&& client.FreeBlockDays > 0
				&& assignedService.BeginWorkDate.Value.Date != SystemTime.Today()
				&& assignedService.EndWorkDate.Value.Date != SystemTime.Today()) {
				client.FreeBlockDays--;
				client.Update();
			}
		}
	}
}