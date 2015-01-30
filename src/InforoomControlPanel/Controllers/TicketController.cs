using System;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления вопросами от пользователя
	/// </summary>
	public class TicketController : AdminController
	{
		public ActionResult TicketIndex()
		{
			var tickets = DbSession.Query<Ticket>().OrderByDescending(i => i.CreationDate).ToList();
			ViewBag.Tickets = tickets;
			return View();
		}

		public ActionResult CallMeBackTicketIndex()
		{
			var tickets = DbSession.Query<CallMeBackTicket>().OrderByDescending(i => i.CreationDate).ToList();
			ViewBag.Tickets = tickets;
			return View();
		}
		public ActionResult EditCallMeBackTicket(int? ticketid)
		{
			var ticket = DbSession.Get<CallMeBackTicket>(ticketid);
			ViewBag.Ticket = ticket;
			return View();
		}

		public ActionResult UpdateCallMeBackTicket([EntityBinder]CallMeBackTicket ticket)
		{
			ViewBag.Ticket = ticket;
			var errors = ValidationRunner.ValidateDeep(ticket);
			if (errors.Length == 0) {
				ticket.AnswerDate = DateTime.Now;
				ticket.Employee = CurrentEmployee;
				DbSession.SaveOrUpdate(ticket);
				SuccessMessage("Запрос на обратный звонок успешно изменен");
			}
			else {
				ErrorMessage("Что-то пошло не так");
				return View("EditCallMeBackTicket");
			}

			return RedirectToAction("CallMeBackTicketIndex");
		}
		public ActionResult EditTicket(int? ticketid)
		{
			Ticket ticket;
			ticket = DbSession.Get<Ticket>(ticketid);
			ViewBag.Ticket = ticket;
			return View();
		}

		public ActionResult UpdateTicket([EntityBinder]Ticket ticket)
		{
			ViewBag.Ticket = ticket;
			var errors = ValidationRunner.ValidateDeep(ticket);
			if (errors.Length == 0) {
				ticket.AnswerDate = DateTime.Now;
				ticket.Employee = CurrentEmployee;
				ticket.IsNotified = true;
				try {
					EmailSender.SendEmail(new PhysicalClient {Email = ticket.Email},
						"Ответ на ваш запрос в техподдержку компании Инфорум", ticket.Answer);
				}
				catch (System.Net.Mail.SmtpException) {
					ErrorMessage("Указанный e-mail клиента не может быть обработан!");
					return View("EditTicket");
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