using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{

    //[Layout("Main")]
    [FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
    public class AgentInfoController : SmartDispatcherController 
    {
        private List<PaymentsForAgent> GetPayments(DateTime startDate, DateTime endDate)
        {
            return PaymentsForAgent.Queryable.Where(
                p =>
                p.Agent == InitializeContent.partner && p.RegistrationDate >= startDate &&
                p.RegistrationDate <= endDate).ToList();
        }

        public virtual void SummaryInformation()
        {
            var endDate = DateTime.Now;
            var startDate = new DateTime(endDate.Year, endDate.Month, 1);
            SummaryInformation(startDate, endDate);
        }

        public virtual void SummaryInformation(DateTime startDate, DateTime endDate)
        {
            var payments = GetPayments(startDate, endDate);
            PropertyBag["Payments"] = payments;
            PropertyBag["startDate"] = startDate.ToShortDateString();
            PropertyBag["endDate"] = endDate.ToShortDateString();
            PropertyBag["TotalSum"] = payments.Sum(p => p.Sum);
        }

        public virtual void GroupInfo()
        {
            PropertyBag["Agents"] = Partner.Queryable.Where(p => p.Categorie.ReductionName == "Agent").ToList();
        }
    }
}