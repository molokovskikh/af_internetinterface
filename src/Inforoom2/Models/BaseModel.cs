using System.Linq;
using System.Web.Services.Configuration;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Util;
using NHibernate.Validator.Cfg.Loquacious.Impl;

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
	}
}