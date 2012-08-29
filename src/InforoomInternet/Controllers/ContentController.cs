using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Models.Editor;
using InforoomInternet.Logic;
using InternetInterface.Models;

namespace InforoomInternet.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	public class ContentController : BaseContentController
	{
		public override void Contextualize(IEngineContext engineContext, IControllerContext context)
		{
			base.Contextualize(engineContext, context);
			foreach (var ivrnContent in SiteContent.FindAll()) {
				DynamicActions[ivrnContent.ViewName] = new DynamicAction(IsAcces(), Lease.IsGray(engineContext.Request.UserHostAddress));
			}
		}

		public override bool IsAcces()
		{
			return LoginLogic.IsAccessiblePartner(Session["LoginPartner"]);
		}
	}
}