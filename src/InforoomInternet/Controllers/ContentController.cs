using System;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;

namespace InforoomInternet.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class ContentController : BaseContentController
	{
		public override bool IsAcces()
		{
			return Context.Items["Partner"] != null;
		}
	}
}