using System;
using System.Linq;
using System.Web.Mvc;
using Common.MySql;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Базовый контролер администратора
	/// </summary>
	[AuthorizeUser(Roles = "Admin")]
	public class AdminController : BaseController
	{
		
		
		public ActionResult Index()
		{
			//проверяем наличие неотвеченных тикетов
			ViewBag.TicketsAmount = DbSession.Query<Ticket>().Count(k => !k.IsNotified);

			var users = DbSession.Query<Employee>().ToList();
			ViewBag.Users = users;
			return View();
		}
		
		public ActionResult CreateUser(Employee client)
		{
			var passprhase = PasswordHasher.Hash(client.Password);
			client.Password = passprhase.Hash;
			client.Salt = passprhase.Salt;
			DbSession.Save(client);
			DbSession.Flush();
			SuccessMessage("Пользователь создан");
			return RedirectToAction("Index");
		}
	}
}