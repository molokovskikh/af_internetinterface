using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;

namespace InternetInterface.Models
{
	[ActiveRecord("Partners", Schema = "accessright", Lazy = true)]
	public class Partner : ActiveRecordLinqBase<Partner>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string Email { get; set; }

		[Property]
		public virtual string TelNum { get; set; }

		[Property]
		public virtual string Adress { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[Property]
		public virtual string Pass { get; set; }

		[Property]
		public virtual string Login { get; set; }

		[Property]
		public virtual uint AcessSet  { get; set; }
	}

}