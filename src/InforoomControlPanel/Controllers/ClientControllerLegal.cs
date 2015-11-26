using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.UI;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InforoomControlPanel.ReportTemplates;
using NHibernate.Linq;
using NHibernate.Util;
using Remotion.Linq.Clauses;
using Agent = Inforoom2.Models.Agent;
using Client = Inforoom2.Models.Client;
using ClientService = Inforoom2.Models.ClientService;
using Contact = Inforoom2.Models.Contact;
using House = Inforoom2.Models.House;
using PhysicalClient = Inforoom2.Models.PhysicalClient;
using RequestType = Inforoom2.Models.RequestType;
using ServiceRequest = Inforoom2.Models.ServiceRequest;
using Status = Inforoom2.Models.Status;
using StatusType = Inforoom2.Models.StatusType;
using Street = Inforoom2.Models.Street;

namespace InforoomControlPanel.Controllers
{
	public partial class ClientController
	{
		//информация о юр лице
		public ActionResult InfoLegal(int id)
		{
			// Find Client
			var client = DbSession.Query<Client>().FirstOrDefault(i => i.LegalClient != null && i.Id == id);
			ViewBag.Client = client;
			// Find active RentalHardware
			var activeServices = client.RentalHardwareList.Where(rh => rh.IsActive).ToList();
			ViewBag.RentIsActive = activeServices.Count > 0;
			return View();
		}

		/// <summary>
		/// Регистрация юр. лица
		/// </summary>
		/// <returns></returns>
		public ActionResult RegistrationLegal()
		{
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.RegionList = regionList;
			return View();
		}

		/// <summary>
		/// Регистрация юр. лица
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public ActionResult RegistrationLegal([EntityBinder] Client client)
		{
			var errors = ValidationRunner.ValidateDeep(client);
			// если ошибок нет
			if (errors.Length == 0) {
				//выставляем базовые значаения созданному клиенту
				LegalClient.GetBaseDataForRegistration(DbSession, client, GetCurrentEmployee());
				// сохраняем модель
				DbSession.Save(client);
				//переадресовываем в старую админку 
				// TODO: убрать после переноса старой админки
				return Redirect(System.Web.Configuration.WebConfigurationManager.AppSettings["adminPanelOld"] +
				                "UserInfo/ShowLawyerPerson?filter.ClientCode=" + client.Id);
			}
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.RegionList = regionList;
			ViewBag.Client = client;
			return View();
		}
	}
}