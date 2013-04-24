using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using NHibernate.Criterion;

namespace InternetInterface.Models
{
	[ActiveRecord("UnresolvedPhone", Schema = "telephony")]
	public class UnresolvedCall
	{
		[PrimaryKey("id")]
		public virtual ulong Id { get; set; }

		[Property("Phone")]
		public virtual string PhoneNumber { get; set; }

		public static UnresolvedCall[] LastCalls
		{
			get
			{
				var criteria = DetachedCriteria.For<UnresolvedCall>()
					//.SetProjection(Projections.Group<UnresolvedCall>(l => l.PhoneNumber))
					.AddOrder(Order.Desc("Id"))
					.SetMaxResults(5);

				return ArHelper.WithSession(s => criteria.GetExecutableCriteria(s).List<UnresolvedCall>().ToArray());
			}
		}
	}
}