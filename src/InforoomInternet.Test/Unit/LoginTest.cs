using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InforoomInternet.Controllers;
using InforoomInternet.Models;
using InternetInterface;
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
		public void Test()
		{
			var u = MenuField.FindFirst();
		}
	}
}
