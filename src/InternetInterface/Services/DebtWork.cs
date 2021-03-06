﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using InternetInterface.Models;

namespace InternetInterface.Services
{
	[ActiveRecord(DiscriminatorValue = "DebtWork")]
	public class DebtWork : Service
	{
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
			return InternalCanActivate(client) && !client.HaveActiveService<DebtWork>();
		}

		//будь бдителен два вызова CanActivate похожи но не идентичные (!!! 24.06.2015 - ТЕПЕРЬ ИДЕНТИЧНЫЕ!!!!)
		//CanActivate(Client client) - происходит если нужно узнать может ли услуга быть активирована
		//CanActivate(ClientService assignedService) - происходит когда услуга активируется
		//разница в проверке дублей когда услуга активируется она уже будет в списке ClientService
		public override bool CanActivate(ClientService assignedService)
		{
			return InternalCanActivate(assignedService.Client) && !assignedService.Client.HaveActiveService<DebtWork>();
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
			return false;
		}

		public override void ForceDeactivate(ClientService assignedService)
		{
			var client = assignedService.Client;
			client.UpdateStatus();
			assignedService.IsActivated = false;
			assignedService.IsDeactivated = true;

			ActiveRecordMediator.Update(client);
			ActiveRecordMediator.Update(assignedService);
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			return assignedService.IsActivated && assignedService.EndWorkDate.GetValueOrDefault() < SystemTime.Now();
		}

		public override void Activate(ClientService assignedService)
		{
			assignedService.EndWorkDate = assignedService.EndWorkDate ?? DateTime.Now.AddDays(3);
			if (!assignedService.IsActivated && !assignedService.IsDeactivated && CanActivate(assignedService)) {
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