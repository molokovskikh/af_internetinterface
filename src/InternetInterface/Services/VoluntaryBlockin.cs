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

		public override bool CanActivate(Client client)
		{
			if (client.PhysicalClient != null) {
				var balance = client.PhysicalClient.Balance >= 0;
				var debtWork = !client.HaveService<DebtWork>();
				return balance && debtWork && client.StartWork();
			}
			return false;
		}

		public override bool CanActivate(ClientService assignedService)
		{
			var begin = SystemTime.Now() >= assignedService.BeginWorkDate.Value;
			return begin && CanActivate(assignedService.Client);
		}

		public override void Activate(ClientService assignedService)
		{
			if (CanActivate(assignedService) && !assignedService.Activated) {
				var client = assignedService.Client;

				client.RatedPeriodDate = DateTime.Now;

				//Это должно быть на этом месте, иначе возможно списывать неправильную сумму
				var now = SystemTime.Now();
				if (!client.PaidDay && now.Hour < 22 && assignedService.BeginWorkDate.Value.Date == now.Date) {
					client.PaidDay = true;
					var toDt = client.GetInterval();
					var price = client.GetPrice();
					var comment = string.Format("Абонентская плата за {0} из-за добровольной блокировки клиента", DateTime.Now.ToShortDateString());
					var writeOff = new UserWriteOff {
						Client = client,
						Date = DateTime.Now,
						Sum = price / toDt,
						Comment = comment
					};
					ActiveRecordMediator.Save(writeOff);
				}

				client.Disabled = true;
				client.AutoUnblocked = false;
				client.DebtDays = 0;
				client.Status = Status.Find((uint)StatusType.VoluntaryBlocking);
				client.Update();

				assignedService.Activated = true;
				var evd = assignedService.EndWorkDate.Value;
				assignedService.EndWorkDate = new DateTime(evd.Year, evd.Month, evd.Day);
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

		public override void CompulsoryDeactivate(ClientService assignedService)
		{
			var client = assignedService.Client;
			client.DebtDays = 0;
			client.RatedPeriodDate = DateTime.Now;
			client.Disabled = client.PhysicalClient.Balance < 0;
			client.ShowBalanceWarningPage = client.PhysicalClient.Balance < 0;
			client.AutoUnblocked = true;

			if (assignedService.Client.PhysicalClient.Balance > 0) {
				client.Disabled = false;
				client.Status = Status.Find((uint)StatusType.Worked);
			}

			if (!client.PaidDay && assignedService.Activated) {
				assignedService.Activated = false;

				//что бы правильно вычислить стоимость нам нужно активировать
				//услуги которые могут быть активированы после
				//отключения добровольной блокировки
				var forActivationCheck = client.ClientServices.Where(s => s.Service.Id != Id);
				foreach (var clientService in forActivationCheck) {
					clientService.Activate();
				}
				var daysInInterval = client.GetInterval();
				var price = client.GetPrice();
				var sum = price / daysInInterval;
				if (sum > 0) {
					client.PaidDay = true;
					var comment = string.Format("Абонентская плата за {0} из-за добровольной разблокировки клиента", DateTime.Now.ToShortDateString());
					ActiveRecordMediator.Save(new UserWriteOff(client, sum, comment));
				}
			}

			assignedService.Activated = false;
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
			if (assignedService.Activated
				&& client.FreeBlockDays > 0
				&& assignedService.BeginWorkDate.Value.Date != SystemTime.Today()
				&& assignedService.EndWorkDate.Value.Date != SystemTime.Today()) {
				client.FreeBlockDays--;
				client.Update();
			}
		}
	}
}