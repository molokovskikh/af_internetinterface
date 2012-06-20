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
			if (client.PhysicalClient != null)
			{
				var balance = client.PhysicalClient.Balance < 0;
				var conVol =
					!client.ClientServices.Select(c => c.Service).Contains(GetByType(typeof (VoluntaryBlockin)));
				return balance && conVol && client.StartWork();
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
			if (assignedService.Client.PhysicalClient.Balance > 0)
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
			client.Disabled = true;
			client.AutoUnblocked = true;
			client.Status = Status.Find((uint)StatusType.NoWorked);
			client.Update();
			assignedService.Activated = false;
			ActiveRecordMediator.Update(assignedService);
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			return assignedService.Activated && assignedService.EndWorkDate.Value < SystemTime.Now();
		}

		public override void Activate(ClientService assignedService)
		{
			if ((!assignedService.Activated && CanActivate(assignedService))) {
				var client = assignedService.Client;
				client.Disabled = false;
				client.RatedPeriodDate = SystemTime.Now();
				client.Status = Status.Find((uint) StatusType.Worked);
				client.Update();
				assignedService.Activated = true;
				ActiveRecordMediator.Update(assignedService);
			}
		}
	}
}