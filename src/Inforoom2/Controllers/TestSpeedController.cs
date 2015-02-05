using System.Web.Mvc;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Отображает страницы оферты
	/// </summary>
	public class TestSpeedController : Inforoom2Controller
	{
		public ActionResult Index()
		{
			return View();
		}
	}
}