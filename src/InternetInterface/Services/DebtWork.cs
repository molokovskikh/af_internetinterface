using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Models;

namespace InternetInterface.Services
{
	[ActiveRecord(DiscriminatorValue = "DebtWork")]
	public class DebtWork : Service
	{
		public override string GetParameters()
		{
			var builder = new StringBuilder();
			builder.Append("<tr>");
			builder.Append(
				string.Format(
					"<td><label for=\"endDate\"> Конец периода </label><input type=text  name=\"endDate\" id=\"endDate\" value=\"{0}\" class=\"date-pick dp-applied\"></td>",
					DateTime.Now.AddDays(1).ToShortDateString()));
			builder.Append("</tr>");
			return builder.ToString();
		}

		private static bool InternalCanActivate(Client client)
		{
			return client.PhysicalClient != null
				&& client.Disabled
				&& client.Balance <= 0
				&& !client.HaveService<VoluntaryBlockin>()
				&& client.AutoUnblocked;
		}

		public override bool CanActivate(Client client)
		{
			return InternalCanActivate(client) && !client.HaveService<DebtWork>();
		}

		//будь бдителен два вызова CanActivate похожи но не идентичные
		//CanActivate(Client client) - происхоидт если нужно узнать может ли услуга быть активирована
		//CanActivate(ClientService assignedService) - проиходит когда услуга активируется
		//разница в проверке дублей когда услуга активируется она уже будет в списке ClientService
		public override bool CanActivate(ClientService assignedService)
		{
			return InternalCanActivate(assignedService.Client)
				&& !assignedService.Client.ClientServices
					.Except(new[] { assignedService })
					.Any(s => s.IsService(assignedService.Service));
		}

		public override void PaymentClient(ClientService assignedService)
		{
			if (!assignedService.Client.CanDisabled())
				assignedService.ForceDeactivate();
		}

		public override bool CanBlock(ClientService assignedService)
		{
			if (assignedService.EndWorkDate == null)
				return false;
			return assignedService.EndWorkDate.Value < SystemTime.Now();
		}

		public override bool CanDelete(ClientService assignedService)
		{
			if (assignedService.Activator != null)
				return true;

			var lastPayments = Payment.Queryable
				.Where(p => p.Client == assignedService.Client && assignedService.BeginWorkDate.Value <= p.PaidOn)
				.ToList().Sum(p => p.Sum);
			var balance = assignedService.Client.PhysicalClient.Balance;
			if (balance > 0 &&
				balance - lastPayments <= 0)
				return true;
			return false;
		}

		public override void ForceDeactivate(ClientService assignedService)
		{
			var client = assignedService.Client;
			client.UpdateStatus();
			client.Update();
			assignedService.IsActivated = false;
			assignedService.IsDeactivated = true;
			ActiveRecordMediator.Update(assignedService);
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			return assignedService.IsActivated && assignedService.EndWorkDate.Value < SystemTime.Now();
		}

		public override void Activate(ClientService assignedService)
		{
			if ((!assignedService.IsActivated && !assignedService.IsDeactivated && CanActivate(assignedService))) {
				if (assignedService.EndWorkDate > SystemTime.Now().AddDays(3)) {
					var userWriteOff = new UserWriteOff(assignedService.Client, 50, "Активация обещанного платежа на 10 дней", false);
					ActiveRecordMediator.Save(userWriteOff);
				}
				var client = assignedService.Client;
				client.Disabled = false;
				client.RatedPeriodDate = SystemTime.Now();
				client.Status = Status.Find((uint)StatusType.Worked);
				client.Update();
				assignedService.IsActivated = true;
			}
		}
	}
}