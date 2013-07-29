using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Models.Audit;
using NHibernate;
using NHibernate.Event;

namespace InternetInterface.Helpers
{
	[EventListener]
	public class DeleteListner : IPostDeleteEventListener
	{
		public void OnPostDelete(PostDeleteEvent @event)
		{
			var type = @event.Persister.GetMappedClass(EntityMode.Poco);
			if (IsLogged(type)) Log(@event, type);
		}

		public void Log(PostDeleteEvent @event, Type type)
		{
			var session = @event.Session;
			var entity = @event.Entity;
			var attributes = type.GetCustomAttributes(typeof(LogDelete), false);
			var senderType = ((LogDelete)attributes[0]).LoggingType;
			var constructorInfo = senderType.GetConstructor(new Type[] { });
			if (constructorInfo != null) {
				var logger = constructorInfo.Invoke(new object[] { });
				var log = logger as ILogInterface;
				if (log != null)
					BaseAuditListener.LoadData(session, () =>
						log.Log(entity));
				else {
					throw new Exception(string.Format("Класс {0} не реализует интерфейс ILogInterface", senderType.Name));
				}
			}
		}

		private bool IsLogged(Type type)
		{
			return type.GetCustomAttributes(typeof(LogDelete), false).Length > 0;
		}
	}
}