using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления новостями
	/// </summary>
	public class NewsController : AdminController
	{
		public NewsController()
		{
			ViewBag.BreadCrumb = "Новости";
		}

		public ActionResult Index()
		{
			return NewsIndex();
		}

		/// <summary>
		/// Страница списка новостей
		/// </summary>
		public ActionResult NewsIndex()
		{
			var newsBlocks = DbSession.Query<NewsBlock>().OrderBy(k => k.Priority).ToList();
			if (newsBlocks.Count == 0) {
				newsBlocks = new List<NewsBlock>();
			}

			ViewBag.NewsBlocks = newsBlocks;
			return View("NewsIndex");
		}

		/// <summary>
		/// Изменение новостей
		/// </summary>
		public ActionResult EditNewsBlock(int? newsBlockId)
		{
			NewsBlock newsBlock;
			if (newsBlockId != null) {
				newsBlock = DbSession.Get<NewsBlock>(newsBlockId);
			}
			else {
				var newsBlocks = DbSession.Query<NewsBlock>().OrderBy(k => k.Priority).ToList();
				int maxPriority = newsBlocks.Count != 0 ? newsBlocks.Max(k => k.Priority) : 0;
				newsBlock = new NewsBlock(maxPriority + 1) {
					Employee = GetCurrentEmployee(),
					PublishedDate = DateTime.Now
				};
			}
			newsBlock.Url = Request.Url.GetLeftPart(UriPartial.Authority);
			ViewBag.NewsBlock = newsBlock;
			return View();
		}

		/// <summary>
		/// Удаление новостей
		/// </summary>
		public ActionResult DeleteNewsBlock(int? newsBlockId)
		{
			var newsBlock = DbSession.Get<NewsBlock>(newsBlockId);
			DbSession.Delete(newsBlock);
			return RedirectToAction("NewsIndex");
		}

		/// <summary>
		/// Создание новостей
		/// </summary>
		public ActionResult CreateNewsBlock()
		{
			NewsBlock newsBlock;

			var newsBlocks = DbSession.Query<NewsBlock>().OrderBy(k => k.Priority).ToList();
			int maxPriority = newsBlocks.Count != 0 ? newsBlocks.Max(k => k.Priority) : 0;
			newsBlock = new NewsBlock(maxPriority + 1) {
				Employee = GetCurrentEmployee(),
				PublishedDate = DateTime.Now
			};
			newsBlock.Url = Request.Url.GetLeftPart(UriPartial.Authority);
			ViewBag.NewsBlock = newsBlock;
			return View();
		}

		[HttpPost]
		public ActionResult CreateNewsBlock([EntityBinder] NewsBlock newsBlock)
		{
			newsBlock.Employee = GetCurrentEmployee();
			ViewBag.NewsBlock = newsBlock;
			var errors = ValidationRunner.ValidateDeep(newsBlock);
			if (errors.Length == 0) {
				DbSession.Save(newsBlock);
				SuccessMessage("Новость успешно сохранена");
			}
			else {
				ErrorMessage("Что-то пошло не так");
				return View("EditNewsBlock");
			}

			return RedirectToAction("NewsIndex");
		}

		/// <summary>
		/// Редактирование новостей
		/// </summary>
		[HttpPost]
		public ActionResult UpdateNewsBlock([EntityBinder] NewsBlock newsBlock)
		{
			newsBlock.Employee = GetCurrentEmployee();
			ViewBag.NewsBlock = newsBlock;
			var errors = ValidationRunner.ValidateDeep(newsBlock);
			if (errors.Length == 0) {
				DbSession.Update(newsBlock);
				SuccessMessage("Новость успешно отредактирована");
			}
			else {
				ErrorMessage("Что-то пошло не так");
				return View("EditNewsBlock");
			}

			return RedirectToAction("NewsIndex");
		}

		/// <summary>
		/// Приоритет отображения новостей
		/// </summary>
		public ActionResult Move(int newsblockid, string direction)
		{
			return ChangeModelPriority<NewsBlock>(newsblockid, direction, "NewsIndex", "News");
		}
	}
}