using System.Web.Mvc;

namespace Inforoom2.Controllers
{
	public class HomeController : BaseController
	{
		public ActionResult Index()
		{
			
			ViewBag.Message = "HomeController";
			if (User != null && User.HasPermissions("CanEverything")) {
				ViewBag.Message = "Я одмин";
			}
			
			return View();
		}
	
		public ActionResult AdminIndex()
		{
			return RedirectToAction("Index", "Admin");
		}
	}
}