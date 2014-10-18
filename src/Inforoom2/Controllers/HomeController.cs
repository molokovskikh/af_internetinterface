using System.Linq;
using System.Web.Mvc;
using Inforoom2.Models;
using NHibernate.Linq;

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

			var news = DbSession.Query<NewsBlock>().Where(k => k.IsPublished).ToList();
			ViewBag.News = news;
			
			return View();
		}
	
		public ActionResult AdminIndex()
		{
			return RedirectToAction("Index", "Admin");
		}

		

		public ActionResult ViewNewsBlock(int? newsid)
		{
			var newsBlock = DbSession.Get<NewsBlock>(newsid);
			ViewBag.NewsBlock = newsBlock;
			return View();
		}
	}
}