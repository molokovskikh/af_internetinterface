using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models;
using NHibernate;

namespace InternetInterface.Helpers
{
	public class ARSesssionHelper<T>
	{
		public static void QueryWithSession(Func<ISession, IEnumerable<T>> result)
		{
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = sessionHolder.CreateSession(typeof(T));
			try
			{
				var _result = result(session);
				foreach (var item in _result)
					session.Evict(item);
			}
			finally
			{
				sessionHolder.ReleaseSession(session);
			}
		}
	}
}