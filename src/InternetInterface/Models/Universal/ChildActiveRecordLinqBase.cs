using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord.Framework;
using NHibernate.Criterion;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;

namespace InternetInterface.Models
{
	public class ChildActiveRecordLinqBase<T> : ActiveRecordLinqBase<T>
	{
		public virtual string LogComment { get; set; }

		public static IList<T> FindAllSort()
		{
			return FindAll(DetachedCriteria.For(typeof(T)).AddOrder(Order.Asc("Name")));
		}

		public static T FirstOrDefault(uint id)
		{
			return (T) ActiveRecordMediator.FindByPrimaryKey(typeof (T), id);
		}
	}	
}