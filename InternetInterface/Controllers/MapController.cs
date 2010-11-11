using System.Collections.Generic;
using Castle.MonoRail.Framework;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	public class MapController : SmartDispatcherController
	{
		public void SiteMap()
		{
			var MapPartner = Partner.FindAllByProperty("Pass", Session["HashPass"]);
			if (MapPartner.Length != 0)
			{
				PropertyBag["PARTNERNAME"] = MapPartner[0].Name;
				var CatList = new List<string>();
				var access = AccessCategories.FindAll();
				foreach (var accessCategoriese in access)
				{
					if ((MapPartner[0].AcessSet & accessCategoriese.Code) == accessCategoriese.Code)
					{
						CatList.Add(accessCategoriese.Name);
					}
				}
				PropertyBag["GetInfo"] = ((MapPartner[0].AcessSet & 1) == 1) ? true : false;
				PropertyBag["RegClient"] = ((MapPartner[0].AcessSet & 2) == 2) ? true : false;
				PropertyBag["CloseDem"] = ((MapPartner[0].AcessSet & 8) == 8) ? true : false;
				PropertyBag["AccessList"] = CatList;
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}
	}
}