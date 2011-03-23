using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InforoomInternet.Controllers;
using InforoomInternet.Models;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;
using NUnit.Framework;

namespace InforoomInternet.Test.Unit
{
	[TestFixture]
	class LoginTest
	{
		[Test]
		public void LoginFixture()
		{
			var m = new MyDialect();
			/*var t = Lease.FindAll(DetachedCriteria.For(typeof(Lease))
									.SetProjection(Projections.SqlFunction("inet_ntoa", NHibernateUtil.String,
																		   Projections.Property("Ip"))));*/
			var pc = new PhisicalClients
				{
					Name = "Петр",
					Patronymic = "Иванович",
					Password = CryptoPass.GetHashString("123")
				};
			pc.SaveAndFlush();
			new Clients
				{
					PhisicalClient = pc
				}.SaveAndFlush();
		}
	}
}
