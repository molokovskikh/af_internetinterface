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
	public class ChildActiveRecordLinqBase<T> : ActiveRecordLinqBase<T> where T : ActiveRecordBase, new()
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

		/// <summary>
		/// Применять только для маленьких коллекций, медленный код!!!
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<T> FindAllAdd()
		{
			var list = ActiveRecordMediator.FindAll(typeof (T)).Cast<T>().OrderBy(e => ((dynamic)e).Name).ToList();
			if (list.Count > 0) {
				var objl = new List<T> {new T()};
				var obj = (dynamic)objl[0];
				obj.Id = 0;
				obj.Name = "Все";
				objl.AddRange(list);
				return objl;
			}
			return list;
		}
	}	
}