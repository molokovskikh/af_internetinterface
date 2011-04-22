using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class PayersController : SmartDispatcherController
	{
		public void Filter()
		{
			PropertyBag["Registrators"] = Partner.FindAll();
			PropertyBag["registrId"] = Partner.FindFirst().Id;
		}

		public void AgentFilter()
		{
			PropertyBag["agents"] = Agent.FindAll();
			PropertyBag["agentId"] = Agent.FindFirst().Id;
		}

		public void Show(uint registrator)
		{
			PropertyBag["Registrators"] = Partner.FindAll();
			PropertyBag["registrId"] = registrator;
			//var payers = Payment.Queryable.Where(p => p.Client.Id == registrator).GroupBy(g => g.Client);
			//PropertyBag["Payers"] = payers;
			PropertyBag["Payers"] = PhysicalClients.Queryable.Where(p => p.WhoRegistered.Id == registrator);
		}

		public void ShowAgent(string startDate, string endDate, uint agent)
		{
			PropertyBag["agents"] = Agent.FindAll();
			PropertyBag["agentId"] = agent;
			var _startDate = DateTime.Parse(startDate);
			var _endDate = DateTime.Parse(endDate);
			var payments = Payment.Queryable.Where(t => t.Agent.Id == agent && t.PaidOn >= _startDate && t.PaidOn <= _endDate && t.Sum != 0).ToList();
			PropertyBag["Payments"] = payments;
			PropertyBag["TotalSumm"] = payments.Sum(h => h.Sum);


		}
	}
}