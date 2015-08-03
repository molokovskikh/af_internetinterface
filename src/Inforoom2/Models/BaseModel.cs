using System;
using System.ComponentModel;
using System.Linq;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Proxy;
using NHibernate.Util;
using NHibernate.Validator.Engine;

namespace Inforoom2.Models
{
	/// <summary>
	/// Базовая модель, от которой все наследуется
	/// </summary>
	public class BaseModel
	{
		[Id(0, Name = "Id")]
		[Generator(1, Class = "native")]
		[Description("Номер")]
		public virtual int Id { get; set; }

		//todo Ну и что это за хрень? Может пора ее выпилить?
		public virtual int GetNextPriority(ISession session)
		{
			var classAttr = GetType().GetCustomAttributes(typeof(ClassAttribute), false).First() as ClassAttribute;
			var tablename = new TableNamingStrategy().TableName(classAttr.Table);
			var query = session.CreateSQLQuery("SELECT MAX(priority) AS max FROM " + tablename);
			var result = query.List();
			if (result.First() != null)
				return (int)result.First() + 1;
			return 1;
		}

		/// <summary>
		/// Меняет идентификатор модели в базе данных.
		/// !!!МЕТОД ТОЛЬКО ДЛЯ ТЕСТОВ!!!
		/// </summary>
		/// <param name="newid">Новый идентификатор</param>
		/// <param name="session">Сессия Nhibernate</param>
		/// <returns></returns>
		public virtual bool ChangeId(int newid, ISession session)
		{
			var attribute = Attribute.GetCustomAttribute(GetType(), typeof(ClassAttribute)) as ClassAttribute;
			var tablename = attribute.Table;
			var query = string.Format("UPDATE {0} SET id={1} WHERE id={2}", tablename, newid, Id);
			session.CreateSQLQuery(query).ExecuteUpdate();
			session.Flush();
			return true;
		}

		/// <summary>
		/// Метод снятия замещения типа Nhibernate.
		/// С proxy типами всегда возникают проблемы при приведении типов.
		/// </summary>
		/// <returns></returns>
		public virtual BaseModel Unproxy()
		{
			var proxy = this as INHibernateProxy;
			if (proxy == null)
				return this;

			var session = proxy.HibernateLazyInitializer.Session;
			var model = (BaseModel)session.PersistenceContext.Unproxy(proxy);
			return model;
		}

		/// <summary>
		/// Функция валидации модели
		/// </summary>
		/// <param name="session">Сессия Nhibernate</param>
		/// <returns></returns>
		public virtual InvalidValue[] Validate(ISession session)
		{
			return new InvalidValue[0];
		}

		/// <summary>
		///  Получение имени таблицы
		/// </summary>
		/// <returns>имя таблицы</returns>
		public virtual string GetTableName()
		{
			var strategy = new TableNamingStrategy();
			var attribute = Attribute.GetCustomAttribute(this.GetType(), typeof(ClassAttribute)) as ClassAttribute;
			if (attribute != null) {
				return strategy.TableName(attribute.Table);
			}
			return "No name!";
		}

		/// <summary>
		/// Безопасный метод снятия замещения типов с объектов Nhibernate. Не выдает ошибок, даже если там null.
		/// С proxy типами всегда возникают проблемы при приведении типов.
		/// </summary>
		/// <param name="model">Модель,дял которой нужно убрать Proxy</param>
		public static BaseModel UnproxyOrDefault(BaseModel model)
		{
			if (model == null)
				return null;
			return model.Unproxy();
		}
	}
}