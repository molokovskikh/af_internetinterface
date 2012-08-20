using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	public enum AdditionalStatusType
	{
		Refused = 1,
		NotPhoned = 3,
		AppointedToTheGraph = 5,
	}


	[ActiveRecord("AdditionalStatus", Schema = "internet", Lazy = true)]
	public class AdditionalStatus : ValidActiveRecordLinqBase<AdditionalStatus>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string ShortName { get; set; }
	}
}