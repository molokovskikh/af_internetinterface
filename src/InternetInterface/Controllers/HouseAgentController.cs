using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{

    [Layout("Main")]
    [FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
    public class HouseAgentController : ARSmartDispatcherController
    {
        public void RegisterAgent()
        {

        }

        public void ShowAgents()
        {
            PropertyBag["agents"] = HouseAgents.FindAllSort();
        }
    }
}