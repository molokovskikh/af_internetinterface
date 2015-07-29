using System;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using InternetInterface.Models;
using NHibernate.Linq;
using NHibernate.Transform;
using Remotion.Linq.Clauses;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления запросами от пользователя
	/// </summary>
	public class TicketController : ControlPanelController
	{
		public TicketController()
		{
			ViewBag.BreadCrumb = "Запросы пользователей";
		}

		public  ActionResult Index()
		{
			return TicketIndex();
		}

		/// <summary>
		/// Страница списка запросов в техподдержку
		/// </summary>
		public ActionResult TicketIndex()
		{
			var tickets = DbSession.Query<Ticket>().OrderByDescending(i => i.CreationDate).ToList();
			ViewBag.Tickets = tickets;
			return View("TicketIndex");
		}

		/// <summary>
		/// Страница списка заявок на обратный звонок
		/// </summary>
		public ActionResult CallMeBackTicketIndex()
		{
			var pager = new ModelFilter<CallMeBackTicket>(this);
			pager.SetOrderBy("CreationDate", OrderingDirection.Desc);
			ViewBag.Pager = pager;
			return View();
		}

		/// <summary>
		/// Изменение заявок на обратный звонок
		/// </summary>
		public ActionResult EditCallMeBackTicket(int? ticketid)
		{
			var ticket = DbSession.Get<CallMeBackTicket>(ticketid);
			ViewBag.Ticket = ticket;
			return View();
		}

		/// <summary>
		/// Изменение заявок на обратный звонок
		/// </summary>
		public ActionResult UpdateCallMeBackTicket([EntityBinder] CallMeBackTicket ticket)
		{
			ViewBag.Ticket = ticket;
			var errors = ValidationRunner.ValidateDeep(ticket);
			errors.RemoveErrors("CallMeBackTicket", "Captcha");
			if (errors.Length == 0) {
				ticket.AnswerDate = DateTime.Now;
				ticket.Employee = GetCurrentEmployee();
				DbSession.SaveOrUpdate(ticket);
				SuccessMessage("Запрос на обратный звонок успешно изменен");
			}
			else {
				ErrorMessage("Что-то пошло не так");
				return View("EditCallMeBackTicket");
			}

			return RedirectToAction("CallMeBackTicketIndex");
		}

		/// <summary>
		/// Изменение запроса в техподдержку
		/// </summary>
		public ActionResult EditTicket(int? ticketid)
		{
			Ticket ticket;
			ticket = DbSession.Get<Ticket>(ticketid);
			ViewBag.Ticket = ticket;
			return View();
		}

		/// <summary>
		/// Изменение запроса в техподдержку
		/// </summary>
		public ActionResult UpdateTicket([EntityBinder] Ticket ticket)
		{
			ViewBag.Ticket = ticket;
			var errors = ValidationRunner.ValidateDeep(ticket);
			if (errors.Length == 0) {
				ticket.AnswerDate = DateTime.Now;
				ticket.Employee = GetCurrentEmployee();
				ticket.IsNotified = true;
				try {
					EmailSender.SendEmail(ticket.Email,"Ответ на ваш запрос в техподдержку компании Инфорум", ticket.Answer);
				}
				catch (System.Net.Mail.SmtpException) {
					ErrorMessage("Указанный e-mail клиента не может быть обработан!");
					return RedirectToAction("TicketIndex");
				}
				catch (FormatException) {
					ErrorMessage("Некорректный формат e-mail у клиента!");
					return RedirectToAction("TicketIndex");
				}
				catch (ArgumentException) {
					ErrorMessage("e-mail клиента не может быть пустым!");
					return RedirectToAction("TicketIndex");
				}
				catch (Exception) {
					ErrorMessage("Произошла ошибка при отправке ответа клиенту!");
					return RedirectToAction("TicketIndex");
				}
				DbSession.SaveOrUpdate(ticket);
				SuccessMessage("Ответ отправлен пользователю.");
			}
			else {
				ErrorMessage("Что-то пошло не так");
				return View("EditTicket");
			}

			return RedirectToAction("TicketIndex");
		}
	}
}