using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Services.Description;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Internal.EventListener;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Audit;
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
			target.Remove(typeof(Listner));
		}
	}

	public class AddValidEventListner
	{
		public static void Make(EventListenerContributor target)
		{
			var config = target.Get(typeof(ValidEventListner));
			target.Add(config);
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
			base.Convert(property, newValue, oldValue);
			Message = Message.Replace("$$$", string.Empty);
		}
	}

	[EventListener]
	public class Listner : BaseAuditListener
	{
		protected override AuditableProperty GetAuditableProperty(PropertyInfo property, string name, object newState, object oldState, object entity)
		{
			var auditableProperty = new AuditablePropertyInternet(property, name, newState, oldState);
			if (entity.GetType() == typeof(OrderService)) {
				if (!string.IsNullOrEmpty(((OrderService)entity).Description))
					auditableProperty.Message = auditableProperty.Message.Insert(16, ((OrderService)entity).Description);
			}
			return auditableProperty;
		}

		protected override void Log(PostUpdateEvent @event, IEnumerable<AuditableProperty> properties, bool isHtml)
		{
			var message = BuildMessage(properties);
			var session = @event.Session;
			var entity = @event.Entity;
			if (entity.GetType() == typeof(ServiceRequest)) {
				var serviceRequest = (ServiceRequest)entity;
				session.Save(
					new ServiceIteration {
						Description = message,
						Request = serviceRequest
					});
				return;
			}

			Appeals appeal = null;
			LoadData(session, () => {
				Client client = null;
				if (entity.GetType() == typeof(Client))
					client = (Client)entity;

				if (entity.GetType() == typeof(OrderService))
					client = ((OrderService)entity).Order.Client;

				var clientProp = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(p => p.PropertyType == typeof(Client));
				if (clientProp != null)
					client = (Client)clientProp.GetValue(entity, null);

				if (client != null) {
					message = string.IsNullOrEmpty(client.LogComment) ? message : string.Format("{0} \r\n Комментарий: \r\n {1}", message, client.LogComment);
					appeal = new Appeals {
						Appeal = message,
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