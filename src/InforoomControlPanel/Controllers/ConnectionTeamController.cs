﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Common.MySql;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using InternetInterface.Helpers;
using NHibernate.Linq;
using NHibernate.Mapping;
using NHibernate.Util;
using Remotion.Linq.Clauses;

namespace InforoomControlPanel.Controllers
{
	public class ConnectionTeamController : ControlPanelController
	{
		public ConnectionTeamController()
		{
			ViewBag.BreadCrumb = "Сервисные бригады";
		}

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.BreadCrumb = "Сервисные бригады";
			ViewBag.TableTimeStep = 30;
		}

		public ActionResult Index()
		{
			return Servicemen();
		}

		/// <summary>
		///     График подключений
		/// </summary>
		/// <returns></returns>
		public ActionResult ConnectionTable(int regionId = 0)
		{
			var team = DbSession.Query<ServiceMan>().ToList();
			var regions = DbSession.Query<Region>().ToList();
			if (regionId != 0) {
				ViewBag.Region = DbSession.Get<Region>(regionId);
				team = team.Where(i => i.Region.Id == regionId).ToList();
			}
			ViewBag.Regions = regions;
			ViewBag.ServicemenDate = DateTime.Today;
			ViewBag.Servicemen = team;
			return View("BigConnectionTable");
		}

		/// <summary>
		///     Асинхронное получение графика подключений по дате
		/// </summary>
		/// <param name="date">Дата для которой отображается график</param>
		/// <param name="regionId">Регион</param>
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
		///     Список неподключенны клиентов
		/// </summary>
		/// <returns></returns>
		public ActionResult UnpluggedClientList()
		{
			var status = DbSession.Query<Status>().First(i => i.ShortName == "BlockedAndNoConnected");

			// формирование фильтра
			var pager = new InforoomModelFilter<Client>(this);
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Id", OrderingDirection.Desc);
			//получение критерия для Hibernate запроса из класса ModelFilter
			var criteria = pager.GetCriteria(i => i.Status == status && i.PhysicalClient != null);

			if (pager.IsExportRequested()) {
				pager.SetExportFields(s => new {
					ЛС = s.Id,
					Дата_создания = s.CreationDate,
					Дата_закрытия = (s.ServicemenScheduleItems != null && s.ServicemenScheduleItems.Count > 0 && s.ServicemenScheduleItems.Any(d => d.RequestType == ServicemenScheduleItem.Type.ClientConnectionRequest && d.ServiceMan != null)
						? s.ServicemenScheduleItems.FirstOrDefault(d => d.RequestType == ServicemenScheduleItem.Type.ClientConnectionRequest).EndTime.ToString() : "Нет"),
					Назначена = (s.ServicemenScheduleItems != null && s.ServicemenScheduleItems.Count > 0 && s.ServicemenScheduleItems.Any(d => d.RequestType == ServicemenScheduleItem.Type.ClientConnectionRequest && d.ServiceMan != null)
						? s.ServicemenScheduleItems.FirstOrDefault(d => d.RequestType == ServicemenScheduleItem.Type.ClientConnectionRequest).ServiceMan.Employee.Name
						: "Не назначена")
				}, true);
				pager.ExportToExcelFile(ControllerContext.HttpContext);
				return null;
			}
			ViewBag.Pager = pager;
			return View();
		}

		/// <summary>
		///     Прикрепление сервисной заявки в график инженеров
		/// </summary>
		/// <param name="id">Идентификатор клиента (заявка на подключение) / сервисной заявки</param>
		/// <param name="type">Тип записи в графике инженеров</param>
		/// <returns></returns>
		public ActionResult AttachRequest(int id, ServicemenScheduleItem.Type type)
		{
			// получение записи в графике (новой/существующей)
			var scheduleItem = ServicemenScheduleItem.GetSheduleItem(DbSession, id, type);
			// получение списков для передачи на форму
			var servicemen = DbSession.Query<ServiceMan>().ToList();
			var regions = DbSession.Query<Region>().ToList();
			ViewBag.Regions = regions;
			// получение информации о записи в графике для передачи на форму
			if (scheduleItem != null) {
				ViewBag.ServicemenScheduleItem = scheduleItem;
				ViewBag.Servicemen = servicemen;
				ViewBag.ServicemenDate = scheduleItem.BeginTime.HasValue &&
				                         scheduleItem.BeginTime != Convert.ToDateTime("01.01.0001 0:00:00")
					? scheduleItem.BeginTime
					: ViewBag.ServicemenDate ?? DateTime.Today;
				var duration =
					((scheduleItem.EndTime.HasValue ? scheduleItem.EndTime.Value : Convert.ToDateTime("01.01.0001 0:00:00"))
					 - (scheduleItem.BeginTime.HasValue ? scheduleItem.BeginTime.Value : Convert.ToDateTime("01.01.0001 0:00:00")))
						.TotalMinutes;

				var clientRegion = scheduleItem.GetClient().GetRegion() ?? regions[0];

				ViewBag.Region = (scheduleItem.ServiceMan != null
					? scheduleItem.ServiceMan.Region
					: clientRegion);
				ViewBag.Duration = duration > 0 ? duration : 60;
			}
			return View();
		}

