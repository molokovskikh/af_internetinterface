using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using Inforoom2.Models;
using NHibernate.Validator.Engine;

namespace Inforoom2.validators
{
	internal abstract class CustomValidator : Attribute
	{
		protected List<InvalidValue> Errors;
		protected PropertyInfo PropertyInfo;
		protected BaseModel Entity;

		protected CustomValidator()
		{
			Errors = new List<InvalidValue>();
		}

		protected void AddError(string msg)
		{
			var item = new InvalidValue(msg, Entity.GetType(), PropertyInfo.Name, PropertyInfo.GetValue(Entity, null), Entity, new Collection<object>());
			Errors.Add(item);
		}

		public InvalidValue[] Start(BaseModel obj, PropertyInfo info)
		{
			Entity = obj;
			PropertyInfo = info;
			var value = PropertyInfo.GetValue(Entity, null);
			Run(value);
			return Errors.ToArray();
		}
		 
		/// <summary>
		/// для принудительной проверки экземпляра модели по заданному атрибуту
		/// </summary>
		/// <param name="obj">Модель</param>
		/// <param name="instableProperty">Свойство, по которому выводится ошибка</param>
		/// <returns></returns>
		public InvalidValue[] ModelForcedValidation(BaseModel obj, PropertyInfo instableProperty)
		{
			PropertyInfo = instableProperty;
			Entity = obj;
			Run(obj);
			return Errors.ToArray();
		}

		public List<InvalidValue> GetCurrentErrors()
		{
			return Errors ?? new List<InvalidValue>();
		}

		protected abstract void Run(object value);
	}
}