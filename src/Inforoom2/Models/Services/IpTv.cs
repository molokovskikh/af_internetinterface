using Inforoom2.Models.Services;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Subclass(0, ExtendsType = typeof(Service), DiscriminatorValue = "IpTv")]
	public class IpTv : Service
	{

		public override bool CanDeactivate(ClientService assignedService)
		{
			return assignedService.Client.Disabled && assignedService.IsActivated;
		}
	}
}