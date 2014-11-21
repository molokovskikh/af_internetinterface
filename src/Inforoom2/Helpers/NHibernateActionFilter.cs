﻿using System;
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
			if (!CurrentSessionContext.HasBind(SessionFactory)) {
				var session = SessionFactory.OpenSession();
				CurrentSessionContext.Bind(session);
				session.BeginTransaction();
			}
		}
		
		public override void OnResultExecuted(ResultExecutedContext filterContext)
		{
			var session = SessionFactory.GetCurrentSession();
			if (session == null)
				return;

			if (!session.Transaction.IsActive)
				return;

			if (filterContext.Exception != null)
				session.Transaction.Rollback();
			else
				try {
					session.Transaction.Commit();
				}
				catch (Exception e) {
					(filterContext.Controller as BaseController).ErrorMessage("Не удалось сохранить транзакцию.");
					session.Transaction.Rollback();
				}
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