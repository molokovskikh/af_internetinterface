using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class HomeController : Inforoom2Controller
	{
		public ActionResult Index()
		{
			ViewBag.Message = "HomeController";
			var news = DbSession.Query<NewsBlock>().Where(k => k.IsPublished && (k.Region == CurrentRegion || k.Region == null)).OrderByDescending(n => n.Priority).ToList();
			var newsList = new List<NewsBlock>();
			int i = 0;
			foreach (var newsBlock in news) {
				i++;
				if (i < 4 && newsBlock != null) {
					newsList.Add(newsBlock);
				}
			}
			ViewBag.News = newsList;
			return View();
		}

		public ActionResult Plans(int? id)
		{
			if (id != null) {
				var plan = DbSession.Get<Plan>(id);
				ViewBag.Plan = plan;
			}
			return View();
		}

		public ActionResult ViewNewsBlock(int id)
		{
			var newsBlock = DbSession.Get<NewsBlock>(id);
			ViewBag.NewsBlock = newsBlock;
			return View();
		}

		public ActionResult PermanentHomeRedirect()
		{
			return RedirectToActionPermanent("Index");
		}


}
}