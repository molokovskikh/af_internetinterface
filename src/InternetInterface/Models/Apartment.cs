using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord("Apartments", Schema = "internet", Lazy = true)]
	public class Apartment : ActiveRecordLinqBase<Apartment>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual House House { get; set; }

		[Property]
		public virtual string Number { get; set; }

		[Property]
		public virtual string LastInternet { get; set; }

		[Property]
		public virtual string LastTV { get; set; }

		[Property]
		public virtual string PresentInternet { get; set; }

		[Property]
		public virtual string PresentTV { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[BelongsTo]
		public virtual ApartmentStatus Status { get; set; }

		public virtual Request GetRequestForThis()
		{
			int num = int.Parse(Number);
			return ActiveRecordLinqBase<Request>.Queryable.FirstOrDefault(r => r.Street == House.Street
				&& r.House == House.Number
				&& r.CaseHouse == House.Case && r.Apartment == num
				&& r.Registrator != null);
		}
	}
}