		/// <summary>
		///     Прикрепление сервисной заявки в график инженеров
		/// </summary>
		/// <param name="scheduleItem">Запись в графике инженеров</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult AttachRequest([EntityBinder] ServicemenScheduleItem scheduleItem)
		{
			var duration = int.Parse(Request.Form["duration"]);
			scheduleItem.EndTime = scheduleItem.BeginTime.Value.AddMinutes(duration);
			ViewBag.Duration = duration;
			ViewBag.ServicemenDate = scheduleItem.BeginTime.Value.Date;

			// проверка, если назначенное время в графике исполнителя не занято
			if ((scheduleItem.BeginTime != null
			     || scheduleItem.BeginTime != Convert.ToDateTime("01.01.0001 0:00:00"))
			    && (scheduleItem.EndTime != null
			        || scheduleItem.EndTime != Convert.ToDateTime("01.01.0001 0:00:00"))
			    && scheduleItem.ServiceMan.SheduleItems.Any(serv =>
				    ((serv.RequestType == ServicemenScheduleItem.Type.ServiceRequest && serv.ServiceRequest.Status != ServiceRequestStatus.Cancel)
				     || serv.RequestType == ServicemenScheduleItem.Type.ClientConnectionRequest) &&
				    serv.Id != scheduleItem.Id &&
				    (serv.BeginTime > scheduleItem.BeginTime && serv.BeginTime < scheduleItem.EndTime ||
				     serv.EndTime > scheduleItem.BeginTime && serv.EndTime < scheduleItem.EndTime))) {
				//вывод сообщения
				ErrorMessage("Назначенное время в графике исполнителя уже занято!");
				//обновление сессии (связанно с эл-ми на форме)
				DbSession.Clear();
				DbSession.Flush();
				//переход к назначению заявки
				if (scheduleItem.RequestType == ServicemenScheduleItem.Type.ServiceRequest)
					return RedirectToAction("AttachRequest", "ConnectionTeam",
						new { id = scheduleItem.ServiceRequest.Id, type = scheduleItem.RequestType });
				if (scheduleItem.RequestType == ServicemenScheduleItem.Type.ClientConnectionRequest)
					return RedirectToAction("AttachRequest", "ConnectionTeam",
						new { id = scheduleItem.GetClient().Id, type = scheduleItem.RequestType });
			}
			//валидация эл-та графика
			var errors = ValidationRunner.Validate(scheduleItem);
			if (errors.Length == 0) {
				// проверка свободного времени исполнителя заявки

				DbSession.Save(scheduleItem);
				//вывод сообщения
				if (scheduleItem.RequestType == ServicemenScheduleItem.Type.ServiceRequest) {
					SuccessMessage("Сервисная заявка успешно добавлена в график");
					//отправка уведомления, о назначенной сервисной заявке
					var appealMessage = string.Format("Сервисная заявка добавлена в график. <br/>Инженер: {0}<br/>Дата / время: {1}", scheduleItem.ServiceMan.Employee.Name, scheduleItem.BeginTime);
					var newAppeal = new Appeal(appealMessage, scheduleItem.GetClient(), AppealType.User) { Employee = GetCurrentEmployee() };
					DbSession.Save(newAppeal);
				}
				else {
					SuccessMessage("Заявка на подключение успешно добавлена в график");
					//отправка уведомления, о назначенном подключении
					var appealMessage = string.Format("Подключение назначено в график.<br/>Инженер: {0}<br/>Дата / время: {1}", scheduleItem.ServiceMan.Employee.Name, scheduleItem.BeginTime);
					var newAppeal = new Appeal(appealMessage, scheduleItem.GetClient(), AppealType.User) { Employee = GetCurrentEmployee() };
					DbSession.Save(newAppeal);
				}
				//обновление сессии (связанно с эл-ми на форме)
				DbSession.Flush();
				DbSession.Clear();
				//переход к назначенной заявке
				if (scheduleItem.RequestType == ServicemenScheduleItem.Type.ServiceRequest)
					return RedirectToAction("AttachRequest", "ConnectionTeam",
						new { id = scheduleItem.ServiceRequest.Id, type = scheduleItem.RequestType });
				if (scheduleItem.RequestType == ServicemenScheduleItem.Type.ClientConnectionRequest)
					return RedirectToAction("AttachRequest", "ConnectionTeam",
						new { id = scheduleItem.GetClient().Id, type = scheduleItem.RequestType });
			}
			//переход к назначению заявки
			if (scheduleItem.RequestType == ServicemenScheduleItem.Type.ServiceRequest)
				return RedirectToAction("AttachRequest", "ConnectionTeam",
					new { id = scheduleItem.ServiceRequest.Id, type = scheduleItem.RequestType });
			if (scheduleItem.RequestType == ServicemenScheduleItem.Type.ClientConnectionRequest)
				return RedirectToAction("AttachRequest", "ConnectionTeam",
					new { id = scheduleItem.GetClient().Id, type = scheduleItem.RequestType });

			ViewBag.ServicemenScheduleItem = scheduleItem;
			return View();
		}

