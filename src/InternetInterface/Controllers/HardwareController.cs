using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate.Linq;
using log4net;

namespace InternetInterface.Controllers
{
	[Helper(typeof(PaginatorHelper))]
	[Helper(typeof(IpHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class HardwareController : BaseController
	{
		public HardwareController()
		{
			SetARDataBinder();
		}

		public void PortInfo(uint endPoint)
		{
			var point = DbSession.Get<ClientEndpoint>(endPoint);
			var informator = HardwareHelper.GetPortInformator(point);
			if (informator != null) {
				informator.GetPortInfo(DbSession, PropertyBag, Logger, endPoint);
				RenderView(informator.ViewName);
			}
			else {
				Error("Не удалось определить механизм подключения к коммутатору. Либо у коммутатора не выставлен тип, либо такого механизма пока не существует.");
			}
		}
	}
}