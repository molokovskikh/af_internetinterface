using Castle.ActiveRecord;

namespace InternetInterface.Helpers
{
	public class HiberSession<T>
	{
		public static NHibernate.ISession GetHiberSission()
		{
				var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
				return sessionHolder.CreateSession(typeof(T));
		}
	}
}