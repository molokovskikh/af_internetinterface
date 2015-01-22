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
			ViewBag.TableTimeStep = 30;
		}

		public new ActionResult Index()
		{
			return View();
		}

		/// <summary>
		/// График подключений
		/// </summary>
		/// <returns></returns>
		public ActionResult ConnectionTable()
		{
			var team = DbSession.Query<ServiceMan>().ToList();

			//var employees = DbSession.Query<Employee>().Take(6).ToList();
			//foreach (var man in employees)
			//	team.Add(new ServiceMan(man));
			//var client = new Client();
			//client.PhysicalClient = new PhysicalClient(); ;
			//client.PhysicalClient.Name = "Клиент Тестович";
			//client.PhysicalClient.PhoneNumber = "8-967-152-66-06";
			//var request = new ServiceRequest(client);
			//request.Description = "Мышь перегрызла провод от интернета";
			//request.BeginTime = DateTime.Now;
			//request.EndTime = DateTime.Now.AddHours(1).AddMinutes(23);
			//team[0].ServiceRequests.Add(request);

			//var client2 = new Client();
			//client2.PhysicalClient = new PhysicalClient(); ;
			//client2.PhysicalClient.Name = "Клиент Сломаевич";
			//client2.PhysicalClient.PhoneNumber = "8-967-152-66-06";

			//var request2 = new ConnectionRequest(client2);
			//request2.BeginTime = DateTime.Now.AddHours(-5).AddMinutes(1);
			//request2.EndTime = DateTime.Now.AddHours(-3).AddMinutes(49);
			//team[1].ConnectionRequests.Add(request2);
			ViewBag.ServicemenDate = DateTime.Today;
			ViewBag.Servicemen = team;
			return View("BigConnectionTable");
		}

		/// <summary>
		/// Асинхронное получение графика подключений по дате
		/// </summary>
		/// <param name="date">Дата для которой отображается график</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult ConnectionTable(DateTime date)
		{
			var team = DbSession.Query<ServiceMan>().ToList();
			ViewBag.ServicemenDate = date;
			ViewBag.Servicemen = team;
			return View();
		}

		/// <summary>
		/// Список сервисных заявок
		/// </summary>
		/// <param name="ClientRequest"></param>
		/// <returns></returns>
		public ActionResult ServiceRequests()
		{
			var serviceRequests = DbSession.Query<ServiceRequest>().ToList();
			ViewBag.ServiceRequests = serviceRequests;
			return View();
		}

		/// <summary>
		/// Прикрепление сервисной заявки в график
		/// </summary>
		/// <param name="ClientRequest"></param>
		/// <returns></returns>
		public ActionResult AttachServiceRequest(int requestId)
		{
			var serviceRequest = DbSession.Get<ServiceRequest>(requestId);
			var servicemen = DbSession.Query<ServiceMan>().ToList();
			ViewBag.ServiceRequest = serviceRequest;
			ViewBag.Servicemen = servicemen;
			ViewBag.ServicemenDate = DateTime.Today;
			return View();
		}

		/// <summary>
		/// Прикрепление сервисной заявки в график
		/// </summary>
		/// <param name="ClientRequest"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult AttachServiceRequest([EntityBinder] ServiceRequest ServiceRequest)
		{
			var duration = int.Parse(Request.Form["duration"]);
			int hours = duration / 60;
			int minutes = duration - hours * 60;
			ViewBag.ServicemenDate = ServiceRequest.BeginTime.Date;
			ServiceRequest.EndTime = ServiceRequest.BeginTime.AddHours(hours).AddMinutes(minutes);
			var errors = ValidationRunner.ValidateDeep(ServiceRequest);
			if (errors.Length == 0)
			{
				DbSession.Save(ServiceRequest);
				SuccessMessage("Сервисная заявка успешно добавлена в график");
				DbSession.Flush();
				DbSession.Clear();
				return AttachServiceRequest(ServiceRequest.Id);
			}
			AttachServiceRequest(ServiceRequest.Id);
			ViewBag.ServiceRequest = ServiceRequest;
			return View();
		}

		/// <summary>
		/// Список заявок на подключение
		/// </summary>
		/// <param name="ClientRequest"></param>
		/// <returns></returns>
		public ActionResult ClientRequests()
		{
			var clientRequests = DbSession.Query<ClientRequest>().ToList();
			ViewBag.ClientRequests = clientRequests;
			return View();
		}

		/// <summary>
		/// Прикрепление заявки на подключение в график
		/// </summary>
		/// <param name="ClientRequest"></param>
		/// <returns></returns>
		public ActionResult AttachClientRequest(int requestId)
		{
			var clientRequest = DbSession.Get<ClientRequest>(requestId);
			var servicemen = DbSession.Query<ServiceMan>().ToList();
			ViewBag.ClientRequest = clientRequest;
			ViewBag.Servicemen = servicemen;
			ViewBag.ServicemenDate = DateTime.Today;
			return View();
		}

		/// <summary>
		/// Прикрепление заявки на подключение в график
		/// </summary>
		/// <param name="ClientRequest"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult AttachClientRequest([EntityBinder] ClientRequest ClientRequest)
		{
			var duration = int.Parse(Request.Form["duration"]);
			int hours = duration / 60;
			int minutes = duration - hours * 60;
			ViewBag.ServicemenDate = ClientRequest.BeginTime.Date;
			ClientRequest.EndTime = ClientRequest.BeginTime.AddHours(hours).AddMinutes(minutes);
			var errors = ValidationRunner.ValidateDeep(ClientRequest);
			if (errors.Length == 0)
			{
				DbSession.Save(ClientRequest);
				SuccessMessage("Сервисная заявка успешно добавлена в график");
				DbSession.Flush();
				DbSession.Clear();
				return AttachClientRequest(ClientRequest.Id);
			}
			AttachServiceRequest(ClientRequest.Id);
			ViewBag.ClientRequest = ClientRequest;
			return View();
		}

		/// <summary>
		/// График подключения бригад
		/// </summary>
		/// <returns></returns>
		public ActionResult ConnectionTeams()
		{
			var teams = DbSession.Query<ServiceTeam>().ToList();
			var servicemen = DbSession.Query<ServiceMan>().ToList();
			ViewBag.Connectionteams = teams;
			ViewBag.Servicemen = servicemen;
			return View();
		}

		/// <summary>
		/// Список сервисных инженеров
		/// </summary>
		/// <returns></returns>
		public ActionResult Servicemen()
		{
			var team = DbSession.Query<ServiceMan>().ToList();
			var employees = DbSession.Query<Employee>().ToList().Where( j=>team.All(i => i.Employee != j)).OrderBy(i=>i.Name).ToList();
			var regions = DbSession.Query<Region>().ToList();
			ViewBag.Regions = regions;
			ViewBag.ServiceMan = new ServiceMan();
			ViewBag.Team = team;
			ViewBag.Employees = employees;
			return View();
		}

		/// <summary>
		/// Создание нового сервисного инженера
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