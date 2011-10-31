using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using InforoomInternet.Logic;

namespace InforoomInternet.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	public class ContentController : BaseContentController
	{
		public override bool IsAcces()
		{
			return LoginLogic.IsAccessiblePartner(Session["LoginPartner"]);
		}
	}
}