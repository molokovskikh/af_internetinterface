using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

		public ValidationErrors()
		{
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
		protected Dictionary<object, ValidationErrors> Errors = new Dictionary<object, ValidationErrors>();
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

		/// <summary>
		/// Валидация по заданному атрибуту
		/// </summary>
		/// <typeparam name="T">Тип модели, чей параметр мы проверяем</typeparam>
		/// <param name="obj">Модель</param>
		/// <param name="instableProperty">Параметр с вероятной ошибкой (для отображения ошибки)</param>
		/// <param name="customValidatorAttribute">Атрибут (от CustomValidator), на основе которого проводится валидация</param>
		/// <param name="validateJustModel">валидация только модели модели</param>
		/// <returns>Перечень ошибок</returns>
		public ValidationErrors ForcedValidationByAttribute<T>(object obj, Expression<Func<T, object>> instableProperty,
			object customValidatorAttribute, bool validateJustModel = true)
		{
			var summary = new List<InvalidValue>();

			var member = instableProperty.Body as MemberExpression;
			var propertyInfo = member.Member as PropertyInfo;
			if (propertyInfo != null) {
				var attribute = customValidatorAttribute as CustomValidator;
				var errors = attribute.ModelForcedValidation((BaseModel)obj, propertyInfo, validateJustModel);
				summary.AddRange(errors);
			}
			return new ValidationErrors(summary.ToList());
		}

		/// <summary>
		///  Принудительная валидация по заданному атрибуту
		/// </summary>
		/// <param name="obj">Модель</param>
		/// <param name="instableProperty">Параметр с вероятной ошибкой (для отображения ошибки)</param>
		/// <param name="customValidatorAttribute">Атрибут (от CustomValidator), на основе которого проводится валидация</param>
		/// <param name="validateJustModel">валидация только модели модели</param>
		/// <param name="currentErrors">Существующий список ошибок</param>
		/// <returns>Перечень ошибок</returns>
		public ValidationErrors ForcedValidationByAttribute(object obj, PropertyInfo instableProperty,
			object customValidatorAttribute, bool validateJustModel = true, ValidationErrors currentErrors = null)
		{
			var summary = new List<InvalidValue>();
			if (instableProperty != null) {
				var attribute = customValidatorAttribute as CustomValidator;
				var errors = attribute.ModelForcedValidation((BaseModel)obj, instableProperty, validateJustModel);
				summary.AddRange(errors);
			}
			if (currentErrors != null) {
				return Concatinate(currentErrors, summary);
			}
			return new ValidationErrors(summary.ToList());
		}

		/// <summary>
		/// Объединяет список текущий список ошибок с другими  
		/// </summary>
		/// <param name="range">текущий список ошибок</param>
		/// <param name="errors">список ошибок</param>
		/// <returns></returns>
		public ValidationErrors Concatinate(ValidationErrors range, List<InvalidValue> errors)
		{
			if (range == null || errors == null) {
				return null;
			}
			range = new ValidationErrors(range.Where(s => !errors
				.Any(d => s.Message == d.Message && s.PropertyName == d.PropertyName && s.Entity == d.Entity)).ToList().Concat(errors));
			return range;
		}

		public ValidationErrors Validate(object obj, ValidationErrors currentErrors = null)
		{
			var summary = new List<InvalidValue>();

			var props = obj.GetType().GetProperties().Where(i => Attribute.GetCustomAttributes(i).OfType<CustomValidator>().Any()).ToList();
			foreach (var prop in props)
				summary.AddRange(ValidateProperty(obj, prop.Name));

			var runner = new ValidatorEngine();
			var runnerErrors = runner.Validate(obj);
			var selfValidateErrors = ((BaseModel)obj).Validate(Session);
			summary.AddRange(runnerErrors);
			summary.AddRange(selfValidateErrors);
			if (currentErrors!=null)
			{
				summary.AddRange(currentErrors); 
			}
			

			var returnedErrors = new ValidationErrors(summary.ToList());
            if(Errors.ContainsKey(obj))
                Errors[obj].AddRange(returnedErrors);
            else
			    Errors.Add(obj, returnedErrors);
			ValidatedObjectList.Add(obj);
			return returnedErrors;
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

		public HtmlString GetError(object obj, string field, string message = null, string html = "", bool IsValidated = false, object forcedValidationAttribute = null)
		{
			if (IsValidated) {
				return WrapSuccess(message);
			}

			if (!ValidatedObjectList.Contains(obj))
				return new HtmlString(string.Empty);

			// необходимо получить ошибоки для текущего поляS
			//	Errors[obj]
			var currentErrors = Errors.ContainsKey(obj) ? Errors[obj] : new ValidationErrors();
			var errors = new ValidationErrors(currentErrors.Where(s => s.PropertyName == field).ToList());

			// отображение ошибок по переданному валидатору, используется при принудительной проверке свойств
			if (forcedValidationAttribute != null) {
				errors.AddRange(((CustomValidator)forcedValidationAttribute).GetCurrentErrors());
			}
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