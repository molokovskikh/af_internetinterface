using System;
using System.Linq;
using System.Web.Mvc;
using Common.MySql;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class AdminController : BaseController
	{
		
		[AuthorizeUser(Permissions = "CanEverything")]
		public ActionResult Index()
		{
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