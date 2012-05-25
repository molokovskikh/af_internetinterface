using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.MonoRailExtentions;
using NHibernate.Cfg;

namespace InternetInterface.Initializers
{
	public class ActiveRecord : ActiveRecordInitializer
	{
		public ActiveRecord()
		{
			Assemblies = new [] { "InternetInterface" };
			AdditionalTypes = new[] {typeof (ValidEventListner)};
		}

		public override void Initialize(IConfigurationSource config)
		{
			var newConfig = new InPlaceConfigurationSource();
			newConfig.IsRunningInWebApp = true;
			newConfig.PluralizeTableNames = true;
			newConfig.Add(typeof(ActiveRecordBase),
				new Dictionary<string, string> {
					{Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
					{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
					{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
					{Environment.ConnectionStringName, ConnectionHelper.GetConnectionName()},
					{Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle"},
					{Environment.Hbm2ddlKeyWords, "none"},
				});

			base.Initialize(newConfig);
		}
	}
}