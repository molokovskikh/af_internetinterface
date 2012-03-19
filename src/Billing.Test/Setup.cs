using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Billing.Test.Integration;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.Tools;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace Billing.Test
{
	[SetUpFixture]
	public class Setup
	{
		private Process mysql;
		protected SessionScope scope;

		[SetUp]
		public void SutupFixture()
		{
			try
			{
				/*var info = new ProcessStartInfo("mysqld", "--standalone")
				{
					UseShellExecute = false,
					CreateNoWindow = true
				};
				mysql = Process.Start(info);*/
				var done = false;
				MainBilling.InitActiveRecord();
				while (!done)
				{
					try
					{
						using (var connection = new MySqlConnection("server=localhost;user=root;"))
						{
							connection.Open();
							done = true;
						}
					}
					catch (MySqlException e)
					{
						if (e.Message != "Unable to connect to any of the specified MySQL hosts.")
							throw;
					}
				}
				//using (new SessionScope())
				{
					MainBillingFixture.PrepareTests();
				}
			}
			catch (Exception ex)
			{
				mysql.Kill();
				mysql = null;
				throw;
			}
		}

		[TearDown]
		public void Teardown()
		{
			if (mysql != null)
				mysql.Kill();
			SystemTime.Reset();
		}
	}
}
