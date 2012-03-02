using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
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
				p.Agent == InitializeContent.Partner && p.RegistrationDate >= startDate &&
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
			var endDate = DateTime.Now;
			var startDate = new DateTime(endDate.Year, endDate.Month, 1);
			var interval = new Week(startDate, endDate);
			var payments = PaymentsForAgent.Queryable.Where(p => p.RegistrationDate.Date >= startDate.Date && p.RegistrationDate.Date <= endDate.Date).ToList().GroupBy(p => p.Agent);
			PropertyBag["payments"] = payments;

			//PropertyBag["Agents"] = Partner.Queryable.Where(p => p.Categorie.ReductionName == "Agent").ToList();
			PropertyBag["interval"] = interval;
		}

		public virtual void GroupInfo([DataBind("interval")]Week interval)
		{
			var payments = PaymentsForAgent.Queryable.Where(p => p.RegistrationDate.Date >= interval.StartDate.Date && p.RegistrationDate.Date <= interval.EndDate.Date).ToList().GroupBy(p => p.Agent);
			PropertyBag["payments"] = payments;
			PropertyBag["interval"] = interval;
		}

		public virtual void EditAgentSettings()
		{}
	}
}