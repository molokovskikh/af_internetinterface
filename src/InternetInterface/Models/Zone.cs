using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	[ActiveRecord("NetworkZones", Schema = "internet", Lazy = true)]
	public class Zone : ValidActiveRecordLinqBase<Zone>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

	}
}