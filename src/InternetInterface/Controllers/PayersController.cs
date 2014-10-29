using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Services.Description;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using NHibernate.Linq;
using NHibernate.Util;
using BankPayment = InternetInterface.Models.BankPayment;

namespace InternetInterface.Controllers
{
	[Helper(typeof(PaginatorHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class PayersController : BaseController
	{
		public void Filter()
		{
			var partners = Partner.All(DbSession);
			PropertyBag["Registrators"] = partners;
			PropertyBag["registrId"] = partners.First().Id;
		}

		public void AgentFilter([SmartBinder] AgentFilter filter)
		{
			filter.CurrentAgent = Partner.GetInitPartner();
			filter.CurrentPartner = InitializeContent.Partner;

			PropertyBag["filter"] = filter;
			if (Request.QueryString.AllKeys.Any(k => k.StartsWith("filter")))
				PropertyBag["Payments"] = filter.Find(DbSession);
		}

		public void Show(uint registrator)
		{
			PropertyBag["Registrators"] = Partner.All(DbSession);
			PropertyBag["registrId"] = registrator;
			PropertyBag["Payers"] = DbSession.Query<Client>().Where(p => p.WhoRegistered.Id == registrator && p.PhysicalClient != null);
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