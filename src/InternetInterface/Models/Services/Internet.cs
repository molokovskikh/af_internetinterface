﻿using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Proxy;

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
					|| assignedService.Client.Disabled
				    //Статус "Заблокирован - Восстановление работы" является причиной отключения интернета, при его наличии услугу нужно деактивировать
					|| assignedService.Client.Status.Type == StatusType.BlockedForRepair);
		}

		//если клиент отключил себе интернет то нужно списать абонентскую плату
		//за день что бы не было смысла отключать себе интернет на момент списания
		public override void ForceDeactivate(ClientService assignedService)
		{
			var client = assignedService.Client;
			if (!client.Disabled && !assignedService.ActivatedByUser) {
				//если null значит клиент не начал работать и не за что списывать
				if (client.RatedPeriodDate != null) {
					var comment = string.Format("Абонентская плата за {0} из-за отключения услуги {1}", DateTime.Now.ToShortDateString(), HumanName);
					var sum = client.GetPriceForTariff() / client.GetInterval();
					client.UserWriteOffs.Add(new UserWriteOff(client, sum, comment));
				}
			}
			//обновление даты "последней" деактивации 
			assignedService.EndWorkDate = SystemTime.Now();
			base.ForceDeactivate(assignedService);
			assignedService.Client.CreareAppeal(String.Format("Отключена услуга \"{0}\"", HumanName));
		}

		public override void Activate(ClientService assignedService)
		{
			//При активации интернета у клиента не должно быть статуса "Заблокирован - Восстановление работы"
			var statusIsTrue = true;
			var statusIsProxy = assignedService.Client.Status as INHibernateProxy != null;
			var number = statusIsProxy ? ((InternetInterface.Models.Status)(assignedService.Client.Status as INHibernateProxy)).Id : 0;
			if (statusIsProxy) statusIsTrue = statusIsProxy? (StatusType)number != StatusType.BlockedForRepair:
				assignedService.Client.Status.Type != StatusType.BlockedForRepair;

			if (assignedService.ActivatedByUser && !assignedService.Client.Disabled && statusIsTrue)
			{
				//обновление даты "последней" активации
				// (нет необходимости затирать значение последней деактивации, услуга не деактивирутся по графику)
				assignedService.BeginWorkDate = SystemTime.Now();
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