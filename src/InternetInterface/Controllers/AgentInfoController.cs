using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{

    [Layout("Main")]
    [FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
    public class AgentInfoController : SmartDispatcherController 
    {
        public virtual void SummaryInformation()
        {
            PropertyBag["Payments"] =
                PaymentsForAgent.Queryable.Where(p => p.Agent == InithializeContent.partner).ToList();
        }
    }
}