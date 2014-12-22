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
		public ActionResult ServiceRequest(int clientId)
		{
			var client = DbSession.Get<Client>(clientId);
			var ServiceRequest = new ServiceRequest(client);
			var servicemen = DbSession.Query<ServiceMan>().ToList();
			ViewBag.Client = client;
			ViewBag.ServiceRequest = ServiceRequest;
			ViewBag.Servicemen = servicemen;
			return View();
		}

		[HttpPost]
		public ActionResult ServiceRequest(ServiceRequest ServiceRequest)
		{
			var forms = Request.Form;
			var client = ServiceRequest.Client;
			this.ServiceRequest(client.Id);

			return View();
		}
	}
}