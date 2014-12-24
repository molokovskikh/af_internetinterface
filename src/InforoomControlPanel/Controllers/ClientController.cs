using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	public class ClientController : AdminController
	{
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.BreadCrumb = "Клиенты";
		}

		public ActionResult Index()
		{
			var clients = DbSession.Query<Client>().Take(100).ToList();
			ViewBag.Clients = clients;
			return View("ClientList");
		}

		/// <summary>
		/// Создание заявки на подключение
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		public ActionResult ServiceRequest(int clientId)
		{
			var client = DbSession.Get<Client>(clientId);
			var ServiceRequest = new ServiceRequest(client);
			var servicemen = DbSession.Query<ServiceMan>().ToList();
			ViewBag.Client = client;
			ViewBag.ServiceRequest = ServiceRequest;
			ViewBag.Servicemen = servicemen;
			ViewBag.ServicemenDate = DateTime.Today;
			return View();
		}

		/// <summary>
		/// Создание заявки на подключение
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult ServiceRequest([EntityBinder] ServiceRequest ServiceRequest)
		{
			var client = ServiceRequest.Client;
			this.ServiceRequest(client.Id);
			ViewBag.ServicemenDate = ServiceRequest.BeginTime.Date;
			var errors = ValidationRunner.ValidateDeep(ServiceRequest);
			if (errors.Length == 0) {
				DbSession.Save(ServiceRequest);
				SuccessMessage("Сервисная заявка успешно добавлена");
				return this.ServiceRequest(client.Id);
			}
			ViewBag.ServiceRequest = ServiceRequest;
			return View();
		}
	}
}