		public ActionResult CancelConnectionRequest(int id)
		{
			var item = DbSession.Query<ServicemenScheduleItem>().FirstOrDefault(s => s.Id == id);
			if (item != null) {
				DbSession.Delete(item);
				// return RedirectToAction("AttachRequest",new {id= });
			}
			return RedirectToAction("UnpluggedClientList");
		}

		/// <summary>
		///     График подключения бригад
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
		///     Список сервисных инженеров
		/// </summary>
		/// <returns></returns>
		public ActionResult Servicemen()
		{
			var team = DbSession.Query<ServiceMan>().OrderBy(s => s.Region).ThenBy(a => a.Employee.Name).ToList();
			var employees =
				DbSession.Query<Employee>().ToList().Where(j => team.All(i => i.Employee != j)).OrderBy(i => i.Name).ToList();
			var regions = DbSession.Query<Region>().ToList();
			ViewBag.Regions = regions;
			ViewBag.ServiceMan = new ServiceMan();
			ViewBag.Team = team;
			ViewBag.Employees = employees;
			return View("Servicemen");
		}

		/// <summary>
		///     Создание нового сервисного инженера
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Servicemen([EntityBinder] ServiceMan ServiceMan)
		{
			var errors = ValidationRunner.ValidateDeep(ServiceMan);
			if (errors.Length == 0) {
				DbSession.Save(ServiceMan);
				SuccessMessage("Сервисный инженер успешно добавлен");
				return RedirectToAction("Servicemen");
			}
			Servicemen();
			ViewBag.newServiceman = ServiceMan;
			return View();
		}

		/// <summary>
		///     Удаление сервисного инженера
		/// </summary>
		/// <returns></returns>
		public ActionResult ServicemenDelete(int id)
		{
			var serviceManToDelete = DbSession.Query<ServiceMan>().FirstOrDefault(s => s.Id == id);
			if (serviceManToDelete != null) {
				if (DbSession.AttemptDelete(serviceManToDelete)) {
					SuccessMessage("Сервисный инженер успешно удален");
				}
				else {
					ErrorMessage("Сервисный инженер не может быть удален");
				}
			}
			else {
				ErrorMessage("Сервисного инженера с данным номером не существует");
			}
			return RedirectToAction("Servicemen");
		}

		public ActionResult PrintTimeTable(int printServicemenId = 0, int printRegionId = 0, string printDate = "", string updatePasswordsKey = "")
		{
			var additionalData = new Dictionary<ServicemenScheduleItem, Tuple<string, string>>();
			var dateTime = printDate == string.Empty ? SystemTime.Now().Date : Convert.ToDateTime(printDate);
			var servicemenScheduleQuery = DbSession.Query<ServicemenScheduleItem>().Where(s => s.BeginTime.Value.Date == dateTime.Date);

			if (printServicemenId != 0) {
				servicemenScheduleQuery = servicemenScheduleQuery.Where(s => s.ServiceMan.Id == printServicemenId);
			}

			var servicemenScheduleList = servicemenScheduleQuery.ToList();

			if (printRegionId != 0) {
				servicemenScheduleList = servicemenScheduleList.Where(s => s.GetClient().GetRegion().Id == printRegionId).ToList();
			}

			if (Session["printKey"] != null && Session["printKey"].ToString() == updatePasswordsKey) {
				//обновление паролей
				foreach (var item in servicemenScheduleList) {
					if (item.RequestType == ServicemenScheduleItem.Type.ClientConnectionRequest) {
						var physicalClient = item.GetClient().PhysicalClient;
						var password = CryptoPass.GeneratePassword();
						physicalClient.Password = CryptoPass.GetHashString(password);
						DbSession.Save(physicalClient);
						additionalData.Add(item, new Tuple<string, string>(item.GetClient().Id.ToString(), password));
					}
				}
			}
			//ключ для обновления паролей
			string updateKey = Md5.GetHash(new Random().Next(1000000, 9999999).ToString());
			ViewBag.UpdatePasswordsKey = updateKey;
			Session.Add("printKey", updateKey);

			ViewBag.PrintServicemenId = printServicemenId;
			ViewBag.PrintRegionId = printRegionId;
			ViewBag.PrintDate = printDate;
			ViewBag.UpdatePasswordsKey = updateKey;
			ViewBag.ServicemenScheduleList = servicemenScheduleList;
			ViewBag.AdditionalData = additionalData;
			return View();
		}
	}
}