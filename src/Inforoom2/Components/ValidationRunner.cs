using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using Castle.Components.Validator;
using Inforoom2.Models;
using Inforoom2.validators;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Engine;

namespace Inforoom2.Components
{
	public class ValidationErrors : List<InvalidValue>
	{
		public int Length
		{
			get { return this.Count; }
		}

		public ValidationErrors(IEnumerable<InvalidValue> ListOfErrors)
		{
			this.AddRange(ListOfErrors);
		}

		/// <summary>
		/// Удаление элемента из списка ошибок, появившихся в результате валидации 
		/// Пример использования RemoveErrors("Inforoom2.Models", "BirthDate")
		/// </summary>
		/// <param name="ClassName">Название класса</param>
		/// <param name="PropertyName">Название свойства</param>
		/// <returns>Список ошибок, появившихся в результате валидации</returns>
		public ValidationErrors RemoveErrors(string ClassName, string PropertyName)
		{
			this.RemoveAll(s => s.EntityType.Name.ToString() + "." + s.PropertyName == ClassName + "." + PropertyName);

			return this;
		}

		/// <summary>
		/// Удаление элементов из списка ошибок, появившихся в результате валидации 
		/// </summary>
		/// <param name="ErrorsToRemove">Строка в виде "RootEntity+"."+PropertyName"</param>
		/// <returns>Список ошибок, появившихся в результате валидации</returns>
		public ValidationErrors RemoveErrors(List<string> ErrorsToRemove)
		{
			foreach (var item in ErrorsToRemove) {
				var ElementToRemove = this.FirstOrDefault(s => s.RootEntity + "." + s.PropertyName == item);
				if (ElementToRemove != null) {
					this.Remove(ElementToRemove);
				}
			}
			return this;
		}
	}

	public class ValidationRunner
	{
		protected ArrayList ValidatedObjectList = new ArrayList();

		protected ISession Session;

		public ValidationRunner(ISession session)
		{
			Session = session;
		}


		private ValidationErrors ValidateProperty(object obj, string name)
		{
			var summary = new List<InvalidValue>();
			//Стандартная валидация
			var prop = obj.GetType().GetProperty(name);
			var runner = new ValidatorEngine();
			var errors = runner.ValidatePropertyValue(obj, prop.Name);
			summary.AddRange(errors);

			//Кастомная валидация
			var validators = Attribute.GetCustomAttributes(prop).OfType<CustomValidator>().ToList();
			foreach (var validator in validators) {
				errors = validator.Start((BaseModel)obj, prop);
				summary.AddRange(errors);
			}
			return new ValidationErrors(summary.ToList());
		}

		public ValidationErrors Validate(object obj)
		{
			var summary = new List<InvalidValue>();
			ValidatedObjectList.Add(obj);

			var props = obj.GetType().GetProperties().Where(i => Attribute.GetCustomAttributes(i).OfType<CustomValidator>().Any()).ToList();
			foreach (var prop in props)
				summary.AddRange(ValidateProperty(obj, prop.Name));

			var runner = new ValidatorEngine();
			var runnerErrors = runner.Validate(obj);
			var selfValidateErrors = ((BaseModel)obj).Validate(Session);
			summary.AddRange(runnerErrors);
			summary.AddRange(selfValidateErrors);

			return new ValidationErrors(summary.ToList());
		}

		public ValidationErrors ValidateDeep(object obj, IList validatedObjects = null)
		{ 

			if (validatedObjects == null)
				validatedObjects = new ArrayList();

			var summary = Validate(obj);

			validatedObjects.Add(obj);

			var allprops = obj.GetType().GetProperties();
			//HasMany атрибуты
			var props = allprops.Where(prop => Attribute.IsDefined(prop, typeof(OneToManyAttribute)));
			foreach (var p in props) {
				var value = p.GetValue(obj, null);
				if (!NHibernateUtil.IsInitialized(value))
					continue;
				var list = (ICollection)value;
				foreach (var o in list) {
					if (validatedObjects.Contains(o))
						continue;
					var errors = ValidateDeep(o, validatedObjects);
					if (errors.Length > 0)
						summary.AddRange(errors);
				}
			}

			//OneToOne and Nested атрибуты
			props = allprops.Where(prop => Attribute.IsDefined(prop, typeof(OneToOneAttribute)));
			foreach (var p in props) {
				var value = p.GetValue(obj, null);
				//Если поля нет, но оно необязательное, то и хрен с ним
				if (value == null && !Attribute.IsDefined(p, typeof(NHibernate.Validator.Constraints.NotNullAttribute)))
					continue;
				if (!NHibernateUtil.IsInitialized(value) || validatedObjects.Contains(value))
					continue;
				var errors = ValidateDeep(value, validatedObjects);
				if (errors.Length > 0)
					summary.AddRange(errors);
			}

			props = allprops.Where(prop => Attribute.IsDefined(prop, typeof(ManyToOneAttribute)));
			foreach (var p in props) {
				var value = p.GetValue(obj, null);
				if (value == null || !NHibernateUtil.IsInitialized(value))
					continue;
				var errors = ValidateDeep(value, validatedObjects);
				if (errors.Length > 0)
					summary.AddRange(errors);
			}

			return summary;
		}

		public HtmlString GetError(object obj, string field, string message = null, string html = "", bool IsValidated = false)
		{
			if (IsValidated) {
				return WrapSuccess(message);
			}

			if (!ValidatedObjectList.Contains(obj))
				return new HtmlString(string.Empty);

			var errors = ValidateProperty(obj, obj.GetType().GetProperty(field).Name);
			if (errors.Length > 0) {
				var ret = errors.First().Message;
				if (message != null)
					return WrapError(message, ret);
				return WrapError(ret);
			}
			return WrapSuccess(message);
		}

		protected HtmlString WrapError(string element, string msg = "")
		{
			var html = "<div class=\"error\">" + element + "<div class=\"msg\">" + msg + "</div><div class=\"icon\"></div>" + "</div>";
			var ret = new HtmlString(html);
			return ret;
		}

		protected HtmlString WrapSuccess(string msg)
		{
			var html = "<div class=\"success\">" + msg + "<div class=\"icon\"></div>" + "</div>";
			var ret = new HtmlString(html);
			return ret;
		}
	}
}