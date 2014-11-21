﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Common.MySql;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class AdminNewsController : AdminController
	{
		public ActionResult AdminNewsIndex()
		{
			var newsBlocks = DbSession.Query<NewsBlock>().OrderBy(k => k.Priority).ToList();
			if (newsBlocks.Count == 0) {
				newsBlocks = new List<NewsBlock>();
			}
			
			ViewBag.NewsBlocks = newsBlocks;
			return View();
		}

		public ActionResult EditNewsBlock(int? newsBlockId)
		{
			NewsBlock newsBlock;
			if (newsBlockId != null) {
				newsBlock = DbSession.Get<NewsBlock>(newsBlockId);
			}
			else {
				var newsBlocks = DbSession.Query<NewsBlock>().OrderBy(k => k.Priority).ToList();
				int maxPriority = newsBlocks.Count != 0 ? newsBlocks.Max(k => k.Priority) : 0;
				newsBlock = new NewsBlock(maxPriority + 1);
				newsBlock.Employee = CurrentEmployee;
				newsBlock.PublishedDate = DateTime.Now;
			}
			newsBlock.Url = Request.Url.GetLeftPart(UriPartial.Authority);
			ViewBag.NewsBlock = newsBlock;
			return View();
		}

		public ActionResult DeleteNewsBlock(int? newsBlockId)
		{
			var newsBlock = DbSession.Get<NewsBlock>(newsBlockId);
			DbSession.Delete(newsBlock);
			return RedirectToAction("AdminNewsIndex");
		}

		[HttpPost]
		public ActionResult UpdateNewsBlock(NewsBlock newsBlock)

		{
			ViewBag.NewsBlock = newsBlock;
			var errors = ValidationRunner.ValidateDeep(newsBlock);
			if (errors.Length == 0) {
				
				DbSession.SaveOrUpdate(newsBlock);
				SuccessMessage("Новость успешно отредактирована");
			}
			else {
				ErrorMessage("Что-то пошло не так");
				return View("EditNewsBlock");
			}

			return RedirectToAction("AdminNewsIndex");
		}

		public ActionResult Move(int newsblockid, string direction)
		{
			return ChangeModelPriority<NewsBlock>(newsblockid, direction, "AdminNewsIndex", "AdminNews");
		}
	}
}