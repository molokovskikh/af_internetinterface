﻿using System;
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
			PropertyBag["Payers"] = Clients.Queryable.Where(p => p.WhoRegistered.Id == registrator && p.PhysicalClient != null);
		}

		public void ShowAgent(string startDate, string endDate, uint agent)
		{
			PropertyBag["agents"] = Agent.FindAll();
			PropertyBag["agentId"] = agent;
			var _startDate = DateTime.Parse(startDate);
			var _endDate = DateTime.Parse(endDate);
			var payments = Payment.Queryable.Where(t => t.Agent.Id == agent && t.PaidOn >= _startDate && t.PaidOn <= _endDate.AddHours(23).AddMinutes(59) && t.Sum != 0 && t.Client.PhysicalClient != null).ToList();
			PropertyBag["Payments"] = payments;
			PropertyBag["TotalSumm"] = payments.Sum(h => h.Sum);


		}
	}
}