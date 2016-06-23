using System.Web.Mvc;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Отображает страницы оферты
	/// </summary>
	public class OfferContractController : Inforoom2Controller
	{
		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server)]
		public ActionResult Index()
		{
			return View();
		}
	}
}