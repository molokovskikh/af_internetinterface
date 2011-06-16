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
        public void Agent(uint id)
        {
            PropertyBag["ActionName"] = "RegisterHouseAgent";
            if (id == 0)
            {
                PropertyBag["ActionName"] = "RegisterHouseAgent";
                PropertyBag["agent"] = new HouseAgent();
            }
            else
            {
                PropertyBag["ActionName"] = "EditHouseAgent";
                PropertyBag["agent"] = HouseAgent.Find(id);
            }
            //PropertyBag["agent"] = agent;
        }

        public void RegisterHouseAgent([DataBind("agent")]HouseAgent agent)
        {
            if (Validator.IsValid(agent))
            {
                agent.Save();
                RedirectToUrl("..//HouseAgent/ShowAgents");
            }
            else
            {
                PropertyBag["agent"] = agent;
                RedirectToUrl("..//HouseAgent/Agent");
            }
        }

        public void EditHouseAgent(uint agentId)
        {
            var agent = HouseAgent.Find(agentId);
            BindObjectInstance(agent, ParamStore.Form, "agent");
            if (Validator.IsValid(agent))
            {
                agent.Update();
                RedirectToUrl("..//HouseAgent/ShowAgents");
            }
            else
            {
                PropertyBag["agent"] = agent;
                RedirectToUrl("..//HouseAgent/Agent");
            }
        }

        public void ShowAgents()
        {
            PropertyBag["agents"] = HouseAgent.FindAllSort();
        }
    }
}