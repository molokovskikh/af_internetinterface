using System;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{

	[ActiveRecord("Status", Schema = "internet", Lazy = true)]
	public class Status : ValidActiveRecordLinqBase<Status>
	{

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

	}
}