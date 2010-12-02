using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("Labels", Schema = "Internet", Lazy = true)]
	public class Label : ActiveRecordLinqBase<Label>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateIsUnique("Имя должно быть уникальным")]
		public virtual string Name { get; set; }

		[Property]
		public virtual string Color { get; set; }

	}
}
