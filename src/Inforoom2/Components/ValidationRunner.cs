using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Engine;

namespace Inforoom2.Components
{

	public class ValidationRunner
	{
		protected ArrayList ValidatedObjectList = new ArrayList();

		public InvalidValue[] ValidateDeep(object obj, IList validatedObjects = null)
		{
			ValidatedObjectList.Add(obj);

			if (validatedObjects == null)
				validatedObjects = new ArrayList();

			var runner = new ValidatorEngine();

			var summary = runner.Validate(obj);
			if (summary.Length != 0)
				return summary;
			validatedObjects.Add(obj);

			var allprops = obj.GetType().GetProperties();
			//HasMany атрибуты
			var props = allprops.Where(prop => Attribute.IsDefined(prop, typeof (OneToManyAttribute)));
			foreach (var p in props) {
				var value = p.GetValue(obj, null);
				if (!NHibernateUtil.IsInitialized(value))
					continue;
				var list = (ICollection) value;
				foreach (var o in list) {
					if (validatedObjects.Contains(o))
						continue;
					var errors = ValidateDeep(o, validatedObjects);
					if (errors.Length > 0)
						return errors;
				}
			}

			//OneToOne and Nested атрибуты
			props = allprops.Where(prop => Attribute.IsDefined(prop, typeof (OneToOneAttribute)));
			foreach (var p in props) {
				var value = p.GetValue(obj, null);
				if (!NHibernateUtil.IsInitialized(value) || validatedObjects.Contains(value))
					continue;
				var errors = ValidateDeep(value, validatedObjects);
				if (errors.Length > 0)
					return errors;
			}

			props = allprops.Where(prop => Attribute.IsDefined(prop, typeof (ManyToOneAttribute)));
			foreach (var p in props) {
				var value = p.GetValue(obj, null);
				if (value == null || !NHibernateUtil.IsInitialized(value))
					continue;
					var errors = ValidateDeep(value, validatedObjects);
					if (errors.Length > 0)
					return errors;
			}

			return summary;
		}
		
		public HtmlString GetError(object obj, string field, string message = null, string html = "")
		{
			var runner = new ValidatorEngine();

			if (!ValidatedObjectList.Contains(obj))
				return new HtmlString(string.Empty);

			var errors = runner.ValidatePropertyValue(obj, obj.GetType().GetProperty(field).Name);
			if (errors.Length > 0) {
				var ret = errors.First().Message;
				if (message != null)
					return WrapError(message);
				return WrapError(ret);
			}
			return WrapSuccess(message);
		}

		protected HtmlString WrapError(string msg)
		{
			var html = "<div class=\"error\">" + msg + "<div class=\"icon\"></div>"+ "</div>";
			var ret = new HtmlString(html);
			return ret;
		}

			protected HtmlString WrapSuccess(string msg)
		{
			var html = "<div class=\"success\">" + msg + "<div class=\"icon\"></div>"+ "</div>";
			var ret = new HtmlString(html);
			return ret;
		}

		
	}
}