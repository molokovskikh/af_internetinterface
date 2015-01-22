using System;
using System.Configuration;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Controllers;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Context;
using NHibernate.Linq;
using Configuration = NHibernate.Cfg.Configuration;

namespace Inforoom2.Helpers
{
	public class NHibernateActionFilter : ActionFilterAttribute
	{
		private ISessionFactory SessionFactory { get; set; }

		public NHibernateActionFilter()
		{
			SessionFactory = MvcApplication.SessionFactory;
		}

		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			var controller = filterContext.Controller as BaseController;
			if (!CurrentSessionContext.HasBind(SessionFactory)) {
				var session = SessionFactory.OpenSession();
				CurrentSessionContext.Bind(session);
				session.BeginTransaction();
				controller.DbSession = session;
			}
			else if (controller.DbSession == null)
				controller.DbSession = MvcApplication.SessionFactory.GetCurrentSession();
		}

		public override void OnResultExecuted(ResultExecutedContext filterContext)
		{
			var session = SessionFactory.GetCurrentSession();
			if (session == null)
				return;

			if (!session.Transaction.IsActive)
				return;

			/*	if (filterContext.Exception != null)
				session.Transaction.Rollback();
			else {*/
			session.Transaction.Commit();
			//	}
			session = CurrentSessionContext.Unbind(SessionFactory);
			session.Close();
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
			if (tableName == "PhysicalClients"
			    || tableName == "Appeals"
			    || tableName == "Tariffs"
			    || tableName == "Regions"
			    || tableName == "Services"
			    || tableName == "ClientServices"
			    || tableName == "Clients"
			    || tableName == "UserWriteOffs"
			    || tableName == "StatusCorrelation"
			    || tableName == "Status"
			    || tableName == "AdditionalStatus"
			    || tableName == "LawyerPerson"
			    || tableName == "InternetSettings"
			    || tableName == "Leases"
			    || tableName == "SaleSettings"
			    || tableName == "ClientEndpoints"
			    || tableName == "StaticIps"
			    || tableName == "WriteOff"
			    || tableName == "Requests"
			    || tableName == "Partners"
			    || tableName == "Payments"
				|| tableName == "ServiceRequest"
				|| tableName == "ConnectBrigads"
			    || tableName == "Contacts") {
				return tableName;
			}
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