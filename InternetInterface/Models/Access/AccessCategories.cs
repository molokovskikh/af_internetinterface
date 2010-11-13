using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using NHibernate.Criterion;

namespace InternetInterface.Models
{
	public enum AccessCategoriesType
	{
		GetClientInfo = 1,
		RegisterClient = 2,
		SendDemand = 3,
		CloseDemand = 4,
		RegisterPartner = 5,
		ChangeBalance = 6
	};

	[ActiveRecord("AccessCategories", Schema = "internet", Lazy = true)]
	public class AccessCategories : ChildActiveRecordLinqBase<AccessCategories>
	{
		[PrimaryKey]
		public virtual int Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string ReduceName { get; set; }

		/*public static IList<T> FindAllSort<T>()
		{
			return T.FindAll(DetachedCriteria.For(typeof (T)).AddOrder(Order.Asc("Name")))
		}*/
	}
}