using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;

namespace InforoomInternet.Controllers
{
	[Layout("blank")]
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class ErrorController : BaseController
	{
		public void Err404()
		{
		}
	}
}