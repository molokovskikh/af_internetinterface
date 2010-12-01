using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("Requests", Schema = "Internet", Lazy = true)]
	public class Requests : ActiveRecordLinqBase<Requests>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string ApplicantName { get; set; }

		[Property]
		public virtual string ApplicantPhoneNumber { get; set; }

		[Property]
		public virtual string ApplicantEmail { get; set; }

		[Property]
		public virtual string ApplicantResidence { get; set; }

		[Property]
		public virtual bool SelfConnect { get; set; }

		[BelongsTo("Tariff")]
		public virtual Tariff Tariff { get; set; }

		[BelongsTo("Label")]
		public virtual Label Label { get; set; }
	}

}