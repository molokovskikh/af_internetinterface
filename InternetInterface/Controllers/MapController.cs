using System.Collections.Generic;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class MapController : SmartDispatcherController
	{
		public void SiteMap()
		{
			PropertyBag["PARTNERNAME"] = InithializeContent.partner.Name;
			var CatList = new List<string>();
			var accessList = AccessCategories.FindAllSort();
			var partnerAccessList = PartnerAccessSet.GetAccessPartner();
			foreach (var accessCategorie in accessList)
			{
				foreach (var partnerAccessSet in partnerAccessList)
				{
					if (accessCategorie.Id == partnerAccessSet.AccessCat.Id)
						CatList.Add(accessCategorie.Name);
				}
			}
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["AccessList"] = CatList;
		}
	}
}