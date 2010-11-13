using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;

namespace InternetInterface.Models
{
	[ActiveRecord("ConnectBrigads", Schema = "Internet", Lazy = true)]
	public class Brigad : ChildActiveRecordLinqBase<Brigad>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Adress { get; set; }

		[Property]
		public virtual int BrigadCount { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[BelongsTo("PartnerID")]
		public virtual Partner PartnerID { get; set; }
	}

}
