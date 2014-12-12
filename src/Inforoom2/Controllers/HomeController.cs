using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class HomeController : BaseController
	{
		public ActionResult Index()
		{
			ViewBag.Message = "HomeController";
			var news = DbSession.Query<NewsBlock>().Where(k => k.IsPublished).OrderByDescending(n=>n.PublishedDate).ToList();
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

		public ActionResult TariffPlans()
		{
			return View();
		}

		public ActionResult ViewNewsBlock(int? newsid)
		{
			
			var newsBlock = DbSession.Get<NewsBlock>(newsid);
			ViewBag.NewsBlock = newsBlock;
			return View();
		}

		
	}
}