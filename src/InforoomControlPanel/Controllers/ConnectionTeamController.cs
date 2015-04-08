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
			var regions = DbSession.Query<Region>().ToList();
			ViewBag.Regions = regions;
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
		public ActionResult ConnectionTable(DateTime date, int regionId = 0)
		{
			var team = DbSession.Query<ServiceMan>().ToList();
			if (regionId != 0) {
				ViewBag.Region = DbSession.Get<Region>(regionId);
				team = team.Where(i => i.Region.Id == regionId).ToList();
			}

			var regions = DbSession.Query<Region>().ToList();
			ViewBag.Regions = regions;
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
			var serviceRequests = DbSession.Query<ServiceRequest>().OrderByDescending(i => i.Id).ToList();
			ViewBag.ServiceRequests = serviceRequests;
			return View();
		}

		public ActionResult UnpluggedClientList()
		{
			var status = DbSession.Query<Status>().First(i => i.ShortName == "BlockedAndNoConnected");
			var clients = DbSession.Query<Client>().Where(i => i.Status == status).OrderByDescending(i => i.Id).ToList();
			ViewBag.clients = clients;
			return View();
		}

		/// <summary>
		/// Прикрепление сервисной заявки в график
		/// </summary>
		/// <param name="ClientRequest"></param>
		/// <returns></returns>
		public ActionResult AttachClient(int id)
		{
			var client = DbSession.Get<Client>(id);
			var request = DbSession.Query<ConnectionRequest>().FirstOrDefault(i => i.Client == client);
			if (request == null) {
				request = new ConnectionRequest();
				request.Client = client;
				DbSession.Save(request);
			}
			return RedirectToAction("AttachConnectionRequest", new { requestId = request.Id });
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
			var regions = DbSession.Query<Region>().ToList();
			ViewBag.Regions = regions;
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
			var errors = ValidationRunner.Validate(ServiceRequest);
			if (errors.Length == 0) {
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
		/// Прикрепление заявки на подключение в график
		/// </summary>
		/// <param name="ClientRequest"></param>
		/// <returns></returns>
		public ActionResult AttachConnectionRequest(int requestId)
		{
			var ConnectionRequest = DbSession.Get<ConnectionRequest>(requestId);
			var servicemen = DbSession.Query<ServiceMan>().ToList();
			var regions = DbSession.Query<Region>().ToList();
			ViewBag.Regions = regions;
			ViewBag.ConnectionRequest = ConnectionRequest;
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
		public ActionResult AttachConnectionRequest([EntityBinder] ConnectionRequest ConnectionRequest)
		{
			var duration = int.Parse(Request.Form["duration"]);
			int hours = duration / 60;
			int minutes = duration - hours * 60;
			ViewBag.ServicemenDate = ConnectionRequest.BeginTime.Value.Date;
			ConnectionRequest.EndTime = ConnectionRequest.BeginTime.Value.AddHours(hours).AddMinutes(minutes);
			var errors = ValidationRunner.Validate(ConnectionRequest);
			if (errors.Length == 0) {
				DbSession.Save(ConnectionRequest);
				SuccessMessage("Сервисная заявка успешно добавлена в график");
				DbSession.Flush();
				DbSession.Clear();
				return AttachConnectionRequest(ConnectionRequest.Id);
			}
			AttachServiceRequest(ConnectionRequest.Id);
			ViewBag.ClientRequest = ConnectionRequest;
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
			var employees = DbSession.Query<Employee>().ToList().Where(j => team.All(i => i.Employee != j)).OrderBy(i => i.Name).ToList();
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