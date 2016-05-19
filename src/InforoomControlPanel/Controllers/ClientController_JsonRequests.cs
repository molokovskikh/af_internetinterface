using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.UI;
using Common.MySql;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InforoomControlPanel.ReportTemplates;
using InternetInterface.Helpers;
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
using NHibernate;
using NHibernate.Proxy.DynamicProxy;
using NHibernate.Transform;
using NHibernate.Validator.Engine;

namespace InforoomControlPanel.Controllers
{
	public partial class ClientController : ControlPanelController
	{
		[HttpPost]
		public JsonResult GetSubnet(int mask)
		{
			return Json(SubnetMask.CreateByNetBitLength(mask).ToString(), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public JsonResult MarkerList()
		{
			var markerList = DbSession.Query<ConnectionRequestMarker>().OrderBy(s => s.Deleted).ThenBy(s => s.Name).ToList();

			return Json(markerList, JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public JsonResult MarkerArchiveOut(int id)
		{
			var marker = DbSession.Query<ClientRequest>().FirstOrDefault(s => s.Id == id);
			if (marker != null) {
				marker.Archived = false;
				DbSession.Save(marker);
				return Json(true, JsonRequestBehavior.AllowGet);
			}
			else {
				return Json(false, JsonRequestBehavior.AllowGet);
			}
		}

		[HttpPost]
		public JsonResult MarkerArchiveIn(int id)
		{
			var marker = DbSession.Query<ClientRequest>().FirstOrDefault(s => s.Id == id);
			if (marker != null) {
				marker.Archived = true;
				DbSession.Save(marker);
				return Json(true, JsonRequestBehavior.AllowGet);
			}
			else {
				return Json(false, JsonRequestBehavior.AllowGet);
			}
		}

		[HttpPost]
		public JsonResult MarkerAdd(string name, string color)
		{
			if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(color)) {
				var marker = new ConnectionRequestMarker();
				marker.Name = name;
				marker.Color = "#" + color.Replace("#", "");
				marker.Deleted = true;
				var errors = ValidationRunner.ValidateDeep(marker);
				if (errors.Length == 0) {
					if (!DbSession.Query<ConnectionRequestMarker>().Any(s => s.Name == marker.Name)) {
						DbSession.Save(marker);
					}
					else {
						return Json("Добавление завершилось ошибкой: запись с подобным именем уже существует",
							JsonRequestBehavior.AllowGet);
					}
				}
			}
			else {
				return Json("Добавление завершилось ошибкой: не все поля заполнены", JsonRequestBehavior.AllowGet);
			}
			return MarkerList();
		}

		[HttpPost]
		public JsonResult MarkerUpdate(int id, string name, string color)
		{
			var marker = DbSession.Query<ConnectionRequestMarker>().FirstOrDefault(s => s.Id == id);
			if (marker != null && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(color)) {
				marker.Name = name;
				marker.Color = "#" + color.Replace("#", "");
				var errors = ValidationRunner.ValidateDeep(marker);
				if (errors.Length == 0) {
					if (DbSession.Query<ConnectionRequestMarker>().Count(s => s.Id != id && s.Name == marker.Name) == 0) {
						DbSession.Update(marker);
					}
					else {
						return Json("Изменение завершилось ошибкой: запись с подобным именем уже существует",
							JsonRequestBehavior.AllowGet);
					}
				}
			}
			else {
				return Json("Изменение завершилось ошибкой: не все поля заполнены", JsonRequestBehavior.AllowGet);
			}
			return MarkerList();
		}

		[HttpPost]
		public JsonResult MarkerRemove(int id)
		{
			var marker = DbSession.Query<ConnectionRequestMarker>().FirstOrDefault(s => s.Id == id && s.Deleted);
			if (marker != null) {
				var markerList = DbSession.Query<ClientRequest>().Where(s => s.Marker == marker).ToList();
				foreach (var item in markerList) {
					item.Marker = null;
					DbSession.Save(item);
				}
				DbSession.Delete(marker);
			}
			else {
				return Json("Удаление данного маркера невозможно", JsonRequestBehavior.AllowGet);
			}
			return MarkerList();
		}


		/// <summary>
		/// получение ФИО по ЛС
		/// </summary>
		[HttpPost]
		public JsonResult getClientName(string id, int clientType = 0)
		{
			int idCurrent = 0;
			int.TryParse(id, out idCurrent);
			var idList =
				DbSession.CreateSQLQuery("SELECT Id, Name, PhysicalClient, Recipient FROM internet.clients WHERE " +
				                         (idCurrent == 0 ? $" Name LIKE '{id}%'" : $"CAST(Id AS CHAR) LIKE '{idCurrent}%'") +
				                         (clientType == 0
					                         ? ""
					                         : (clientType == 1 ? " AND PhysicalClient IS NOT NULL " : " AND PhysicalClient IS NULL ")))
					.List();
			var listToReturn = new List<object>();
			if (idList != null) {
				int idClient = 0;
				int idRecipient = 0;
				for (int i = 0; i < idList.Count; i++) {
					bool isPhysical = (idList[i] as object[])[1] != null;
					string name = ((idList[i] as object[])[1] ?? "").ToString();
					int.TryParse(((idList[i] as object[])[0] ?? "").ToString(), out idClient);
					int.TryParse(((idList[i] as object[])[3] ?? "").ToString(), out idRecipient);
					if (idClient != 0) {
						listToReturn.Add(
							new
							{
								@name = name,
								@id = idClient,
								@url = clientType == 0
									? Url.Action(isPhysical ? "InfoPhysical" : "InfoLegal", new {id = idClient})
									: (clientType == 1
										? Url.Action("InfoPhysical", new {id = idClient})
										: Url.Action("InfoLegal", new {id = idClient})),
								@recipient = idRecipient
							});
					}
				}
			}
			if (listToReturn.Count > 0) {
				return
					Json(listToReturn,
						JsonRequestBehavior.AllowGet);
			}
			return
				Json("Данного ЛС в базе нет",
					JsonRequestBehavior.AllowGet
					);
		}

		/// <summary>
		/// получение ФИО по ЛС
		/// </summary>
		[HttpPost]
		public
		JsonResult getBusyPorts(int id)
		{
			var switchItem = DbSession.Get<Switch>(id);
			if (switchItem != null) {
				var ports =
					switchItem.Endpoints.Select(
						s => new {@endpoint = s.Port, @client = s.Client.Id, @type = s.Client.PhysicalClient != null ? 0 : 1}).ToList();
				var data = new {Ports = ports, Comment = switchItem.Description};
				return Json(data, JsonRequestBehavior.AllowGet);
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// получение Коммутаторов по зоне
		/// </summary>
		[HttpPost]
		public
		JsonResult getSwitchesByZone(string name)
		{
			var switchList = name == String.Empty
				? DbSession.Query<Switch>()
					.Where(s => s.Name != null)
					.Select(s => s.Name)
					.OrderBy(s => s)
					.ToList()
					.Distinct()
					.ToList()
				: DbSession.Query<Switch>()
					.Where(s => s.Zone.Name == name && s.Name != null)
					.Select(s => s.Name)
					.OrderBy(s => s)
					.ToList()
					.Distinct()
					.ToList();

			return Json(switchList, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// получение Ip для фиксирования
		/// </summary>
		[HttpPost]
		public
		JsonResult GetStaticIp(int? id)
		{
			if (!id.HasValue)
				return Json("", JsonRequestBehavior.AllowGet);

			var lease = DbSession.Query<Lease>().FirstOrDefault(l => l.Endpoint.Id == id);
			return
				Json(lease != null &&
				     lease.Ip
				     != null
					? lease.Ip.ToString
						()
					: "",
					JsonRequestBehavior.AllowGet
					);
		}

		/// <summary>
		/// валидация контактов клиента
		/// </summary>
		[HttpPost]
		public JsonResult GetContactValidation(string[] contacts, int[] types)
		{
			if (contacts == null || types == null || contacts.Length != types.Length) {
				return Json("Ошибка в отправке данных", JsonRequestBehavior.AllowGet);
			}
			for (int i = 0; i < contacts.Length; i++) {
				var contactForValidation = new Contact() {ContactString = contacts[i], Type = (ContactType) types[i]};
				var errors = ValidationRunner.ForcedValidationByAttribute<Contact>(contactForValidation, s => s.ContactString,
					new Inforoom2.validators.ValidatorContacts());
				if (errors.Length != 0) {
					return Json($"{errors[0].Message} '{errors[0].Value}' ", JsonRequestBehavior.AllowGet);
				}
			}
			return Json("", JsonRequestBehavior.AllowGet);
		}
	}
}