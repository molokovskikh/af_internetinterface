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
using NHibernate.Event;

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

	public class AuditablePropertyIp : AuditableProperty
	{

		public AuditablePropertyIp(PropertyInfo property, string name, object newValue, object oldValue)
			: base(property, name, newValue, oldValue)
		{
		}

		protected override void Convert(PropertyInfo property, object newValue, object oldValue)
		{
			if (oldValue == null) {
				OldValue = "";
			}
			else {
				OldValue = AsString(oldValue);
			}

			if (newValue == null) {
				NewValue = "";
			}
			else {
				NewValue = AsString(newValue);
			}
			Message = String.Format("Изменено '{0}' было '{1}' стало '{2}'", Name, OldValue, NewValue);
		}

		public string AsString(object value)
		{
			return IpHelper.GetNormalIp(value.ToString());
		}
	}


	public class AuditablePropertyInternet : AuditableProperty
	{

		public AuditablePropertyInternet(PropertyInfo property, string name, object newValue, object oldValue)
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
		protected override AuditableProperty GetAuditableProperty(PropertyInfo property, string name, object newState, object oldState, object entity)
		{
			var attrs = property.GetCustomAttributes(typeof (Auditable), false);
			if (attrs.Length > 0 && ((Auditable)attrs.First()).Name == "Фиксированный IP")
			return new AuditablePropertyIp(property, name, newState, oldState);
			return new AuditablePropertyInternet(property, name, newState, oldState);
		}

		protected override void Log(PostUpdateEvent @event, string message, bool isHtml)
		{
			var session = @event.Session;
			var entity = @event.Entity;
			if (entity.GetType() == typeof(ServiceRequest)) {
				session.Save(
					new ServiceIteration {
						Description = message,
						Request = (ServiceRequest)entity
					});
				return;
			}

			Appeals appeal = null;
			LoadData(session, () => {
				Client client = null;
				if (entity.GetType() == typeof(Client))
					client = (Client)entity;

				var clientProp = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(p => p.PropertyType == typeof (Client));
				if (clientProp != null)
					client = (Client)clientProp.GetValue(entity, null);

				if (client != null) {
					appeal = new Appeals {
						Appeal = string.IsNullOrEmpty(client.LogComment) ? message : string.Format("{0} \r\n Комментарий: \r\n {1}", message, client.LogComment),
						Client = client,
						Date = DateTime.Now,
						Partner = InitializeContent.Partner,
						AppealType = AppealType.System
					};
				}
			});

			if (appeal != null)
				session.Save(appeal);
		}
	}
}