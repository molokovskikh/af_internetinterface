using System;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{

	[ActiveRecord("Agents", Schema = "internet", Lazy = true)]
	public class Agent : ValidActiveRecordLinqBase<Agent>
	{

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo("Partner")]
		public virtual Partner Partner { get; set; }

	}
}