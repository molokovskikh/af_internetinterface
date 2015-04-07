using Inforoom2.Models.Services;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Subclass(0, ExtendsType = typeof(Service), DiscriminatorValue = "PinnedIp")]
	public class FixedIp : Service
	{
	}
}