﻿using System;
using System.Configuration;
using System.Globalization;
using System.Text;
using Common.Tools;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models.Services
{
	[Subclass(0, ExtendsType = typeof(Service), DiscriminatorValue = "WorkLawyer")]
	public class WorkLawyer : Service
	{
		public override bool CanActivate(ClientService assignedService)
		{
			return assignedService.Client.LegalClient != null && assignedService.Client.LegalClient.Plan != null;
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			return assignedService.Client.LegalClient != null;
		}

		public override void Deactivate(ClientService assignedService, ISession session)
		{
			var client = assignedService.Client;

			var warningParamRaw = ConfigurationManager.AppSettings["LawyerPersonBalanceWarningRate"];
			var warningParam = (decimal)float.Parse(warningParamRaw, CultureInfo.InvariantCulture);

			var disableParamRaw = ConfigurationManager.AppSettings["LawyerPersonBalanceBlockingRate"];
			var disableParam = (decimal)float.Parse(disableParamRaw, CultureInfo.InvariantCulture);

			var warning = client.LegalClient.NeedShowWarning() && client.LegalClient.Plan.HasValue
			              && (client.Balance < -(client.LegalClient.Plan * warningParam));
			var disable = warning && (client.Balance <= -(client.LegalClient.Plan * disableParam));

			client.ShowBalanceWarningPage = warning;
			client.Disabled = disable;
			if (warning) {
				client.Appeals.Add(new Appeal(string.Format("В результате деактивации услуги {0} клиент был заблокирован.", assignedService.Service.Name), client, AppealType.System));
			}
			client.IsNeedRecofiguration = true;
			assignedService.IsActivated = false;
		}

		public override void Activate(ClientService assignedService, ISession session)
		{
			if ((!assignedService.IsActivated && CanActivate(assignedService))) {
				var client = assignedService.Client;
				client.ShowBalanceWarningPage = false;
				client.Disabled = false;
				assignedService.IsActivated = true;
				client.IsNeedRecofiguration = true;
				assignedService.EndDate = assignedService.EndDate.Value.Date;
			}
		}

		// WriteOff - не перенесена из InternetInterface, используется в биллинге
		//public override void WriteOff(ClientService assignedService)
		//{
		//	if ((assignedService.EndDate == null) ||
		//	    (assignedService.EndDate != null && (SystemTime.Now().Date >= assignedService.EndDate.Value.Date))) {
		//		ForceDeactivate(assignedService);
		//		assignedService.Client.ClientServices.Remove(assignedService);
		//	}
		//}
	}
}