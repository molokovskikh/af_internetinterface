using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Tools;
using Common.Web.Ui.Helpers;

namespace InternetInterface.Models
{
	public enum ServiceRequestStatus
	{
		[Description("Новый")]
		New = 1,
		[Description("Закрыт")]
		Close = 3,
		[Description("Отменен")]
		Cancel = 5
	}

	[ActiveRecord("ServiceRequest", Schema = "internet", Lazy = true), Auditable]
	public class ServiceRequest
	{
		public ServiceRequest()
		{
			RegDate = DateTime.Now;
			Status = ServiceRequestStatus.New;
		}

		private ServiceRequestStatus _status;

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty, Description("Текст заявки")]
		public virtual string Description { get; set; }

		[Property,
			ValidateNonEmpty,
			ValidateRegExp(@"^\d{3}-\d{7}$", "Ошибка формата телефонного номера: мобильный телефн (***-*******)"),
			Description("Контактный телефон"),
			Auditable]
		public virtual string Contact { get; set; }

		[Property, Auditable("Статус сервисной заявки")]
		public virtual ServiceRequestStatus Status 
		{ 
			get
			{
				return _status;
			}
		
			set
			{
				if (value == ServiceRequestStatus.Close && value != _status) {
					ClosedDate = DateTime.Now;
					if (Sum != null && Sum > 0) {
						var comment = String.Format("Оказание дополнительных услуг, заявка №{0}", Id);
						Writeoff = new UserWriteOff(Client, Sum.Value, comment);
					}
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

		[BelongsTo]
		public virtual Partner Registrator { get; set; }

		[BelongsTo, Auditable("Исполнитель"), ValidateNonEmpty]
		public virtual Partner Performer { get; set; }

		[HasMany(ColumnKey = "Request", OrderBy = "RegDate", Lazy = true)]
		public virtual IList<ServiceIteration> Iterations { get; set; }

		public virtual UserWriteOff Writeoff { get; set; }

		public virtual string GetDescription()
		{
			return AppealHelper.GetTransformedAppeal(Description);
		}

		public virtual string GetMinDiscription()
		{
			if (Description != null)
				return  AppealHelper.GetTransformedAppeal(Description.Take(100).Implode(string.Empty)) + (Description.Length > 100 ? "..." : string.Empty);
			return string.Empty;
		}

		public virtual string GetStatusName()
		{
			return GetStatusName(Status);
		}

		public static string GetStatusName(ServiceRequestStatus status)
		{
			switch (status) {
				case ServiceRequestStatus.New:
					return "Новый";
				case ServiceRequestStatus.Close:
					return "Закрыт";
				case ServiceRequestStatus.Cancel:
					return "Отменен";
			}
			return string.Empty;
		}

		public static List<object> GetStatuses()
		{
			return new List<object>(Enum.GetValues(typeof(ServiceRequestStatus)).Cast<int>().Select(s => new {Id = s, Name = GetStatusName((ServiceRequestStatus)s)}));
		}

		public virtual SmsMessage GetSms()
		{
			if (Performer == null)
				return null;
			var endPoint = Client.Endpoints.FirstOrDefault();
			var port = endPoint != null ? endPoint.Port.ToString() : string.Empty;
			var sms = new SmsMessage {
				PhoneNumber = "+7" + Performer.TelNum,
				Text = "$"
			};

			if (PerformanceDate != null)
				sms.Text += " " + PerformanceDate;
			sms.Text += " " + String.Format("{0} т. {1} п. {2} сч. {3}",
				Client.GetCutAdress(),
				"+7-" + Contact.Replace("-", string.Empty), port, Client.Id);

			return sms;
		}
	}
}