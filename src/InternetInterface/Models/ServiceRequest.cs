using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Models
{
	public enum ServiceRequestStatus
	{
		[Description("Новый")]
		New = 1,
		[Description("Закрыт")]
		Close = 3,
		[Description("Ожидает")]
		Waiting = 5,
		[Description("Заблокирован")]
		Block = 7
	}

	[ActiveRecord("ServiceRequest", Schema = "internet", Lazy = true), Auditable]
	public class ServiceRequest : ActiveRecordLinqBase<ServiceRequest>
	{
		public ServiceRequest()
		{
			RegDate = DateTime.Now;
			Registrator = InitializeContent.Partner;
			Status = ServiceRequestStatus.New;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Description { get; set; }

		[Property, Auditable("Контактный телефон")]
		public virtual string Contact { get; set; }

		[Property, Auditable("Статус")]
		public virtual ServiceRequestStatus Status { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[Property]
		public virtual DateTime? ClosedDate { get; set; }

		[Property, Auditable("Дата выполнения заявки")]
		public virtual DateTime? PerformanceDate { get; set; }

		[Property, Auditable("Сумма за предоставленные услуги")]
		public virtual decimal? Sum { get; set; }

		[BelongsTo]
		public virtual Partner Registrator { get; set; }

		[BelongsTo, Auditable("Исполнитель")]
		public virtual Partner Performer { get; set; }

		[HasMany(ColumnKey = "Request", OrderBy = "RegDate", Lazy = true)]
		public virtual IList<ServiceIteration> Iterations { get; set; }

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
				case ServiceRequestStatus.Block:
					return "Заброкирован";
				case ServiceRequestStatus.Waiting:
					return "Ожидает";
			}
			return string.Empty;
		}
	}
}