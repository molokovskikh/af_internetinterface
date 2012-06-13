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

		public override bool CanActivate(ClientService service)
		{
			var client = service.Client;
			var payTar = client.PaymentForTariff();
			if (service.Activator != null)
				payTar = true;
			return payTar && CanActivate(client);
		}

		public override void PaymentClient(ClientService service)
		{
			if (service.Client.PhysicalClient.Balance > 0)
				service.Deactivate();
		}

		public override bool CanBlock(ClientService service)
		{
			if (service.EndWorkDate == null)
				return false;
			return service.EndWorkDate.Value < SystemTime.Now();
		}

		public override bool CanDelete(ClientService CService)
		{
			if (CService.Activator != null)
				return true;

			var lastPayments = Payment.Queryable
				.Where(p => p.Client == CService.Client && CService.BeginWorkDate.Value < p.PaidOn)
				.ToList().Sum(p => p.Sum);
			var balance = CService.Client.PhysicalClient.Balance;
			if (balance > 0 &&
				balance - lastPayments <= 0)
				return true;
			return false;
		}

		public override void CompulsoryDeactivate(ClientService service)
		{
			var client = service.Client;
			client.Disabled = true;
			client.AutoUnblocked = true;
			client.Status = Status.Find((uint)StatusType.NoWorked);
			client.Update();
			service.Activated = false;
			service.Update();
		}

		public override bool Deactivate(ClientService assignedService)
		{
			if (assignedService.Activated && assignedService.EndWorkDate.Value < SystemTime.Now())
			{
				CompulsoryDeactivate(assignedService);
				return true;
			}
			return !assignedService.Activated;
		}

		public override void Activate(ClientService assignedService)
		{
			if ((!assignedService.Activated && CanActivate(assignedService)))
			{
				var client = assignedService.Client;
				client.Disabled = false;
				client.RatedPeriodDate = SystemTime.Now();
				client.Status = Status.Find((uint) StatusType.Worked);
				client.Update();
				assignedService.Activated = true;
				assignedService.Update();
			}
		}
	}
}