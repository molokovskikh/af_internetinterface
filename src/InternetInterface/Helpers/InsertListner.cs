using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Mails;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Event;

namespace InternetInterface.Helpers
{
	[EventListener]
	public class InsertListner : IPostInsertEventListener
	{
		public void OnPostInsert(PostInsertEvent @event)
		{
			var type = @event.Persister.GetMappedClass(EntityMode.Poco);

			if (type == typeof(UserWriteOff)) {
				var item = @event.Entity as UserWriteOff;
				if (!item.Client.IsPhysical())
					LawyerUserWriteOffNotice.Send(item);
			}
		}
	}
}