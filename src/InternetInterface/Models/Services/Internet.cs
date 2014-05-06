using System;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Services;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(DiscriminatorValue = "Internet")]
	public class Internet : Service
	{
		public override bool SupportUserAcivation
		{
			get { return true; }
		}

		public override bool CanDelete(ClientService assignedService)
		{
			return false;
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			return assignedService.IsActivated &&
				(!assignedService.ActivatedByUser
					|| assignedService.Client.Disabled);
		}

		//если клиент отключил себе интернет то нужно списать абонентскую плату
		//за день что бы не было смысла отключать себе интернет на момент списания
		public override void ForceDeactivate(ClientService assignedService)
		{
			var client = assignedService.Client;
			if (!client.Disabled
				&& !assignedService.ActivatedByUser) {
				//если null значит клиент не начал работать и не за что списывать
				if (client.RatedPeriodDate != null) {
					var comment = string.Format("Абонентская плата за {0} из-за отключения услуги {1}", DateTime.Now.ToShortDateString(), HumanName);
					var sum = client.GetPriceForTariff() / client.GetInterval();
					client.UserWriteOffs.Add(new UserWriteOff(client, sum, comment, false));
				}
			}
			base.ForceDeactivate(assignedService);
			assignedService.Client.CreareAppeal(String.Format("Включена услуга \"{0}\"", HumanName));
		}

		public override void Activate(ClientService assignedService)
		{
			if (assignedService.ActivatedByUser
				&& !assignedService.Client.Disabled) {
				base.Activate(assignedService);
				assignedService.Client.CreareAppeal(String.Format("Включена услуга \"{0}\"", HumanName));
			}
		}

		public override decimal GetPrice(ClientService assignedService)
		{
			if (assignedService.Client.Disabled
				|| !assignedService.ActivatedByUser)
				return 0;
			return assignedService.Client.GetTariffPrice();
		}
	}
}