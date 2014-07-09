using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Queries;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class ExportController : BaseController
	{
		public void GetClientsInExcel([DataBind("filter")] SearchFilter filter)
		{
			this.RenderFile("Клиенты.xls", ExportModel.GetClients(filter, filter.Find(DbSession)));
		}
	}
}