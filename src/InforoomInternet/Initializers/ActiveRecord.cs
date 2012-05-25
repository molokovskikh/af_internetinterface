using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Models.Editor;
using InternetInterface.Models;
using NHibernate.Engine;
using NHibernate.Type;
using Environment=NHibernate.Cfg.Environment;

namespace InforoomInternet.Initializers
{
	public class ActiveRecord : ActiveRecordInitializer
	{
		public ActiveRecord()
		{
			Assemblies = new [] { "InforoomInternet", "InternetInterface" };
			AdditionalTypes = new[] {typeof(MenuField), typeof(SiteContent), typeof(SubMenuField)};
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

			SetupFilters();
		}

		public static void SetupFilters()
		{
			var configuration = ActiveRecordMediator
				.GetSessionFactoryHolder()
				.GetAllConfigurations()
				.First();
			configuration.FilterDefinitions.Add("HiddenTariffs",
				new FilterDefinition("HiddenTariffs", "", new Dictionary<string, IType>(), true));
			var mapping = configuration.GetClassMapping(typeof(Tariff));
			mapping.AddFilter("HiddenTariffs", "Hidden = 0");
		}
	}
}