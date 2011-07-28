using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
    public interface IServiceAction
    {
        void EditClient(Clients client);
        void PaymentClient(Clients client);
    }

    [ActiveRecord("Services", Schema = "Internet", Lazy = true, DiscriminatorColumn = "Name",
        DiscriminatorType = "String", DiscriminatorValue = "Service")]
    public class Service : ValidActiveRecordLinqBase<Service>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [Property]
        public virtual string Name { get; set; }

        [Property]
        public virtual decimal Price { get; set; }

        public virtual void Activate(Clients client)

        public virtual void EditClient(Clients client)
        {}

        public virtual void PaymentClient(Clients client)
        {}

        public static Service GetByName(string name)
        {
            return Queryable.Where(s => s.Name == name).FirstOrDefault();
        }
    }

    [ActiveRecord(DiscriminatorValue = "DebtWork")]
    public class DebtWork : Service
    {
        public override void EditClient(Clients client)
        {
            Console.WriteLine("ThisClass: DebtWork");
        }
    }

    [ActiveRecord(DiscriminatorValue = "VoluntaryBlockin")]
    public class VoluntaryBlockin : Service
    {
        public override void EditClient(Clients client)
        {
            var clientService = ClientService.Queryable.Where(c => c.Client == client && c.Service == this).FirstOrDefault();
            if ((clientService == null) && (client.Status.Type == StatusType.VoluntaryBlocking))
            {
                client.RatedPeriodDate = DateTime.Now;
                client.DebtDays = 0;
                client.Update();
                new ClientService {
                                      Client = client,
                                      BeginWorkDate = DateTime.Now,
                                      EndWorkDate = null,
                                      Service = this
                                  }.Save();
            }
            if ((clientService != null) && (client.Status.Type != StatusType.VoluntaryBlocking))
            {
                client.DebtDays = 0;
                client.RatedPeriodDate = DateTime.Now;
                client.Update();
                clientService.Delete();
            }
            //Console.WriteLine("ThisClass: VoluntaryBlockin");
        }
    }
}