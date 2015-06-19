using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Audit;

namespace InternetInterface.Helpers
{
	public class RewrittenAuditableProperty : AuditableProperty
	{
		public RewrittenAuditableProperty(PropertyInfo property, string name, object newValue, object oldValue) : base(property, name, newValue, oldValue)
		{
			Property = property;
			Name = name;

			if (String.IsNullOrEmpty(name))
				Name = BindingHelper.GetDescription(property);
			Convert(property, newValue, oldValue);
		}

		protected override void Convert(PropertyInfo property, object newValue, object oldValue)
		{
			if (oldValue == null) {
				oldValue = "значение отсуствует";
			}
			else {
				if (OldValue == string.Empty) {
					OldValue = "";
				}
				OldValue = AsString(property, oldValue);
			}

			if (newValue == null) {
				NewValue = "значение отсуствует";
			}
			else {
				if (NewValue == string.Empty) {
					NewValue = "";
				}
				NewValue = AsString(property, newValue);
			}
			Message = String.Format("$$$Изменено '{0}' было '{1}' стало '{2}'", Name, OldValue, NewValue);
		}
	}
}