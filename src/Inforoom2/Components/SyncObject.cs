using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Framework.Config;
using Common.MySql;
using Inforoom2.Models;

using NHibernate;
using NHibernate.Event;
using NHibernate.Linq;
using Client = Inforoom2.Models.Client;
using Configuration = NHibernate.Cfg.Configuration;
using Environment = NHibernate.Cfg.Environment;


namespace Inforoom2.Components
{
	public class SyncObject : IPostUpdateEventListener, IPostInsertEventListener
	{
		public SyncObject()
		{
			Initialize();
			var scope = new SessionScope();
			SessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
		}

		//InternetInterface Session
		public ISession DbSession { get; set; }
		public static ISessionFactory SessionFactory;
		private ISessionFactoryHolder SessionHolder { get; set; }
		public static Configuration Configuration { get; set; }

		public static void Initialize()
		{
			if (!ActiveRecordStarter.IsInitialized) {
				BuildConfiguration();
				var holder = ActiveRecordMediator.GetSessionFactoryHolder();
				SessionFactory = holder.GetSessionFactory(typeof(ActiveRecordBase));
				Configuration = holder.GetConfiguration(typeof(ActiveRecordBase));
			}
		}


		public static void BuildConfiguration()
		{
			var nhibernateConnectionString = ConfigurationManager.AppSettings["nhibernateConnectionString"];
			var config = new InPlaceConfigurationSource();
			var properties = new Dictionary<string, string> {
				{ NHibernate.Cfg.Environment.Dialect, "NHibernate.Dialect.MySQLDialect" },
				{ NHibernate.Cfg.Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver" },
				{ NHibernate.Cfg.Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider" },
				{ NHibernate.Cfg.Environment.ConnectionString, nhibernateConnectionString },
				{ NHibernate.Cfg.Environment.Hbm2ddlKeyWords, "none" },
				{ NHibernate.Cfg.Environment.FormatSql, "true" },
				{ NHibernate.Cfg.Environment.UseSqlComments, "true" }
			};
			if (typeof(ISession).Assembly.GetName().Version < new Version(3, 3))
				properties.Add(Environment.ProxyFactoryFactoryClass,
					"NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle");

			config.Add(typeof(ActiveRecordBase), properties);
			ActiveRecordStarter.Initialize(new[] { typeof(SyncObject).Assembly }, config);
		}

		public void OnPostUpdate(PostUpdateEvent @event)
		{
			DbSession = SessionHolder.CreateSession(typeof(ActiveRecordBase));
			var type = @event.Entity.GetType().ToString();

			switch (type) {
				case "Inforoom2.Models.Client":
					
					break;
			}
			DbSession.Flush();
			SessionHolder.ReleaseSession(DbSession);
		}

		public void OnPostInsert(PostInsertEvent @event)
		{
			DbSession = SessionHolder.CreateSession(typeof(ActiveRecordBase));
			var type = @event.Entity.GetType().ToString();

			switch (type) {
				case "Inforoom2.Models.Client":
				
					break;
			}
			DbSession.Flush();
			SessionHolder.ReleaseSession(DbSession);
		}

	
	}
}