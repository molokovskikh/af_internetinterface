using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	public class ConnectionTeamController : AdminController
	{
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.BreadCrumb = "Сервисные бригады";
		}

		public new ActionResult Index()
		{
			return View();
		}
		public ActionResult ConnectionTable()
		{
			var team = DbSession.Query<ServiceMan>().ToList();
			var employees = DbSession.Query<Employee>().Take(6).ToList();
			foreach (var man in employees)
				team.Add(new ServiceMan(man));
			var client = new Client();
			client.PhysicalClient = new PhysicalClient(); ;
			client.PhysicalClient.Name = "Клиент Тестович";
			client.PhysicalClient.PhoneNumber = "8-967-152-66-06";
			var request = new ServiceRequest(client);
			request.Description = "Мышь перегрызла провод от интернета";
			request.BeginTime = DateTime.Now;
			request.EndTime = DateTime.Now.AddHours(1).AddMinutes(23);
			team[0].ServiceRequests.Add(request);

			var client2 = new Client();
			client2.PhysicalClient = new PhysicalClient(); ;
			client2.PhysicalClient.Name = "Клиент Сломаевич";
			client2.PhysicalClient.PhoneNumber = "8-967-152-66-06";

			var request2 = new ConnectionRequest(client2);
			request2.BeginTime = DateTime.Now.AddHours(-5).AddMinutes(1);
			request2.EndTime = DateTime.Now.AddHours(-3).AddMinutes(49);
			team[1].ConnectionRequests.Add(request2);
			ViewBag.team = team;
			return View();
		}

		/// <summary>
		/// График подключения бригад
		/// </summary>
		/// <returns></returns>
		public ActionResult Brigads()
		{
			var team = DbSession.Query<ServiceMan>().ToList();
			return View();
		}

		/// <summary>
		/// Список сервисных инженеров
		/// </summary>
		/// <returns></returns>
		public ActionResult Servicemen()
		{
			var team = DbSession.Query<ServiceMan>().ToList();
			var employees = DbSession.Query<Employee>().ToList().Where( j=>team.All(i => i.Employee != j)).ToList();
			ViewBag.ServiceMan = new ServiceMan();
			ViewBag.Team = team;
			ViewBag.Employees = employees;
			return View();
		}

		/// <summary>
		/// Создание нового сервисного инденера
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Servicemen([EntityBinder] ServiceMan ServiceMan)
		{
			var errors = ValidationRunner.ValidateDeep(ServiceMan);
			if (errors.Length == 0) {
				DbSession.Save(ServiceMan);
				SuccessMessage("Сервисный инженер успешно добавлен");
				return Servicemen();
			}
			Servicemen();
			ViewBag.newServiceman = ServiceMan;
			return View();
		}
	}
}