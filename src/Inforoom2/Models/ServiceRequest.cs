using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web.Services.Description;
using Common.MySql;
using Common.Tools;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using Inforoom2.validators;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Статус запроса
	/// </summary>
	public enum ServiceRequestStatus
	{
		[Display(Name = "Новая"), Description("Новая")] New = 1,
		[Display(Name = "Закрыта"), Description("Закрыта")] Close = 3,
		[Display(Name = "Отменена"), Description("Отменена")] Cancel = 5
	}

	[Class(0, Table = "ServiceRequest", NameType = typeof(ServiceRequest)), Description("Сервисная заявка")]
	public class ServiceRequest : BaseModel, IServicemenScheduleItem
	{
		public ServiceRequest()
		{
			CreationDate = SystemTime.Now();
			ModificationDate = SystemTime.Now();
			Status = ServiceRequestStatus.New;
		}

		public ServiceRequest(Client client)
			: this()
		{
			CreationDate = SystemTime.Now();
			ModificationDate = SystemTime.Now();
			Status = ServiceRequestStatus.New;
			Client = client;
		}

		[OneToOne(PropertyRef = "ServiceRequest")]
		public virtual ServicemenScheduleItem ServicemenScheduleItem { get; set; }

		[ManyToOne(Column = "Registrator"), Description("Зарегистрировал")]
		public virtual Employee Employee { get; set; }

		[ManyToOne(Column = "Client"), Description("Клиент")]
		public virtual Client Client { get; set; }

		[Property(Column = "Description"), NotNullNotEmpty(Message = "Поле необходимо заполнить"), Description("Описание заявки")]
		public virtual string Description { get; set; }

		// TODO: удалить это поле в скором будущем!
		[Property(Column = "BlockForRepair"), Description("Блокировать клиента и списание")]
		public virtual bool BlockClientAndWriteOffs { get; set; }

		[Property(Column = "Contact"), Description("Телефон"), ValidatorPhone]
		public virtual string Phone { get; set; }

		[Property(Column = "RegDate"), Description("Дата создания заявки")]
		public virtual DateTime CreationDate { get; set; }

		[Property(Column = "CancelDate"), Description("Дата отмены заявки")]
		public virtual DateTime? CancelDate { get; set; }

		private ServiceRequestStatus privateStatus { get; set; }

		[Property, Description("Статус сервисной заявки")]
		public virtual ServiceRequestStatus Status
		{
			get { return privateStatus; }
			protected set { privateStatus = value; }
		}

		[Property, Description("Сумма за предоставленные услуги")]
		public virtual decimal? Sum { get; set; }

		[Property, Description("Бесплатная заявка")]
		public virtual bool Free { get; set; }

		[Property, Description("Дата выполнения заявки")]
		public virtual DateTime? PerformanceDate { get; set; }

		[Property, Description("Дата последней модификации")]
		public virtual DateTime ModificationDate { get; set; }

		[Property, Description("Дата закрытия заявки")]
		public virtual DateTime? ClosedDate { get; set; }

		public virtual string GetAddress()
		{
			return Client.Address.GetStringForPrint();
		}

		public virtual Client GetClient()
		{
			return Client;
		}

		public virtual string GetPhone()
		{
			return Phone;
		}

		/// <summary>
		/// Проверка просрочки заявки
		/// </summary>
		/// <param name="settings">модель настроек</param>
		/// <returns>Подтверждение просрочки</returns>
		public virtual bool IsOverdue(SaleSettings settings)
		{
			if (Status == ServiceRequestStatus.New)
				return SystemTime.Now().Date > CreationDate.AddDays(settings.DaysForRepair);
			return false;
		}

		/// <summary>
		/// Смена статуса заявки
		/// </summary>
		/// <param name="dbSession">Сессия БД</param>
		/// <param name="status">Статус заявки</param>
		public virtual void SetStatus(ISession dbSession, ServiceRequestStatus status, Employee employee)
		{
			switch (status) {
				case ServiceRequestStatus.Close:
					if (Status != status) {
						Status = status;
						Close(dbSession, employee);
					}
					break;
				case ServiceRequestStatus.Cancel:
					if (Status != status) {
						Status = status;
						Cancel(dbSession, employee);
					}

					break;
				default:
					Status = status;
					break;
			}
		}

		/// <summary>
		/// При отмене заявки
		/// </summary>
		private void Cancel(ISession dbSession, Employee employee)
		{
			TrySwitchClientStatusTo_Worked(dbSession);
			// вероятно, в скором будущем отправка смс
			CancelDate = SystemTime.Now();
			string appealMessage = string.Format("Сервисная заявка № <a href='{1}ServiceRequest/ServiceRequestEdit/{0}'>{0}</a> <strong>отменена</strong>.",
				Id, ConfigHelper.GetParam("adminPanelNew"));
			dbSession.Save(new Appeal(appealMessage, Client, AppealType.User) { Employee = employee, inforoom2 = true });
		}

		/// <summary>
		/// При закрытии заявки
		/// </summary>
		private void Close(ISession dbSession, Employee employee)
		{
			TrySwitchClientStatusTo_Worked(dbSession);
			ClosedDate = SystemTime.Now();
			//списание средств
			if (Sum != null
			    && Sum > 0) {
				var comment = String.Format("Оказание дополнительных услуг, заявка №{0}", Id);
				dbSession.Save(new UserWriteOff(Client, Sum.Value, comment) { Employee = employee });
			}

			string appealMessage = string.Format("Сервисная заявка № <a href='{1}ServiceRequest/ServiceRequestEdit/{0}'>{0}</a> <strong>закрыта</strong>.",
				Id, ConfigHelper.GetParam("adminPanelNew"));
			dbSession.Save(new Appeal(appealMessage, Client, AppealType.User) { Employee = employee, inforoom2 = true });
		}

		/// <summary>
		/// Попытка выставить клиенту статус Worked, если для этого выполнены все условия
		/// </summary> 
		public virtual void TrySwitchClientStatusTo_Worked(ISession dbSession)
		{
			if (BlockClientAndWriteOffs
			    && Client.Status.Type == StatusType.BlockedForRepair
			    && (Status == ServiceRequestStatus.Close || Status == ServiceRequestStatus.Cancel)
			    && !dbSession.Query<ServiceRequest>().Any(r => r.Client == Client && r.Status == ServiceRequestStatus.New && r.BlockClientAndWriteOffs)) {
				ModificationDate = SystemTime.Now();
				Client.SetStatus(Models.Status.Get(StatusType.Worked, dbSession));
			}
		}

		/// <summary>
		/// Попытка выставить клиенту статус Worked, если для этого выполнены все условия
		/// </summary> 
		public virtual void TrySwitchClientStatusTo_BlockedForRepair(ISession dbSession, Employee employee)
		{
			if (BlockClientAndWriteOffs && Client.PhysicalClient != null) {
				ModificationDate = SystemTime.Now();
				Client.SetStatus(Models.Status.Get(StatusType.BlockedForRepair, dbSession));

				string appealMessage = string.Format("По сервисной заявке № <a href='{1}ServiceRequest/ServiceRequestEdit/{0}'>{0}</a> <strong>необходимо восстановление работы.</strong>.",
					Id, ConfigHelper.GetParam("adminPanelNew"));
				dbSession.Save(new Appeal(appealMessage, Client, AppealType.User) { Employee = employee, inforoom2 = true });
			}
		}

		/// <summary>
		/// Добавление комментария к заявки
		/// </summary>
		/// <param name="dbSession">Сессия БД</param>
		/// <param name="comment">Комментарий</param>
		public virtual void AddComment(ISession dbSession, string comment, Employee employee)
		{
			//формирование и сохранение комментария
			dbSession.Save(new ServiceRequestComment() {
				Comment = comment,
				ServiceRequest = this,
				Author = employee,
				CreationDate = SystemTime.Now()
			});
			string appealMessage = string.Format("Комментарий к сервисной заявке № <a href='{1}ServiceRequest/ServiceRequestEdit/{0}'>{0}</a> :<br/>{2}",
				Id, ConfigHelper.GetParam("adminPanelNew"), comment);
			dbSession.Save(new Appeal(appealMessage, Client, AppealType.User) { Employee = employee, inforoom2 = true });
		}

		/// <summary>
		/// Получение комментариев по данному запросу из списков комментариев и логов 
		/// </summary> 
		public virtual List<ServiceRequestComment> GetComments(ISession dbSession)
		{
			//получение списка комментариев по текущей сервисной заявке
			var commentsList = dbSession.Query<ServiceRequestComment>().Where(s => s.ServiceRequest == this).ToList();
			// дополнение списка комментариев событиями из логов по сервисным заявкам
			commentsList.AddRange(dbSession.Query<Log>().Where(s => s.ModelId == this.Id
			                                                        && s.ModelClass == this.GetType().Name && s.Type == LogEventType.Update).Select(s => new ServiceRequestComment() {
				                                                        // убираем ModificationDate из логов т.к. запись будет дублировать CreationDate
				                                                        Comment = s.Message.Replace("title='ModificationDate'", "title='ModificationDate' style='display:none;'"),
				                                                        ServiceRequest = this,
				                                                        CreationDate = s.Date,
				                                                        Author = s.Employee
			                                                        }).ToList());
			//возврат сортированного списка комментариев
			return commentsList.OrderBy(s => s.CreationDate).ToList();
		}
	}
}