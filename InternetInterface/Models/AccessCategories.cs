using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;

namespace InternetInterface.Models
{
	[ActiveRecord("AccessCategories", Schema = "accessright", Lazy = true)]
	public class AccessCategories : ActiveRecordLinqBase<AccessCategories>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual uint Code { get; set; }

		[Property]
		public virtual string Name { get; set; }

		public static Boolean AccesPartner(Partner PartnerI, uint AccessOption)
		{
			if ((PartnerI.AcessSet & AccessOption) == AccessOption)
			{
				return true;
			}
			return false;
		}
	}

}