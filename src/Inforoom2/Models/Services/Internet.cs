using System;
using System.Linq;
using Common.Tools;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models.Services
{
	[Subclass(0, ExtendsType = typeof(Service), DiscriminatorValue = "Internet")]
	public class Internet : Service
	{
		public override decimal GetPrice(ClientService assignedService)
		{
			if (assignedService.Client.Disabled || !assignedService.ActivatedByUser)
				return 0;
			return assignedService.Client.GetTariffPrice();
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			return assignedService.IsActivated &&
				   (!assignedService.ActivatedByUser
					|| assignedService.Client.Disabled
					//Статус "Заблокирован - Восстановление работы" является причиной отключения интернета, при его наличии услугу нужно деактивировать
					|| assignedService.Client.Status.Type == StatusType.BlockedForRepair);
		}
	}
}