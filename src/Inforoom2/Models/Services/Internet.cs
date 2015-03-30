using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models.Services
{
	[Subclass(0,ExtendsType = typeof(Service) ,DiscriminatorValue= "Internet")]
	public class Internet : Service
	{
		public override decimal GetPrice(ClientService assignedService)
		{
			if (assignedService.Client.Disabled || !assignedService.ActivatedByUser)
				return 0;
			return assignedService.Client.GetTariffPrice();
		}
	}
}