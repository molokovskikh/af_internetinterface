using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Internal.EventListener;
using Common.Web.Ui.Models.Audit;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate.Event;

namespace InternetInterface.Helpers
{
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
	public class AuditListener : BaseAuditListener, IPreInsertEventListener
	{
		protected override AuditableProperty GetAuditableProperty(PropertyInfo property, string name, object newState, object oldState, object entity)
		{
			var auditableProperty = new AuditablePropertyInternet(property, name, newState, oldState);
			if (entity.GetType() == typeof(OrderService)) {
				if (!String.IsNullOrEmpty(((OrderService)entity).Description))
					auditableProperty.Message = String.Format("Услуга '{0}' {1}", ((OrderService)entity).Description, auditableProperty.Message);
			}
			return auditableProperty;
		}

		public bool OnPreInsert(PreInsertEvent @event)
		{
			var session = @event.Session;
			var entity = @event.Entity;

			if (entity is ClientEndpoint) {
				var endpoint = (ClientEndpoint)entity;
				var message = "Создана точка подключения.";
				LoadData(session, () => {
					if (endpoint.Switch != null) {
						var @switch = endpoint.Switch;
						message += " Коммутатор " + @switch.Name + " # " + @switch.Id;
					}
					var appeal = new Appeals(message, endpoint.Client, AppealType.System);
					session.Save(appeal);
				});
			}
			return false;
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
					message = String.IsNullOrEmpty(client.LogComment) ? message : String.Format("{0} \r\n Комментарий: \r\n {1}", message, client.LogComment);
					appeal = new Appeals {
						Appeal = message,
						Client = client,
						Partner = InitializeContent.TryGetPartner(),
						AppealType = AppealType.System
					};
				}
			});

			if (appeal != null)
				session.Save(appeal);
		}

		public static void RemoveAuditListener(EventListenerContributor target)
		{
			target.Remove(typeof(AuditListener));
		}
	}
}