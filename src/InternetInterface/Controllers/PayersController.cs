using System;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Services.Description;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using NHibernate.Linq;
using BankPayment = InternetInterface.Models.BankPayment;

namespace InternetInterface.Controllers
{
	public enum VirtualType
	{
		[Description("Небонусные")] NoBonus = 0,
		[Description("Бонусные")] Bonus = 1
	}

	[Helper(typeof(PaginatorHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class PayersController : BaseController
	{
		public void Filter()
		{
			PropertyBag["Registrators"] = Partner.FindAll();
			PropertyBag["registrId"] = Partner.FindFirst().Id;
		}

		public void AgentFilter([DataBind("filter")] AgentFilter filter)
		{
			var thisD = DateTime.Now;
			if (filter.startDate == null)
				filter.startDate = new DateTime(thisD.Year, thisD.Month, 1);
			if (filter.endDate == null)
				filter.endDate = DateTime.Now;
			PropertyBag["filter"] = filter;
			PropertyBag["agents"] = Agent.FindAll();
			PropertyBag["agentId"] = filter.agent;
			PropertyBag["colorId"] = 0;
		}

		public void Show(uint registrator)
		{
			PropertyBag["Registrators"] = Partner.FindAll();
			PropertyBag["registrId"] = registrator;
			PropertyBag["Payers"] = DbSession.Query<Client>().Where(p => p.WhoRegistered.Id == registrator && p.PhysicalClient != null);
		}

		public void ShowAgent([DataBind("filter")] AgentFilter filter)
		{
			PropertyBag["agents"] = Agent.FindAll();
			PropertyBag["agentId"] = filter.agent;
			var payments = filter.Find(DbSession);
			PropertyBag["filter"] = filter;
			PropertyBag["Payments"] = payments;
			PropertyBag["TotalSumm"] = filter.TotalSum;
			if (filter.startDate.Value.Month == filter.endDate.Value.Month)
				PropertyBag["colorId"] = filter.startDate.Value.Month;
			else
				PropertyBag["colorId"] = 0;
		}

		public void NewPaymets()
		{
			if (IsPost) {
				var file = Request.Files["inputfile"] as HttpPostedFile;
				if (file == null || file.ContentLength == 0)
					return;

				Session["payments"] = BankPayment.Parse(file.FileName, file.InputStream);
				RedirectToReferrer();
			}
			else {
				PropertyBag["payments"] = Session["payments"];
				RedirectToUrl(@"../Payments/ProcessPayments");
			}
		}
	}
}