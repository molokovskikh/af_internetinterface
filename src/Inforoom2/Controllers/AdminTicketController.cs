using System;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Страница управления вопросами от пользователя
	/// </summary>
	public class AdminTicketController : AdminController
	{
		public ActionResult AdminTicketIndex()
		{
			var tickets = DbSession.Query<Ticket>().Where(k => !k.IsNotified).ToList();
			ViewBag.Tickets = tickets;
			return View();
		}

		public ActionResult EditTicket(int? ticketid)
		{
			Ticket ticket;
			ticket = DbSession.Get<Ticket>(ticketid);
			ViewBag.Ticket = ticket;
			return View();
		}

		public ActionResult UpdateTicket(Ticket ticket)
		{
			ViewBag.Ticket = ticket;
			var errors = ValidationRunner.ValidateDeep(ticket);
			if (errors.Length == 0) {
				var employee = DbSession.Query<Employee>().FirstOrDefault(k => k.Username == User.Identity.Name);
				ticket.AnswerDate = DateTime.Now;
				ticket.Employee = employee;
				ticket.IsNotified = true;
				EmailSender.SendEmail(new Client { Email = ticket.Email }, "Ответ на вопрос", ticket.Answer);
				DbSession.SaveOrUpdate(ticket);
				SuccessMessage("Ответ отправлен пользователю.");
			}
			else {
				ErrorMessage("Что-то пошло не так");
				return View("EditTicket");
			}

			return RedirectToAction("AdminTicketIndex");
		}
	}
}