using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Models
{
	[ActiveRecord("ServiceIterations", Schema = "internet", Lazy = true)]
	public class ServiceIteration : ActiveRecordLinqBase<ServiceIteration>
	{
		public ServiceIteration()
		{
			RegDate = DateTime.Now;
			Performer = InitializeContent.Partner;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Description { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[BelongsTo]
		public virtual Partner Performer { get; set; }

		[BelongsTo]
		public virtual ServiceRequest Request { get; set; }

		public virtual string GetDescription()
		{
			return AppealHelper.GetTransformedAppeal(Description);
		}
	}
}