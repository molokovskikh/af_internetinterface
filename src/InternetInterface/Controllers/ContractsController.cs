using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class ContractsController : BaseController
	{
		public void Index([SmartBinder] ContractFilter filter)
		{
			PropertyBag["filter"] = filter;
			PropertyBag["contracts"] = filter.Find(DbSession);
			PropertyBag["printers"] = Printer.All();
		}

		public void Print(uint id)
		{
			LayoutName = "print";
			PropertyBag["contract"] = DbSession.Load<Contract>(id);
		}
	}
}