using Inforoom2.Models.Services;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Subclass(0,ExtendsType = typeof(Service) ,DiscriminatorValue= "IpTvBoxRent")]
	public class IpTvBoxRent : Service
	{
		 
	}
}