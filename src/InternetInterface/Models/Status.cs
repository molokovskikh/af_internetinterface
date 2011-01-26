using System;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	public enum StatusType 
	{
		BlockedAndNoConnected = 1,
		BlockedAndConnected = 3,
		Worked = 5,
		NoWorked = 7,
	};


	[ActiveRecord("Status", Schema = "internet", Lazy = true)]
	public class Status : ValidActiveRecordLinqBase<Status>
	{

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual bool Blocked { get; set; }

	}
}