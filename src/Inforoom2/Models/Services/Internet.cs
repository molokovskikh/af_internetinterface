using System;
using System.Linq;
using Common.Tools;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Proxy;

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

        /// <summary>
        /// Деактивирует клиентскую услугу assignedService
        /// </summary>
        public override void Deactivate(ClientService assignedService, ISession session)
        {
            var client = assignedService.Client;
            if (!client.Disabled && !assignedService.ActivatedByUser) {
                //если null значит клиент не начал работать и не за что списывать
                if (client.RatedPeriodDate != null) {
                    var comment = string.Format("Абонентская плата за {0} из-за отключения услуги {1}",
                        DateTime.Now.ToShortDateString(), Name);
                    var sum = client.GetTariffPrice()/client.GetInterval();
                    client.UserWriteOffs.Add(new UserWriteOff(client, sum, comment));
                }
            }
            base.Deactivate(assignedService, session);
            assignedService.Client.Appeals.Add(
                new Appeal(String.Format("Отключена услуга \"{0}\"", Name),
                    assignedService.Client, AppealType.System));
        }

        public override void Activate(ClientService assignedService, ISession session)
	    {
	        //При активации интернета у клиента не должно быть статуса "Заблокирован - Восстановление работы"
	        var statusIsTrue = true;
	        var statusIsProxy = assignedService.Client.Status as INHibernateProxy != null;
	        var number = statusIsProxy ? ((Status) (assignedService.Client.Status as INHibernateProxy)).Id : 0;
	        if (statusIsProxy)
	            statusIsTrue = statusIsProxy
	                ? (StatusType) number != StatusType.BlockedForRepair
	                : assignedService.Client.Status.Type != StatusType.BlockedForRepair;

	        if (assignedService.ActivatedByUser && !assignedService.Client.Disabled && statusIsTrue) {
	            base.Activate(assignedService, session);
	            assignedService.Client.Appeals.Add(
	                new Appeal(String.Format("Включена услуга \"{0}\"", assignedService.Service.Name),
	                    assignedService.Client, AppealType.System));
	        }
	    }
    }
}