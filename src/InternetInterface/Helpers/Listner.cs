using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Services.Description;
using Castle.ActiveRecord;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Helpers
{
    public class AuditablePropertyUser : AuditableProperty
    {

        public AuditablePropertyUser(PropertyInfo property, string name, object newValue, object oldValue)
            : base(property, name, newValue, oldValue)
        {
        }

        protected override void Convert(PropertyInfo property, object newValue, object oldValue)
        {
            if (oldValue == null)
            {
                OldValue = "";
            }
            else
            {
                OldValue = AsString(property, oldValue);
            }

            if (newValue == null)
            {
                NewValue = "";
            }
            else
            {
                NewValue = AsString(property, newValue);
            }
            Message = String.Format("Изменено '{0}' было '{1}' стало '{2}'", Name, OldValue, NewValue);
        }
    }

    [EventListener]
    public class Listner : BaseAuditListner
    {
        protected override AuditableProperty GetAuditableProperty(PropertyInfo property, string name, object newState, object oldState)
        {
            return new AuditablePropertyUser(
                property,
                name,
                newState,
                oldState);
        }

        protected override void Log(NHibernate.Event.PostUpdateEvent @event, string message)
        {
            var client = new Clients();
            if (@event.Entity.GetType() == typeof(Clients))
                client = (Clients)@event.Entity;
            if (@event.Entity.GetType() == typeof(PhysicalClients))
                client =
                    Clients.Queryable.Where(c => c.PhysicalClient == (PhysicalClients) @event.Entity).FirstOrDefault();
            if (@event.Entity.GetType() == typeof(LawyerPerson))
                client = Clients.Queryable.Where(c => c.LawyerPerson == (LawyerPerson) @event.Entity).FirstOrDefault();
            @event.Session.Save(new Appeals { Appeal = message, Client = client, Date = DateTime.Now, Partner = InithializeContent.partner, AppealType = (int)AppealType.System});
        }
    }
}