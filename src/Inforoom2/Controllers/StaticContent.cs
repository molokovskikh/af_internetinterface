using System.Web.Mvc;

namespace Inforoom2.Controllers
{
	public class StaticContentController : Inforoom2Controller
	{
		public ActionResult PageNotFound()
		{
			return View();
		}

		public ActionResult Error()
		{
			return View();
		}
	}
}