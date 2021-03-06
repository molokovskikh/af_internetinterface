﻿using System.Configuration;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Services;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(DiscriminatorValue = "SpeedBoost")]
	public class SpeedBoost : Service
	{
		public override void Activate(ClientService assignedService)
		{
			base.Activate(assignedService);
			var packageId = NullableConvert.ToInt32(ConfigurationManager.AppSettings["SpeedBoostPackageId"]);
			if (packageId == null)
				return;
			assignedService.Client.Endpoints.Where(s => !s.Disabled).Each(e => {
				e.SetStablePackgeId(packageId);
			});
			assignedService.Client.IsNeedRecofiguration = true;
		}

		public override bool CanActivate(Client client)
		{
			if (client.ClientServices.Any(s => s.Service.Id == this.Id && !s.IsActivated && s.BeginWorkDate != null && s.BeginWorkDate > SystemTime.Now())) {
				return false;
			}
			return true;
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			return (assignedService.EndWorkDate != null && SystemTime.Now().Date >= assignedService.EndWorkDate.Value.Date);
		}

		public override void ForceDeactivate(ClientService assignedService)
		{
			base.ForceDeactivate(assignedService);
			assignedService.Client.PhysicalClient.UpdatePackageId();
			assignedService.Client.IsNeedRecofiguration = true;
		}

		public override bool CanActivateInWeb(Client client)
		{
			if (client.Type == ClientType.Legal)
				return false;
			return true;
		}
	}
}