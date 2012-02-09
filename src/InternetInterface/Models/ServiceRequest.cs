using System;
using System.Collections.Generic;
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
		New = 1,
		Close = 3,
		Waiting = 5,
		Block = 7
	}

	[ActiveRecord("ServiceRequest", Schema = "internet", Lazy = true)]
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

		[Property]
		public virtual string Contact { get; set; }

		[Property]
		public virtual ServiceRequestStatus Status { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[BelongsTo]
		public virtual Partner Registrator { get; set; }
		
		[BelongsTo]
		public virtual Partner Performer { get; set; }

		[HasMany(ColumnKey = "Request", OrderBy = "RegDate", Lazy = true)]
		public virtual IList<ServiceInteration> Iterations { get; set; }

		public virtual string GetMinDiscription()
		{
			if (Description != null)
				return  AppealHelper.GetTransformedAppeal(Description.Take(100).Implode(string.Empty)) + (Description.Length > 100 ? "..." : string.Empty);
			return string.Empty;
		}

		public virtual string GetStatusName()
		{
			switch (Status) {
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