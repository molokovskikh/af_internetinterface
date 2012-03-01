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
		public virtual int Number { get; set; }

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
			/*var notNullReq = Request.Queryable.Where(
				r => r.House != string.Empty && r.Apartment != string.Empty).ToList();*/
			return Request.Queryable.Where(r =>
									r.Street == House.Street && r.House == House.Number &&
									r.CaseHouse == House.Case && r.Apartment == Number &&
									r.Registrator != null).FirstOrDefault();
		}
	}
}