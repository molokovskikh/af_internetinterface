using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Models;
using Common.Models.Repositories;
using Common.MySql;
using InforoomInternet.Controllers;
using InforoomInternet.Models;
using InternetInterface;
using InternetInterface.Models;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Criterion;
using NUnit.Framework;
using MySql;
using MySqlHelper = Common.MySql.MySqlHelper;

namespace InforoomInternet.Test.Unit
{
	[TestFixture]
	class LoginTest
	{
		[Test, Ignore]
		public void Test()
		{
			var Con = new MySqlConnection(@"Data Source=testSQL.analit.net;Database=internet;User ID=system;Password=newpass;Connect Timeout=300;pooling=true;convert zero datetime=yes;Default command timeout=300;Allow User Variables=true;");
			Con.Open();
			var h = new MySqlHelper(Con);
			h.Command(string.Format("insert into internet.PhysicalClients (Password) values (\"{0}\")", CryptoPass.GetHashString("123"))).Execute();
		}
	}
}
