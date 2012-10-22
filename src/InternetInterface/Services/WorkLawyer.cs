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
	[ActiveRecord(DiscriminatorValue = "WorkLawyer")]
	public class WorkLawyer : Service
	{
		public override string GetParameters()
		{
			var builder = new StringBuilder();
			builder.Append("<tr>");
			builder.Append(
				string.Format(
					"<td><label for=\"endDate\" id=\"endDateLabel\"> Разрешить работать до  </label><input type=text  name=\"endDate\" value=\"{0}\"  id=\"endDate\" class=\"date-pick dp-applied\"></td>",
					DateTime.Now.AddDays(1).ToShortDateString()));
			builder.Append("</tr>");
			return builder.ToString();
		}

		public override bool CanActivate(ClientService assignedService)
		{
			return CanActivate(assignedService.Client);
		}

		public override bool CanActivate(Client client)
		{
			var person = client.LawyerPerson;
			if (person != null) {
				if (person.Tariff == null)
					return false;
				return person.NeedShowWarning();
			}
			return false;
		}

		public override void CompulsoryDeactivate(ClientService assignedService)
		{
			var client = assignedService.Client;
			var warning = client.LawyerPerson.NeedShowWarning();
			client.ShowBalanceWarningPage = warning;
			client.Disabled = warning;
			client.Update();
			assignedService.Activated = false;
			ActiveRecordMediator.Save(assignedService);
		}

		public override void Activate(ClientService assignedService)
		{
			if ((!assignedService.Activated && CanActivate(assignedService))) {
				var client = assignedService.Client;
				client.ShowBalanceWarningPage = false;
				client.Disabled = false;
				client.Save();
				assignedService.Activated = true;
				assignedService.EndWorkDate = assignedService.EndWorkDate.Value.Date;
				ActiveRecordMediator.Save(assignedService);
			}
		}

		public override void WriteOff(ClientService assignedService)
		{
			if ((assignedService.EndWorkDate == null) ||
				(assignedService.EndWorkDate != null && (SystemTime.Now().Date >= assignedService.EndWorkDate.Value.Date))) {
				CompulsoryDeactivate(assignedService);
				assignedService.Client.ClientServices.Remove(assignedService);
				ActiveRecordMediator.Save(assignedService.Client);
				ActiveRecordMediator.Delete(assignedService);
			}
		}
	}
}