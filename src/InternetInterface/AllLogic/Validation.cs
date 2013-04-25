using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.Components.Binder;
using Castle.Components.Validator;
using Castle.MonoRail.Framework;
using InternetInterface.Models;

namespace InternetInterface.AllLogic
{
	public class Validation
	{
		public static string ValidationConnectInfo(ConnectInfo info, bool register)
		{
			if (string.IsNullOrEmpty(info.Port) && !register)
				return "Введите порт";
			if (info.Switch == 0 && !register)
				return "Выберете коммутатор";
			int res;
			if (Int32.TryParse(info.Port, out res)) {
				if (res > 48 || res < 1)
					return "Порт должен быть в пределах о 1 до 48";
				if (ClientEndpoint.Queryable.Where(e => e.Port == res && e.Switch.Id == info.Switch).Count() == 0)
					return string.Empty;
				else {
					return "Такая пара порт/коммутатор уже существует";
				}
			}
			else {
				return string.Empty;
			}
		}
	}

	public class DecimalValidateBinder : DataBinder
	{
		protected override void BeforeBindingProperty(object instance, System.Reflection.PropertyInfo prop, string prefix, CompositeNode node)
		{
			dynamic child = node.GetChildNode(prop.Name);
			if (child != null) {
				if (child.GetType() != typeof(LeafNode))
					return;
				var validators = Validator.GetValidators(instance.GetType(), prop);
				var decValidator = validators.OfType<DecimalValidator>().FirstOrDefault();
				if (decValidator != null && !decValidator.IsValid(instance, child.Value)) {
					var errorSummary = GetValidationSummary(instance);
					errorSummary.RegisterErrorMessage(prop, decValidator.ErrorMessage);
				}
			}
		}
	}
}