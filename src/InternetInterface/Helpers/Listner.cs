using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Services.Description;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Internal.EventListener;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Helpers
{
	public class RemoverListner
	{
		public static void Make(EventListenerContributor target)
		{
			target.Remove(typeof (Listner));
		}
	}

	public class AddValidEventListner
	{
		public static void Make(EventListenerContributor target)
		{
			var config = target.Get(typeof (ValidEventListner));
			target.Add(config);
		}
	}

	public class AuditablePropertyUser : AuditableProperty
	{

		public AuditablePropertyUser(PropertyInfo property, string name, object newValue, object oldValue)
			: base(property, name, newValue, oldValue)
		{
		}

		protected override void Convert(PropertyInfo property, object newValue, object oldValue)
		{
			if (oldValue == null) {
				OldValue = "";
			}
			else {
				OldValue = AsString(property, oldValue);
			}

			if (newValue == null) {
				NewValue = "";
			}
			else {
				NewValue = AsString(property, newValue);
			}
			Message = String.Format("Изменено '{0}' было '{1}' стало '{2}'", Name, OldValue, NewValue);
		}
	}

	[EventListener]
	public class Listner : BaseAuditListener
	{
		protected override AuditableProperty GetAuditableProperty(PropertyInfo property, string name, object newState,
		                                                          object oldState)
		{
			return new AuditablePropertyUser(
				property,
				name,
				newState,
				oldState);
		}

		protected override void Log(NHibernate.Event.PostUpdateEvent @event, string message)
		{
			using (new SessionScope()) {
				var logInfo = string.Empty;
				var client = new Client();
				if (@event.Entity.GetType() == typeof (Client)) {
					client = (Client) @event.Entity;
					logInfo = ((Client) @event.Entity).LogComment;
				}
				if (@event.Entity.GetType() == typeof (PhysicalClients)) {
					client =
						Client.Queryable.Where(c => c.PhysicalClient == (PhysicalClients) @event.Entity).FirstOrDefault
							();
					logInfo = ((PhysicalClients) @event.Entity).LogComment;
				}
				if (@event.Entity.GetType() == typeof (LawyerPerson)) {
					client =
						Client.Queryable.Where(c => c.LawyerPerson == (LawyerPerson) @event.Entity).FirstOrDefault();
					logInfo = ((LawyerPerson) @event.Entity).LogComment;
				}
				if (@event.Entity.GetType() == typeof (ClientEndpoints)) {
					client = ((ClientEndpoints) @event.Entity).Client;
					logInfo = ((ClientEndpoints) @event.Entity).LogComment;
				}
				@event.Session.Save(new Appeals {
					Appeal =
				                    	string.IsNullOrEmpty(logInfo)
				                    		? message
				                    		: string.Format("{0} \r\n Комментарий: \r\n {1}", message,
				                    		                logInfo),
					Client = client,
					Date = DateTime.Now,
					Partner = InitializeContent.Partner,
					AppealType = (int) AppealType.System
				});
			}
		}
	}
}