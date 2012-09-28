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

		public override bool CanActivate(Client client)
		{
			if (client.PhysicalClient != null) {
				var conVol = !client.ClientServices.Select(c => c.Service).Contains(GetByType(typeof(VoluntaryBlockin)));
				return conVol && client.StartWork();
			}
			return false;
		}

		public override bool CanActivate(ClientService assignedService)
		{
			var client = assignedService.Client;
			var payTar = client.PaymentForTariff();
			if (assignedService.Activator != null)
				payTar = true;
			return payTar && CanActivate(client);
		}

		public override void PaymentClient(ClientService assignedService)
		{
			if (assignedService.Client.CanDisabled())
				assignedService.Deactivate();
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
				.Where(p => p.Client == assignedService.Client && assignedService.BeginWorkDate.Value < p.PaidOn)
				.ToList().Sum(p => p.Sum);
			var balance = assignedService.Client.PhysicalClient.Balance;
			if (balance > 0 &&
				balance - lastPayments <= 0)
				return true;
			return false;
		}

		public override void CompulsoryDeactivate(ClientService assignedService)
		{
			var client = assignedService.Client;
			client.Disabled = client.CanDisabled();
			client.AutoUnblocked = true;
			var status = client.Disabled ? Status.Find((uint)StatusType.NoWorked) : Status.Find((uint)StatusType.Worked);
			client.Status = status;
			client.Update();
			assignedService.Activated = false;
			assignedService.Diactivated = true;
			var writeOff = new UserWriteOff(client, assignedService.DebtInfo.Sum, "Списание при отключении обещаного платежа");
			writeOff.Save();
			ActiveRecordMediator.Update(assignedService);
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			return assignedService.Activated && assignedService.EndWorkDate.Value < SystemTime.Now();
		}

		public override void Activate(ClientService assignedService)
		{
			if ((!assignedService.Activated && !assignedService.Diactivated && CanActivate(assignedService))) {
				var client = assignedService.Client;
				client.Disabled = false;
				client.RatedPeriodDate = SystemTime.Now();
				client.Status = Status.Find((uint)StatusType.Worked);
				client.Update();
				assignedService.Activated = true;
				if (assignedService.DebtInfo == null) {
					var oneDaySum = assignedService.Client.GetPrice() / assignedService.Client.GetInterval();
					var debtInfo = new DebtWorkInfo(assignedService, oneDaySum);
					assignedService.DebtInfo = debtInfo;
				}
				var payment = new Payment(assignedService.Client, assignedService.DebtInfo.Sum, true);
				ActiveRecordMediator.Save(payment);
				ActiveRecordMediator.Save(assignedService);
			}
		}
	}
}