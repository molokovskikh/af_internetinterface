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
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}
	}
}