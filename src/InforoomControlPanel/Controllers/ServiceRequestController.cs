using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Helpers;
using Inforoom2.Models;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using NHibernate.Util;
using Remotion.Linq.Clauses;
using AppealType = Inforoom2.Models.AppealType;
using Client = Inforoom2.Models.Client;
using ServiceRequest = Inforoom2.Models.ServiceRequest;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления заявками на подключение
	/// </summary>
	public class ServiceRequestController : ControlPanelController
	{
		public ServiceRequestController()
		{
			ViewBag.BreadCrumb = "Сервисные заявки";
		}

		/// <summary>
		/// Список сервисных заявок
		/// </summary> 
		public ActionResult ServiceRequestList()
		{
			// формирование фильтра
			var pager = new InforoomModelFilter<ServiceRequest>(this);
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Id", OrderingDirection.Desc);
			//получение критерия для Hibernate запроса из класса ModelFilter
			var criteria = pager.GetCriteria();


			//объединение 'сервисной заявки' с 'клиентом'
			var tempCriteria = criteria.GetCriteriaByPath("Client") ?? criteria.CreateCriteria("Client", JoinType.LeftOuterJoin);
			// для фильтра: Статус заявки
			if (!string.IsNullOrEmpty(pager.GetParam("ServiceRequestStatus")))
				//добавление условиий фильтрации
				criteria.Add(Restrictions.Eq("Status", (Inforoom2.Models.ServiceRequestStatus)int.Parse(pager.GetParam("ServiceRequestStatus"))));
			// для фильтра: Назначения на инженера
			if (!string.IsNullOrEmpty(pager.GetParam("ServiceMenFilter")) && pager.GetParam("ServiceMenFilter") != "0") {
				//объединение 'сервисной заявки' с 'записью в графике инженеров'
				criteria.CreateCriteria("ServicemenScheduleItem", "serviceManFor", JoinType.LeftOuterJoin);
				//добавление условиий фильтрации
				criteria.Add(Restrictions.Eq("serviceManFor.ServiceMan.Id", int.Parse(pager.GetParam("ServiceMenFilter"))));
			}

			// для фильтра: Поисковая фраза
			if (!string.IsNullOrEmpty(pager.GetParam("TextSearch"))) {
				//проверка, если поисковая фраза не является числом
				string textSearch = pager.GetParam("TextSearch").Trim();
				int idValue = 0;
				Int32.TryParse(pager.GetParam("TextSearch"), out idValue);
				//добавление условиий фильтрации

				criteria.Add(Restrictions.Or(
					Restrictions.Or(Restrictions.Like("Description", "%" + textSearch + "%"), Restrictions.Like("Phone", "%" + textSearch + "%")),
					Restrictions.Or(Restrictions.Eq("Client.Id", idValue), Restrictions.Eq("Id", idValue))
					));
			}

			// для фильтра: Интервал даты с / по
			if (pager.GetParam("DataFilter") != "BeginTime") {
				// фильтрация по датам, принадлежащих моделе сервисной заявки
				if (!string.IsNullOrEmpty(pager.GetParam("RequestFilterFrom")))
					//добавление условиий фильтрации
					criteria.Add(Restrictions.Gt(pager.GetParam("DataFilter"), DateTime.Parse(pager.GetParam("RequestFilterFrom"))));
				if (!string.IsNullOrEmpty(pager.GetParam("RequestFilterTill")))
					//добавление условиий фильтрации
					criteria.Add(Restrictions.Lt(pager.GetParam("DataFilter"), DateTime.Parse(pager.GetParam("RequestFilterTill")).AddDays(1)));
			}
			else {
				// дату постановления заявки в график фильтруем с добавлением субкритерия
				//проверка, если 'сервисная заявка' еще не объединена с 'записью в графике инженеров'. Если нет, объединяем
				var newCriteria = criteria.GetCriteriaByAlias("serviceManFor") ?? criteria.CreateCriteria("ServicemenScheduleItem", "serviceManFor", JoinType.LeftOuterJoin);
				//добавление условиий фильтрации
				if (!string.IsNullOrEmpty(pager.GetParam("RequestFilterFrom")))
					newCriteria.Add(Restrictions.Gt("serviceManFor.BeginTime", DateTime.Parse(pager.GetParam("RequestFilterFrom"))));
				;
				if (!string.IsNullOrEmpty(pager.GetParam("RequestFilterTill")))
					newCriteria.Add(Restrictions.Lt("serviceManFor.BeginTime", DateTime.Parse(pager.GetParam("RequestFilterTill")).AddDays(1)));
			}

			if (pager.IsExportRequested()) {
				pager.SetExportFields(s => new { ЛС = s.Client.Id, Номер_сз = s.Id, Дата_создания = s.CreationDate, Дата_закрытия = s.ClosedDate, Сумма = s.Sum, Назначена = s.ServicemenScheduleItem.ServiceMan.Employee.Name });
				pager.ExportToExcelFile(ControllerContext.HttpContext);
				return null;
			}
			// Сбор во ViewBag необходимых объектов
			ViewBag.Regions = DbSession.Query<Region>().ToList();
			ViewBag.ServiceMan = DbSession.Query<ServiceMan>().ToList();
			ViewBag.Pager = pager;
			return View();
		}

		/// <summary>
		/// Создание заявки на подключение
		/// </summary> 
		/// <param name="id">Id клиента, оставляющего заявку</param>
		/// <returns></returns>
		public ActionResult ServiceRequestCreate(int id)
		{
			//получение клиента 
			var client = DbSession.Query<Client>().FirstOrDefault(s => s.Id == id);
			if (client != null) {
				//получение телефона-'по умолчанию'
				var phone = client.Contacts.FirstOrDefault(s => s.Type == ContactType.SmsSending);
				//создание заявки и передача ее на форму
				var serviceRequest = new ServiceRequest() { Client = client, Phone = phone != null ? phone.ContactString : "" };
				ViewBag.ServiceRequest = serviceRequest;
				return View();
			}
			//переход на форму списка клиентов
			return RedirectToAction("List", "Client");
		}

		/// <summary>
		/// Создание сервисной заявки
		/// </summary>
		/// <param name="serviceRequest">модель сервисной заявки</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult ServiceRequestCreate([EntityBinder] ServiceRequest serviceRequest)
		{
			var errors = ValidationRunner.ValidateDeep(serviceRequest);
			if (errors.Length == 0) {
				serviceRequest.Employee = GetCurrentEmployee();
				serviceRequest.TrySwitchClientStatusTo_BlockedForRepair(DbSession, GetCurrentEmployee());
				serviceRequest.ModificationDate = SystemTime.Now();
				DbSession.Save(serviceRequest);
				SuccessMessage("Сервисная заявка успешно создана.");
				//Отправляем аппил о создании
				string appealMessage = string.Format("Сервисная заявка № <a href='{1}ServiceRequest/ServiceRequestEdit/{0}'>{0}</a> успешно создана.",
					serviceRequest.Id, ConfigHelper.GetParam("adminPanelNew"));
				DbSession.Save(new Appeal(appealMessage, serviceRequest.Client, AppealType.User) { Employee = serviceRequest.Employee, inforoom2 = true });

				return RedirectToAction("ServiceRequestEdit", new { id = serviceRequest.Id });
			}
			ViewBag.ServiceRequest = serviceRequest;
			return View();
		}

		/// <summary>
		/// Редактирование сервисной заявки
		/// </summary> 
		/// <param name="id">Id сервисной заявки</param>
		/// <returns></returns>
		public ActionResult ServiceRequestEdit(int id)
		{
			var serviceRequest = DbSession.Query<ServiceRequest>().FirstOrDefault(s => s.Id == id);
			ViewBag.ServiceRequest = serviceRequest;
			ViewBag.ReasonForFreeShown = !serviceRequest.Free;
			ViewBag.СurrentStatus = serviceRequest.Status;
			ViewBag.ServicemenScheduleItem = DbSession.Query<ServicemenScheduleItem>()
				.FirstOrDefault(s => s.ServiceRequest.Id == id && s.RequestType == ServicemenScheduleItem.Type.ServiceRequest);
			ViewBag.ServiceRequestCommentList = serviceRequest.GetComments(DbSession);
			return View();
		}

		/// <summary>
		/// Редактирование сервисной заявки
		/// </summary>
		/// <param name="serviceRequest">модель сервисной заявки</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult ServiceRequestEdit([EntityBinder] ServiceRequest serviceRequest,
			ServiceRequestComment reasonForFree, bool reasonForFreeShown, Inforoom2.Models.ServiceRequestStatus currentStatus)
		{
			// проверяем, бесплатна ли заявка
			serviceRequest.Sum = serviceRequest.Free ? null : serviceRequest.Sum;
			// валидация
			var errors = ValidationRunner.Validate(serviceRequest);
			// если должна быть описана причина бесплатности заявки - валидация заявки
			if (serviceRequest.Free && reasonForFreeShown) {
				errors.AddRange(ValidationRunner.Validate(reasonForFree));
				if (errors.Length == 0) {
					serviceRequest.AddComment(DbSession, string.Format("<strong>Заявка стала бесплатной</strong>, поскольку: {0}", reasonForFree.Comment), GetCurrentEmployee());
				}
			}

			// если нет ошибок
			if (errors.Length == 0) {
				// изменяем статус, при необходимости
				if (serviceRequest.Status != currentStatus) {
					serviceRequest.SetStatus(DbSession, currentStatus, GetCurrentEmployee());
				}
				// сохраняем изменения
				serviceRequest.ModificationDate = SystemTime.Now();
				DbSession.Save(serviceRequest);
				SuccessMessage("Сервисная заявка успешно обновлена.");

				//Отправляем аппил о редактировании
				string appealMessage = string.Format("Сервисная заявка № <a href='{1}ServiceRequest/ServiceRequestEdit/{0}'>{0}</a> обновлена.",
					serviceRequest.Id, ConfigHelper.GetParam("adminPanelNew"));
				DbSession.Save(new Appeal(appealMessage, serviceRequest.Client, AppealType.User) { Employee = GetCurrentEmployee(), inforoom2 = true });

				return RedirectToAction("ServiceRequestEdit", new { id = serviceRequest.Id });
			}

			// Сбор во ViewBag необходимых объектов
			ViewBag.ReasonForFreeShown = reasonForFreeShown;
			ViewBag.СurrentStatus = currentStatus;
			ViewBag.ServiceRequest = serviceRequest;
			ViewBag.ReasonForFree = reasonForFree;
			ViewBag.ServiceRequestCommentList = serviceRequest.GetComments(DbSession);
			//на форме происходит flush сессии (перед передачей э-тов на форму и закрытием сессии запрещаем auto-flush)
			DbSession.FlushMode = FlushMode.Never;
			return View();
		}

		/// <summary>
		/// Изменение статуса клиента, связанное с восстановительными работами
		/// </summary>
		/// <param name="id">Id сервисной заявки</param>
		/// <returns></returns>
		public ActionResult BlockAdd(int id)
		{
			//получение сервисной заявки
			var serviceRequest = DbSession.Query<ServiceRequest>().FirstOrDefault(s => s.Id == id);
			//установление флага
			serviceRequest.BlockClientAndWriteOffs = true;
			//попытка изменить статус пользователя
			serviceRequest.TrySwitchClientStatusTo_BlockedForRepair(DbSession, GetCurrentEmployee());
			DbSession.Save(serviceRequest);
			//вывод сообщения
			SuccessMessage("Сервисная заявка успешно обновлена.");
			//переход к редактированию сервисной заяки
			return RedirectToAction("ServiceRequestEdit", new { id = serviceRequest.Id });
		}

		/// <summary>
		/// Добавление комментария к сервисной заявке
		/// </summary>
		/// <param name="serviceRequest">модель сервисной заявки</param>
		/// <param name="comment">модель комментария</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult AddComment([EntityBinder] ServiceRequest serviceRequest, ServiceRequestComment comment)
		{
			ViewBag.ServiceRequestComment = comment;
			//вализация комментария
			var errors = ValidationRunner.ValidateDeep(comment);
			if (errors.Length == 0 && serviceRequest != null && serviceRequest.Id != 0) {
				//добавление комментария
				serviceRequest.AddComment(DbSession, comment.Comment, GetCurrentEmployee());
				//вывод сообщения
				SuccessMessage("Комментарий был добавлен успешно.");
				//переход к редактированию сервисной заяки
				return RedirectToAction("ServiceRequestEdit", new { id = serviceRequest.Id });
			}
			//переход к редактированию сервисной заяки
			ViewBag.ServiceRequest = serviceRequest;
			ViewBag.ServiceRequestCommentList = serviceRequest.GetComments(DbSession);
			return View("ServiceRequestEdit", new { id = serviceRequest.Id });
		}
	}
}