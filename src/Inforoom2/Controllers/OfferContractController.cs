using System.Web.Mvc;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Отображает страницы оферты
	/// </summary>
	public class OfferContractController : Inforoom2Controller
	{
		public ActionResult Index()
		{
			return View();
		}
	}
}