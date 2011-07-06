using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
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
				var info = new ProcessStartInfo("mysqld", "--standalone")
				{
					UseShellExecute = false,
					CreateNoWindow = true
				};
				mysql = Process.Start(info);
				var done = false;
				MainBilling.InitActiveRecord();
				while (!done)
				{
					try
					{
						using (var connection = new MySqlConnection("server=localhost;user=root;"))
						{
							connection.Open();
							//new MySqlCommand("drop database if exists logs;create database logs;", connection).ExecuteNonQuery();
							new MySqlCommand("drop database if exists Internet;create database Internet;", connection).ExecuteNonQuery();
							ActiveRecordStarter.DropSchema();
							done = true;
						}
						scope = new SessionScope(FlushAction.Never);
					}
					catch (MySqlException e)
					{
						if (e.Message != "Unable to connect to any of the specified MySQL hosts.")
							throw;
					}
				}
				ActiveRecordStarter.CreateSchema();
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
            scope.Dispose();
            scope = null;
			if (mysql != null)
				mysql.Kill();
			if (scope == null)
				return;
			SystemTime.Reset();
		}
	}
}
