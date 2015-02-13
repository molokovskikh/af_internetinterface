using System;
using System.Configuration;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Components;
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

			if (session.Transaction.IsActive) {
				//Мне кажется этот код никогда не исполнится, todo подумать и удалить
				if (filterContext.Exception != null) {
					EmailSender.SendEmail("asarychev@analit.net", "Rollback транзакции в OnResultExecuted","");
					session.Transaction.Rollback();
				}
				else
					session.Transaction.Commit();
			}

			session = CurrentSessionContext.Unbind(SessionFactory);
			if(session.IsOpen)
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
			tableName = tableName.ToLower();
			if (tableName == "PhysicalClients".ToLower()
				|| tableName == "Appeals".ToLower()
				|| tableName == "Tariffs".ToLower()
				|| tableName == "Regions".ToLower()
				|| tableName == "Services".ToLower()
				|| tableName == "ClientServices".ToLower()
				|| tableName == "Clients".ToLower()
				|| tableName == "UserWriteOffs".ToLower()
				|| tableName == "StatusCorrelation".ToLower()
				|| tableName == "Status".ToLower()
				|| tableName == "AdditionalStatus".ToLower()
				|| tableName == "LawyerPerson".ToLower()
				|| tableName == "InternetSettings".ToLower()
				|| tableName == "Leases".ToLower()
				|| tableName == "SaleSettings".ToLower()
				|| tableName == "ClientEndpoints".ToLower()
				|| tableName == "NetworkSwitches".ToLower()
				|| tableName == "PaymentForConnect".ToLower()
				|| tableName == "StaticIps".ToLower()
				|| tableName == "WriteOff".ToLower()
				|| tableName == "Requests".ToLower()
				|| tableName == "Partners".ToLower()
				|| tableName == "Payments".ToLower()
				|| tableName == "ServiceRequest".ToLower()
				|| tableName == "ConnectBrigads".ToLower()
				|| tableName == "Contacts".ToLower())
			{
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