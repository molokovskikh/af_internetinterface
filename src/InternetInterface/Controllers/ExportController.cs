using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Queries;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class ExportController : BaseController
	{
		private void PrepareExcelHeader(string fileName)
		{
			CancelLayout();
			CancelView();

			Response.Clear();
			Response.AppendHeader("Content-Disposition",
				String.Format("attachment; filename=\"{0}\"", Uri.EscapeDataString(fileName)));
			Response.ContentType = "application/vnd.ms-excel";
		}

		public void GetClientsInExcel([DataBind("filter")]SeachFilter filter)
		{
			PrepareExcelHeader("Клиенты.xls");

			var buf = ExportModel.GetClients(filter);

			Response.OutputStream.Write(buf, 0, buf.Length);
		}
	}
}