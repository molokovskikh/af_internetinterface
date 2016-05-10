using System.Configuration;
using System.Linq;
using Common.Tools;
using NHibernate;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models.Services
{
	[Subclass(0, ExtendsType = typeof(Service), DiscriminatorValue = "SpeedBoost")]
	public class SpeedBoost : Service
	{
		public override void Activate(ClientService assignedService, ISession session)
		{
			base.Activate(assignedService, session);
			//var packageId = NullableConvert.ToInt32(ConfigurationManager.AppSettings["SpeedBoostPackageId"]);
			//if (packageId == null)
			//	return;
			//assignedService.Client.Endpoints.Each(e => e.PackageId = packageId);
			assignedService.Client.IsNeedRecofiguration = true;
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			//в админке это делается руками (в любое время)
			return true;
			// это для биллинга
			//	return (assignedService.EndDate != null && SystemTime.Now().Date >= assignedService.EndDate.Value.Date);
		}

		public override void Deactivate(ClientService assignedService, ISession session)
		{
			base.Deactivate(assignedService, session);
			assignedService.Client.PhysicalClient.UpdatePackageId(assignedService.Client.Endpoints.FirstOrDefault());
			assignedService.Client.IsNeedRecofiguration = true;
		}
		 
	}
}