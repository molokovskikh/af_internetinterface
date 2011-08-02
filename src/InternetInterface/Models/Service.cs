using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
        public virtual string HumanName { get; set; }

        [Property]
        public virtual decimal Price { get; set; }

        [Property]
        public virtual bool BlockingAll { get; set; }

        //public virtual Func<bool> CreateAppeal { get; set; }

        public static Service GetByType(Type type)
        {
            return FindAll().Where(s => s.GetType() == type).FirstOrDefault();
        }



        public virtual void Activate(ClientService CService)
        {}

        public virtual bool Diactivate(ClientService CService)
        {
            return false;
        }

        public virtual void EditClient(ClientService CService)
        {}

        public virtual void PaymentClient(ClientService CService)
        {}

        public virtual string GetParameters()
        {
            return string.Empty;
        }

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

        public virtual bool CanActivate(Client client)
        {
            return !client.ClientServices.Select(c => c.Service).Contains(this);
            //return true;
        }

        /*public virtual void CreateAppeal(string message, ClientService CService)
        {
            new Appeals {
                            Appeal = message,
                            Client = CService.Client,
                            AppealType = (int)AppealType.System,
                            Date = DateTime.Now,
                            Partner = InithializeContent.partner
                        }.Save();
        }*/
    }



    [ActiveRecord(DiscriminatorValue = "DebtWork")]
    public class DebtWork : Service
    {
        public override string GetParameters()
        {
            var builder = new StringBuilder();
            builder.Append("<tr>");
            builder.Append(
                string.Format(
                    "<td><label for=\"endDate\"> Конец периода </label><input type=text  name=\"endDate\" id=\"endDate\" value=\"{0}\" class=\"date-pick dp-applied\"></td>",
                    DateTime.Now.AddDays(1).ToShortDateString()));
            builder.Append("</tr>");
            return builder.ToString();
        }

        public override bool CanActivate(Client client)
        {
            return client.PaymentForTariff() && base.CanActivate(client) &&
                   !client.ClientServices.Select(c => c.Service).Contains(GetByType(typeof (VoluntaryBlockin)));
        }

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

        public override bool CanDelete(ClientService CService)
        {
            if (CService.Activator != null)
                return true;

            var lastPayments =
                Payment.Queryable.Where(
                    p => p.Client == CService.Client && (CService.BeginWorkDate.Value - p.PaidOn).TotalHours <= 0).
                    ToList().Sum(p => p.Sum);
            var balance = CService.Client.PhysicalClient.Balance;
            if (balance > 0 &&
                balance - lastPayments <= 0)
                return true;
            return false;
        }

        public override bool Diactivate(ClientService CService)
        {
            if ((CService.EndWorkDate.Value - SystemTime.Now()).TotalHours <= 0)
            {
                var client = CService.Client;
                client.Disabled = true;
                client.Status = Status.Find((uint)StatusType.NoWorked);
                client.UpdateAndFlush();
                return true;
                //CService.Delete();
            }
            return false;
        }

        public override void Activate(ClientService CService)
        {
            if (!CService.Activated && CanActivate(CService.Client))
            {
                var client = CService.Client;
                client.Disabled = false;
                client.RatedPeriodDate = SystemTime.Now();
                client.Status = Status.Find((uint) StatusType.Worked);
                client.Update();
                CService.Activated = true;
                CService.Update();
            }
            //CreateAppeal("Услуга \"Обещанный платеж активирована\"", CService);
        }
    }

    [ActiveRecord(DiscriminatorValue = "VoluntaryBlockin")]
    public class VoluntaryBlockin : Service
    {
        public override string GetParameters()
        {
            var builder = new StringBuilder();
            builder.Append("<tr>");
            builder.Append(
                string.Format(
                    "<td><label for=\"startDate\">Начала периода </label><input type=text value=\"{0}\" name=\"startDate\" id=\"startDate\" class=\"date-pick dp-applied\"> </td>",
                    DateTime.Now.ToShortDateString()));
            builder.Append(
                string.Format(
                    "<td><label for=\"endDate\"> Конец периода </label><input type=text  name=\"endDate\" value=\"{0}\"  id=\"endDate\" class=\"date-pick dp-applied\"></td>",
                    DateTime.Now.AddDays(1).ToShortDateString()));
            builder.Append("</tr>");
            return builder.ToString();
        }

        public override bool CanActivate(Client client)
        {
            return base.CanActivate(client) &&
                   !client.ClientServices.Select(c => c.Service).Contains(GetByType(typeof (DebtWork)));
        }

        public override void Activate(ClientService CService)
        {
            if (((SystemTime.Now().Date - CService.BeginWorkDate.Value.Date).TotalHours <= 0) && !CService.Activated)
            {
                var client = CService.Client;
                client.RatedPeriodDate = DateTime.Now;
                client.Disabled = true;
                client.AutoUnblocked = false;
                client.DebtDays = 0;
                client.Update();
                CService.Activated = true;
                //CService.BeginWorkDate = DateTime.Now;
                //CService.EndWorkDate = null;
                CService.Update();
            }
            //CreateAppeal("Услуга добровольная блокировка включена", CService);
        }

        /*public virtual void DiactivateVoluntaryBlockin(ClientService CService)
        {
            diactivate = true;
            Diactivate(CService);
        }*/

        public override bool Diactivate(ClientService CService)
        {
            if ((CService.EndWorkDate == null && CService.Client.PhysicalClient.Balance < 0) ||
                (CService.EndWorkDate != null && (SystemTime.Now().Date - CService.EndWorkDate.Value.Date).TotalHours < 0))
            {
                var client = CService.Client;
                client.DebtDays = 0;
                client.RatedPeriodDate = DateTime.Now;
                client.Disabled = CService.Client.PhysicalClient.Balance < 0;
                client.AutoUnblocked = true;
                client.Update();
                return true;
                //CService.EndWorkDate = DateTime.Now;
                //CService.Delete();
                //CreateAppeal("Услуга добровольная блокировка отключена", CService);
            }
            return false;
        }

        public override void PaymentClient(ClientService CService)
        {
            Diactivate(CService);
        }

        public override bool CanDelete(ClientService CService)
        {
            if (CService.EndWorkDate == null)
                return true;
            return (SystemTime.Now().Date - CService.EndWorkDate.Value.Date).Days < 45;
        }

        public override decimal GetPrice(ClientService CService)
        {
            if (CService.BeginWorkDate == null)
                return 0;

            if ((SystemTime.Now().Date - CService.BeginWorkDate.Value.AddMonths(1).Date).TotalDays >= 0 )
                return 0;
            return CService.Client.GetInterval()*2m;
        }
    }
}