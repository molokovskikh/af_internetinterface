using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
    public class ServiceNames
    {
        public static string DebtWork
        {
            get { return "DebtWork"; }
        }

        public static string VoluntaryBlockin
        {
            get { return "DebtWork"; }
        }
    }

    [ActiveRecord("Services", Schema = "Internet", Lazy = true, DiscriminatorColumn = "Name",
        DiscriminatorType = "String", DiscriminatorValue = "Service")]
    public class Service : ValidActiveRecordLinqBase<Service>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        /*[Property]
        public virtual string Name { get; set; }*/

        [Property]
        public virtual decimal Price { get; set; }

        [Property]
        public virtual bool BlockingAll { get; set; }


        public static Service GetByType(Type type)
        {
            return FindAll().Where(s => s.GetType() == type).FirstOrDefault();
        }



        public virtual void Activate(ClientService CService)
        {}

        public virtual void Diactivate(ClientService CService)
        {}

        public virtual void EditClient(ClientService CService)
        {}

        public virtual void PaymentClient(ClientService CService)
        {}

        public virtual bool CanDelete(ClientService CService)
        {
            return true;
        }

        public virtual bool CanBlock(ClientService CService)
        {
            return true;
        }

        public virtual decimal GetPrice(ClientService CService)
        {
            return Price;
        }

        public virtual void CreateAppeal(string message, ClientService CService)
        {
            new Appeals {
                            Appeal = message,
                            Client = CService.Client,
                            AppealType = (int)AppealType.System,
                            Date = DateTime.Now,
                            Partner = InithializeContent.partner
                        }.Save();
        }
    }



    [ActiveRecord(DiscriminatorValue = "DebtWork")]
    public class DebtWork : Service
    {
        public override void PaymentClient(ClientService CService)
        {
            if (CService.Client.PhysicalClient.Balance > 0)
                Diactivate(CService);
        }

        public override bool CanBlock(ClientService CService)
        {
            if (CService.EndWorkDate == null)
                return false;
            return (CService.EndWorkDate.Value - SystemTime.Now()).TotalHours <= 0;
        }

        public override void Diactivate(ClientService CService)
        {
            if ((CService.EndWorkDate.Value - SystemTime.Now()).TotalHours <= 0)
            {
                var client = CService.Client;
                client.Disabled = true;
                client.Status = Status.Find((uint)StatusType.NoWorked);
                client.UpdateAndFlush();
                CService.Delete();
            }
        }

        public override void Activate(ClientService CService)
        {
            var client = CService.Client;
            client.Disabled = false;
            client.Status = Status.Find((uint)StatusType.Worked);
            client.Update();
            CreateAppeal("Услуга \"Обещанный платеж активирована\"", CService);
        }
    }

    [ActiveRecord(DiscriminatorValue = "VoluntaryBlockin")]
    public class VoluntaryBlockin : Service
    {
        private bool diactivate { get; set; }

        public override void Activate(ClientService CService)
        {
            var client = CService.Client;
            client.RatedPeriodDate = DateTime.Now;
            client.Disabled = true;
            client.AutoUnblocked = false;
            client.DebtDays = 0;
            client.Update();
            CService.BeginWorkDate = DateTime.Now;
            CService.EndWorkDate = null;
            CService.Update();
            CreateAppeal("Услуга добровольная блокировка включена", CService);
        }

        public virtual void DiactivateVoluntaryBlockin(ClientService CService)
        {
            diactivate = true;
            Diactivate(CService);
        }

        public override void Diactivate(ClientService CService)
        {
            if (diactivate)
            {
                var client = CService.Client;
                client.DebtDays = 0;
                client.RatedPeriodDate = DateTime.Now;
                client.Disabled = false;
                client.AutoUnblocked = true;
                client.Update();
                CService.EndWorkDate = DateTime.Now;
                CService.Update();
                CreateAppeal("Услуга добровольная блокировка отключена", CService);
            }
        }

        public override void PaymentClient(ClientService CService)
        {
            Diactivate(CService);
        }

        public override bool CanDelete(ClientService CService)
        {
            if (CService.EndWorkDate == null)
                return true;
            return (DateTime.Now - CService.EndWorkDate.Value).Days < 45;
        }

        public override decimal GetPrice(ClientService CService)
        {
            if (CService.BeginWorkDate == null)
                return 0;

            if ((SystemTime.Now() - CService.BeginWorkDate.Value).Days < 15)
                return 0;
            return Price;
        }
    }
}