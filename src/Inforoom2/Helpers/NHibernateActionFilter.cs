using System;
using System.Configuration;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Controllers;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using Configuration = NHibernate.Cfg.Configuration;

namespace Inforoom2.Helpers
{
	public class NHibernateActionFilter : ActionFilterAttribute
	{
		public static readonly ISessionFactory sessionFactory = BuildSessionFactory();

		public static ISession CurrentSession
		{
			get { return HttpContext.Current.Items["NHibernateSession"] as ISession; }
			set { HttpContext.Current.Items["NHibernateSession"] = value; }
		}

		private static ISessionFactory BuildSessionFactory()
		{
			Configuration configuration = new Configuration();
			configuration.SetNamingStrategy(new TableNamingStrategy());
			var nhibernateConnectionString = ConfigurationManager.AppSettings["nhibernateConnectionString"];
			configuration.SetProperty("connection.provider", "NHibernate.Connection.DriverConnectionProvider")
				.SetProperty("connection.driver_class", "NHibernate.Driver.MySqlDataDriver")
				.SetProperty("connection.connection_string", nhibernateConnectionString)
				.SetProperty("dialect", "NHibernate.Dialect.MySQL5Dialect");
			/*	var configurationPath = HttpContext.Current.Server.MapPath(@"~\Nhibernate\hibernate.cfg.xml");
			configuration.Configure(configurationPath);*/
			configuration.AddInputStream(
				NHibernate.Mapping.Attributes.HbmSerializer.Default.Serialize(Assembly.GetExecutingAssembly()));

		/*	var schema = new NHibernate.Tool.hbm2ddl.SchemaExport(configuration);
			schema.Create(false, true);
*/
			return configuration.BuildSessionFactory();
		}


		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			var controller = filterContext.Controller as BaseController;
			controller.DbSession = CurrentSession = sessionFactory.OpenSession();
			CurrentSession.BeginTransaction();
		}

		public override void OnActionExecuted(ActionExecutedContext filterContext)
		{
			var session = CurrentSession;
			if (session == null)
				return;

			if (!session.Transaction.IsActive)
				return;

			if (filterContext.Exception != null)
				session.Transaction.Rollback();
			else
				session.Transaction.Commit();
		}
	}

	public class TableNamingStrategy : INamingStrategy
	{
		public string ClassToTableName(string className)
		{
			return className;
		}

		public string PropertyToColumnName(string propertyName)
		{
			return propertyName;
		}

		public string TableName(string tableName)
		{
			return "inforoom2_" + tableName;
		}

		public string ColumnName(string columnName)
		{
			return columnName;
		}

		public string PropertyToTableName(string className, string propertyName)
		{
			throw new NotImplementedException();
		}

		public string LogicalColumnName(string columnName, string propertyName)
		{
			throw new NotImplementedException();
		}
	}
}