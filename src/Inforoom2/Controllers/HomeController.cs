using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class HomeController : Inforoom2Controller
	{

		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server, VaryByCustom = "User,Cookies")]
		public ActionResult Index()
		{
		    var newIp = IPAddress.Parse("91.235.89.149");
		    var endp = DbSession.Query<ClientEndpoint>().FirstOrDefault();
		    endp.Ip = newIp;
		    DbSession.Save(endp);
			var pathFromConfigUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadUrl"];
			if (pathFromConfigUrl == null)
			{
				throw new Exception("Значение 'inforoom2UploadUrl' отсуствует в Global.config!");
			}

			ViewBag.pathFromConfigURL = pathFromConfigUrl;
			ViewBag.Message = "HomeController";
			var news = DbSession.Query<NewsBlock>().Where(k => k.IsPublished && (k.Region == CurrentRegion 
				|| k.Region == null)).OrderByDescending(n => n.Priority).ToList();
			var newsList = new List<NewsBlock>();
			int i = 0;
			foreach (var newsBlock in news)
			{
				i++;
				if (i < 4 && newsBlock != null)
				{
					newsList.Add(newsBlock);
				}
			}
			ViewBag.News = newsList; 

			ViewBag.SlideList = DbSession.Query<Slide>().Where(k => k.Enabled && (k.Region == CurrentRegion
				|| k.Region == null)).OrderByDescending(n => n.Priority).ToList();
			ViewBag.Banner = DbSession.Query<Banner>().FirstOrDefault(k => k.Enabled 
				&& (k.Region == CurrentRegion || k.Region == null))??new Banner();  
			 
			return View();
		}

		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server, VaryByParam = "*", VaryByCustom = "User,Cookies")]
		public ActionResult Plans(int? id)
		{
			ViewBag.Client = CurrentClient;
			ViewBag.ContentHtml = DbSession.Query<PlanHtmlContent>().FirstOrDefault(s => s.Region == CurrentRegion);
			if (id != null) {
				var plan = DbSession.Get<Plan>(id);
				ViewBag.Plan = plan;
			}
			ViewBag.Plans = DbSession.Query<RegionPlan>().Where(s => s.Region == CurrentRegion && s.Plan.AvailableForNewClients && s.Plan.Disabled==false)
				.OrderBy(s=>s.Region).ThenByDescending(s=>s.Plan.Priority).Select(s=>s.Plan)
				.Take(3).ToList();
			return View();
		}

		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server, VaryByParam = "*",
			VaryByCustom = "User,Cookies")]
		public ActionResult PricesList()
		{
			var model =
				DbSession.Query<PublicData>()
					.Where(
						s => s.ItemType == PublicDataType.PriceList && s.Display && (s.Region == null || s.Region.Id == CurrentRegion.Id))
					.OrderBy(s => s.PositionIndex)
					.ToList();
			return View("ExtraServicesPriceList", model);
		}

		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server, VaryByParam = "*")]
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