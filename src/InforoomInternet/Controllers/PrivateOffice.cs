using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;

namespace InforoomInternet.Controllers
{
	[Layout("Main")]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AccessFilter))]
	public class PrivateOffice:SmartDispatcherController
	{
		public void Index()
		{
		}
	}
}