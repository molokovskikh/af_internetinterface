using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Mvc;
using System.Web.Services.Configuration;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping;
using NHibernate.Mapping.Attributes;
using NHibernate.Mapping.ByCode.Impl;
using NHibernate.Proxy;
using NHibernate.Type;
using NHibernate.Util;
using NHibernate.Validator.Cfg.Loquacious.Impl;
using NHibernate.Validator.Engine;

namespace Inforoom2.Models
{
	/// <summary>
	/// Базовая модель, от которой все наследуется
	/// </summary>
	public class BaseModel
	{
		[Id(0,Name = "Id")]
		[Generator(1,Class = "native")]
		public virtual int Id { get; set; }

		//todo Ну и что это за хрень? Может пора ее выпилить?
		public virtual int GetNextPriority(ISession session)
		{
			var classAttr = GetType().GetCustomAttributes(typeof(ClassAttribute), false).First() as ClassAttribute;
			var tablename = new TableNamingStrategy().TableName(classAttr.Table);
			var query = session.CreateSQLQuery("SELECT MAX(priority) AS max FROM "+tablename);
			var result = query.List();
			if (result.First() != null)
				return (int)result.First() + 1;
			return 1;
		}
		///TODO (для тестов!)  Не использовать 
		public virtual bool ChangeId(int newid, ISession session)
		{
			var attribute = Attribute.GetCustomAttribute(GetType(), typeof(ClassAttribute)) as ClassAttribute;
			var tablename = attribute.Table;
			var query = string.Format("UPDATE {0} SET id={1} WHERE id={2}", tablename, newid, Id);
			session.CreateSQLQuery(query).ExecuteUpdate();
			session.Flush();
			return true;
		}

		public virtual BaseModel Unproxy()
		{
			var proxy = this as INHibernateProxy;
			if (proxy == null)
				return this;

			var session = proxy.HibernateLazyInitializer.Session;
			var model = (BaseModel) session.PersistenceContext.Unproxy(proxy);
			return model;
		}

		public virtual InvalidValue[] Validate(ISession session)
		{
			return new InvalidValue[0];
		}
	}
}