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
		}

		public void Show(uint registrator)
		{
			PropertyBag["Registrators"] = Partner.FindAll();
			PropertyBag["registrId"] = registrator;
			//var payers = Payment.Queryable.Where(p => p.Client.Id == registrator).GroupBy(g => g.Client);
			//PropertyBag["Payers"] = payers;
			PropertyBag["Payers"] = PhisicalClients.Queryable.Where(p => p.HasRegistered.Id == registrator);
		}
	}
}