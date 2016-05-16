using System.Web.Mvc;
using System.Web.UI;

namespace Inforoom2.Controllers
{
	public class StaticContentController : Controller
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