using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
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

		public override bool CanActivate(Client client)
		{
			return client.LawyerPerson != null && client.LawyerPerson.Tariff != null;
		}

		public override void ForceDeactivate(ClientService assignedService)
		{
			var client = assignedService.Client;

			var warningParamRaw = ConfigurationManager.AppSettings["LawyerPersonBalanceWarningRate"];
			var warningParam = (decimal)float.Parse(warningParamRaw, CultureInfo.InvariantCulture);

			var disableParamRaw = ConfigurationManager.AppSettings["LawyerPersonBalanceBlockingRate"];
			var disableParam = (decimal)float.Parse(disableParamRaw, CultureInfo.InvariantCulture);

			var warning = client.LawyerPerson.NeedShowWarning() && client.LawyerPerson.Tariff.HasValue
			              && (client.Balance < -(client.LawyerPerson.Tariff * warningParam));
			var disable = warning && (client.Balance <= -(client.LawyerPerson.Tariff * disableParam));

			client.ShowBalanceWarningPage = warning;
			client.Disabled = disable;
			client.Update();
			if (warning) {
				client.CreareAppeal(string.Format("В результате деактивации услуги {0} клиент был заблокирован.",
					assignedService.Service.Name));
			}
			client.IsNeedRecofiguration = true;
			assignedService.IsActivated = false;
			ActiveRecordMediator.Save(assignedService);
		}

		public override void Activate(ClientService assignedService)
		{
			if ((!assignedService.IsActivated && CanActivate(assignedService))) {
				var client = assignedService.Client;
				client.ShowBalanceWarningPage = false;
				client.Disabled = false;
				client.Save();
				assignedService.IsActivated = true;
				assignedService.EndWorkDate = assignedService.EndWorkDate.Value.Date;
				ActiveRecordMediator.Save(assignedService);
			}
		}

		public override void WriteOff(ClientService assignedService)
		{
			if ((assignedService.EndWorkDate == null) ||
			    (assignedService.EndWorkDate != null && (SystemTime.Now().Date >= assignedService.EndWorkDate.Value.Date))) {
				ForceDeactivate(assignedService);
				assignedService.Client.ClientServices.Remove(assignedService);
				ActiveRecordMediator.Save(assignedService.Client);
				ActiveRecordMediator.Delete(assignedService);
			}
		}
	}
}