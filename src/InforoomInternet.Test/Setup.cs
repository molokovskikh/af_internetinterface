

using System.Collections.Generic;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using NHibernate.Cfg;
using NUnit.Framework;

namespace InforoomInternet.Test
{
	[SetUpFixture]
	public class Setup
	{
		[SetUp]
		public void SetupFixture()
		{
			if (!ActiveRecordStarter.IsInitialized)
			{
				var configuration = new InPlaceConfigurationSource();
				configuration.PluralizeTableNames = true;
				configuration.Add(typeof(ActiveRecordBase),
								  new Dictionary<string, string>
				                  	{
				                  		{Environment.Dialect, "InforoomInternet.Controllers.MyDialect, InforoomInternet"},
				                  		{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
				                  		{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
				                  		{Environment.ConnectionStringName, "DB"},
										{Environment.ProxyFactoryFactoryClass,
				                  			"NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle"
				                  			},
				                  		{Environment.Hbm2ddlKeyWords, "none"},
				                  	});
				ActiveRecordStarter.Initialize(new[] { Assembly.Load("InforoomInternet"), Assembly.Load("InforoomInternet.Test"), Assembly.Load("InternetInterface") },
											   configuration);
			}
		}
	}
}
