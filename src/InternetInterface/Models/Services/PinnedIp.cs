using Castle.ActiveRecord;
using InternetInterface.Services;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(DiscriminatorValue = "PinnedIp")]
	public class PinnedIp : Service
	{
	}
}