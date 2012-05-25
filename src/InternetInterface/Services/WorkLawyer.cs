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

		public override bool CanActivate(ClientService cService)
		{
			return CanActivate(cService.Client);
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

		public override void CompulsoryDiactivate(ClientService CService)
		{
			var client = CService.Client;
			var warning = client.LawyerPerson.NeedShowWarning();
			client.ShowBalanceWarningPage = warning;
			client.Disabled = warning;
			client.Update();
			CService.Activated = false;
			CService.Update();
		}

		public override void Activate(ClientService CService)
		{
			if ((!CService.Activated && CanActivate(CService))) {
				var client = CService.Client;
				client.ShowBalanceWarningPage = false;
				client.Disabled = false;
				client.Save();
				CService.Activated = true;
				CService.EndWorkDate = CService.EndWorkDate.Value.Date;
				CService.Update();
			}
		}

		public override void WriteOff(ClientService cService)
		{
			if ((cService.EndWorkDate == null) ||
				(cService.EndWorkDate != null && (SystemTime.Now().Date >= cService.EndWorkDate.Value.Date)))
			{
				CompulsoryDiactivate(cService);
				cService.Delete();
			}
		}
	}
}