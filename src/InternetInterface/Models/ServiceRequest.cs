using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Audit;
using Common.Web.Ui.NHibernateExtentions;
using NHibernate;

namespace InternetInterface.Models
{
	public enum ServiceRequestStatus
	{
		[Description("Новый")] New = 1,
		[Description("Закрыт")] Close = 3,
		[Description("Отменен")] Cancel = 5
	}

	[ActiveRecord("ServiceRequest", Schema = "internet", Lazy = true), Auditable]
	public class ServiceRequest
	{
		private ServiceRequestStatus _status;

		public ServiceRequest()
		{
			RegDate = DateTime.Now;
			Status = ServiceRequestStatus.New;
			Iterations = new List<ServiceIteration>();
		}

		public ServiceRequest(Partner registrator, Partner performer, DateTime performanceDate)
			: this(registrator)
		{
			Performer = performer;
			PerformanceDate = performanceDate;
		}

		public ServiceRequest(Partner registrator)
			: this()
		{
			Registrator = registrator;
			PerformanceDate = DateTime.Today.AddDays(1);
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty, Description("Текст заявки")]
		public virtual string Description { get; set; }

		[Property,
			ValidateNonEmpty,
			ValidateRegExp(@"^\d{3}-\d{7}$", "Ошибка формата телефонного номера: мобильный телефон (***-*******)"),
			Description("Контактный телефон"),
			Auditable]
		public virtual string Contact { get; set; }

		[Property, Auditable("Статус сервисной заявки")]
		public virtual ServiceRequestStatus Status
		{
			get { return _status; }

			set
			{
				if (value == _status)
					return;

				if (value == ServiceRequestStatus.Close) {
					ClosedDate = DateTime.Now;
				}
				if (value == ServiceRequestStatus.Cancel) {
					CancelDate = DateTime.Now;
				}

				_status = value;
			}
		}

		[BelongsTo]
		public virtual Client Client { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[Property]
		public virtual DateTime? ClosedDate { get; set; }

		[Property]
		public virtual DateTime? CancelDate { get; set; }

		[Property, Auditable("Дата выполнения заявки")]
		public virtual DateTime? PerformanceDate { get; set; }

		public virtual TimeSpan? PerformanceTime
		{
			get
			{
				if (PerformanceDate == null)
					return null;
				return PerformanceDate.Value.TimeOfDay;
			}
			set
			{
				if (PerformanceDate == null)
					return;
				PerformanceDate = PerformanceDate.Value.Add(-PerformanceDate.Value.TimeOfDay);
				if (value != null)
					PerformanceDate = PerformanceDate.Value.Add(value.Value);
			}
		}

		[Property, Auditable("Сумма за предоставленные услуги")]
		public virtual decimal? Sum { get; set; }

		[Property, Auditable("Бесплатная заявка")]
		public virtual bool Free { get; set; }

		[Description("Текст смс для отправки клиенту")]
		public virtual string CloseSmsMessage { get; set; }

		[Description("Причина задержки выполения заявки")]
		public virtual string OverdueReason { get; set; }

		[Property, Description("Восстановление работы")]
		public virtual bool BlockForRepair { get; set; }

		[BelongsTo]
		public virtual Partner Registrator { get; set; }

		[BelongsTo, Auditable("Исполнитель"), ValidateNonEmpty]
		public virtual Partner Performer { get; set; }

		[HasMany(ColumnKey = "Request", OrderBy = "RegDate", Lazy = true, Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public virtual IList<ServiceIteration> Iterations { get; set; }

		public virtual bool IsOverdue { get; protected set; }

		public virtual void Calculate(SaleSettings settings)
		{
			if (Status == ServiceRequestStatus.New)
				IsOverdue = DateTime.Now > RegDate.AddDays(settings.DaysForRepair);
		}

		public virtual string GetDescription()
		{
			return AppealHelper.GetTransformedAppeal(Description);
		}

		public virtual string GetMinDiscription()
		{
			if (Description != null)
				return AppealHelper.GetTransformedAppeal(Description.Take(100).Implode(string.Empty)) + (Description.Length > 100 ? "..." : string.Empty);
			return string.Empty;
		}

		public virtual UserWriteOff GetWriteOff(ISession session)
		{
			if (Status == ServiceRequestStatus.Close
				&& session.IsChanged(this, r => r.Status)) {
				if (Sum != null
					&& Sum > 0) {
					var comment = String.Format("Оказание дополнительных услуг, заявка №{0}", Id);
					return new UserWriteOff(Client, Sum.Value, comment);
				}
			}
			return null;
		}

		public virtual List<SmsMessage> GetEditSms(ISession session)
		{
			var messages = new List<SmsMessage>();
			if (Status == ServiceRequestStatus.New && session.IsChanged(this, r => r.Performer)) {
				messages.AddRange(new[] {
					GetCancelSms(session.OldValue(this, r => r.Performer)),
					GetNewSms()
				});
			}

			if (Status == ServiceRequestStatus.Cancel && session.IsChanged(this, r => r.Status)) {
				messages.Add(GetCancelSms(Performer));
			}

			if (Status == ServiceRequestStatus.Close
				&& session.IsChanged(this, r => r.Status)
				&& Sum > 0
				&& Client.Type == ClientType.Phisical) {
				var text = String.Format("С Вашего счета списано {0:C} по сервисной заявке №{1} от {2:d} {3}",
					Sum, Id, RegDate, CloseSmsMessage);
				messages.Add(new SmsMessage(Client, Contact, text));
			}

			return messages.Where(m => m != null).ToList();
		}

		public virtual SmsMessage GetNewSms()
		{
			if (!ShouldSendSms(Performer))
				return null;

			var endPoint = Client.Endpoints.FirstOrDefault();
			var port = endPoint != null ? endPoint.Port.ToString() : string.Empty;
			var sms = new SmsMessage(Performer.TelNum) {
				Text = "$"
			};

			if (PerformanceDate != null)
				sms.Text += " " + PerformanceDate;
			sms.Text += " " + String.Format("{0} т. {1} п. {2} сч. {3}",
				Client.GetCutAdress(),
				"+7-" + Contact.Replace("-", string.Empty), port, Client.Id);

			return sms;
		}

		private SmsMessage GetCancelSms(Partner performer)
		{
			if (performer == null)
				return null;
			var sms = new SmsMessage(performer.TelNum);
			sms.Text = String.Format("сч. {0} заявка отменена", Client.Id);
			return sms;
		}

		private bool ShouldSendSms(Partner performer)
		{
			if (performer == null)
				return false;
			if (String.IsNullOrEmpty(performer.TelNum))
				return false;
			return true;
		}
	}
}