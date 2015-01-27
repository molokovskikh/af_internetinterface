using System.Linq;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class IpPoolsController : InternetInterfaceController
	{
		// Метод-контроллер отображения списка регионов (страница $siteroot/IpPools/ShowRegionsList)
		public void ShowRegionsList()
		{
			PropertyBag["RegionsList"] = DbSession.Query<RegionHouse>().ToList();
		}

		// Метод-контроллер отображения IP-пулов для данного региона (страница $siteroot/IpPools/ShowIpPoolsOfRegion)
		public void ShowIpPoolsOfRegion(uint regionId)
		{
			var region = DbSession.Get<RegionHouse>(regionId);
			PropertyBag["RegionName"] = region.Name;
			var poolRegsList = DbSession.Query<IpPoolRegion>()
				.ToList().FindAll(rp => (rp.Region == regionId));
			PropertyBag["RegIpPoolsList"] = poolRegsList;
			var poolsList = DbSession.Query<IpPool>().Where(p => !(p.IsGray))
				.ToList().FindAll(p => !(poolRegsList.Exists(rp => rp.IpPool.Id == p.Id)));
			PropertyBag["IpPoolsList"] = poolsList;
		}

		// Метод-контроллер присоединения выбранного IP-пула к данному региону (страница $siteroot/IpPools/ShowIpPoolsOfRegion)
		public void ChainIpPoolWithRegion(uint regionId,
			[ARDataBind("addRegPool", AutoLoad = AutoLoadBehavior.NewInstanceIfInvalidKey)] IpPoolRegion addRegPool)
		{
			if (addRegPool == null || addRegPool.IpPool.Id == 0)
				return;

			addRegPool.Region = regionId;
			var errors = ValidateDeep(addRegPool);
			if (errors.ErrorsCount == 0) {
				DbSession.Save(addRegPool);
				RedirectToUrl("ShowIpPoolsOfRegion?regionId=" + regionId);
			}
		}

		// Метод-контроллер открепления выбранного IP-пула от данного региона (страница $siteroot/IpPools/ShowIpPoolsOfRegion)
		public void UnchainIpPoolWithRegion(uint regionId,
			[ARDataBind("delRegPool", AutoLoad = AutoLoadBehavior.NewInstanceIfInvalidKey)] IpPoolRegion delRegPool)
		{
			delRegPool.Region = regionId;
			var errors = ValidateDeep(delRegPool);
			if (errors.ErrorsCount == 0) {
				DbSession.Delete(delRegPool);
				RedirectToUrl("ShowIpPoolsOfRegion?regionId=" + regionId);
			}
		}
	}
}