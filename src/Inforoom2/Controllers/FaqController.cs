﻿using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Страница вопросов и ответов
	/// </summary>
	public class FaqController : Inforoom2Controller
	{
		/// <summary>
		/// Отображает список вопросов и ответов, а также форму нового вопроса
		/// </summary>
		/// 
		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server, VaryByCustom = "Cookies")]
		public ActionResult Index()
		{
			var questions = DbSession.Query<Question>().Where(k => k.IsPublished).OrderBy(k => k.Priority).ToList();
			var ticket = new Ticket();

			ViewBag.NewTicket = ticket;
			ViewBag.Questions = questions;
			return View();
		}

		/// <summary>
		/// Обрабатывает отправку нового вопроса
		/// </summary>
		[HttpPost]
		public ActionResult Index(Ticket ticket, HttpPostedFileBase[] uploadedFile = null)
		{
			var client = CurrentClient;
			if (client != null) {
				ticket.Client = client;
			}
			Index();
			ViewBag.NewTicket = ticket;
			var errors = ValidationRunner.Validate(ticket);
			if (errors.Length == 0) {
				DbSession.Save(ticket);

				//Дублирование письма на почту
				var builder = new StringBuilder(1000);
				if (ticket.Client != null)
					builder.Append("Клиент: " + ticket.Client.Id);
				builder.AppendLine("<br />");
				builder.Append("IP: " + Request.UserHostAddress);
				builder.AppendLine("<br />");
				builder.Append(ticket.Text);

#if DEBUG
				var email = ConfigurationManager.AppSettings["DebugMailAddress"];
#else
				var email = ConfigurationManager.AppSettings["MailSenderAddress"];
#endif
				try {
					EmailSender.SendEmail(email, "Запрос в техподдержку с ivrn.net от " + ticket.Email, builder.ToString(), uploadedFile);
				}
				catch (Exception) {
					ErrorMessage("Извините, произошла ошибка");
					return RedirectToAction("TechSupport");
				}

				if (ticket.Client != null) {
					var appeal = new Appeal("Клиент написал тикет в техподдержку #" + ticket.Id, ticket.Client, AppealType.Statistic) {
						Employee = GetCurrentEmployee()
					};
					DbSession.Save(appeal);
				}

				SuccessMessage("Вопрос успешно отправлен. Ответ придет вам на почту.");
				ViewBag.NewTicket = new Ticket();
			}
			else
				ViewBag.ShowQuestionForm = true;
			if (uploadedFile != null && uploadedFile.Length > 0) {
				return RedirectToAction("TechSupport");
			}
			return View();
		}

		/// <summary>
		/// Страница техподдержки
		/// </summary>
		/// 
		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server, VaryByCustom = "Cookies")]
		public ActionResult TechSupport()
		{
			Index();
			return View();
		}

		/// <summary>
		/// Обрабатывает отправку нового вопроса
		/// </summary>
		[HttpPost]
		public ActionResult TechSupport(Ticket ticket, HttpPostedFileBase[] uploadedFile = null)
		{
			if (uploadedFile != null) {
				uploadedFile = uploadedFile.Where(s => s != null).ToArray();
				string[] extensionsAllowed = { ".jpeg", ".jpg", ".png", ".bmp", ".doc", ".docx" };
				if (!uploadedFile.All(d => extensionsAllowed.Any(s => {
					var tempInfo = new FileInfo(d.FileName);
					return tempInfo.Extension.ToLower() == s && d.ContentLength <= 2400000;
				})) || uploadedFile.Length > 5) {
					ErrorMessage(uploadedFile.Length > 5 ? "Количество файлов превышает допустимое: макс. 5"
						: "Добавленные файлы не соответствуют установленному формату.");
					return RedirectToAction("TechSupport");
				}
			}
			Index(ticket, uploadedFile);
			return View();
		}
	}
}