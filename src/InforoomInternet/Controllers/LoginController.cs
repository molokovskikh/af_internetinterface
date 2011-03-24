using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InforoomInternet.Logic;
using InforoomInternet.Models;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Dialect;
using NHibernate.Dialect.Function;
using NHibernate.Type;

namespace InforoomInternet.Controllers
{
	public class MyDialect: MySQL5Dialect
	{
		public MyDialect():base()
		{
			RegisterFunction("inet_ntoa", new StandardSQLFunction("inet_ntoa", NHibernateUtil.UInt32));
		}
	}

    [Layout("Main")]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class LoginController : SmartDispatcherController
	{
		public void LoginClient()
		{ }

		[AccessibleThrough(Verb.Post)]
		public void Accept(string Login, string Password)
		{
			try
			{
				var id = Convert.ToUInt32(Login);
				if (LoginLogic.IsAccessibleClient(id, Password))
				{
					Session["Login"] = Login;
					RedirectToUrl(@"..\\PrivateOffice\Index");
				}
				else
				{
					RedirectToUrl(@"..\\Login\LoginClient");
				}
			}
			catch (Exception ex)
			{
				RedirectToUrl(@"..\\Login\LoginClient");
			}
		}
	}
}