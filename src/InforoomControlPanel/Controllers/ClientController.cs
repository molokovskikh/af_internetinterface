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

		public ActionResult ClientList(int page=1)
		{
			var perpage = 100;
			var clients = DbSession.Query<Client>().Where(i=>i.PhysicalClient != null).Skip((page-1)*perpage).Take(perpage).ToList();
			ViewBag.Clients = clients;
			//Пагинация
			ViewBag.Models = clients;
			ViewBag.Page = page;
			ViewBag.ModelsPerPage = perpage;
			ViewBag.ModelsCount = DbSession.QueryOver<Client>().Where(i=>i.PhysicalClient != null).RowCount();
			return View("ClientList");
		}

		public ActionResult ClientInfo(int clientId)
		{
			ViewBag.Client = DbSession.Get<Client>(clientId);
			return View();
		}

		[HttpPost]
		public ActionResult ClientInfo([EntityBinder] Client client)
		{
			DbSession.Update(client);
			return View();
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