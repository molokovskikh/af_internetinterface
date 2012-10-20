using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace InforoomInternet.Helpers
{
	public class AppHelper : Common.Web.Ui.Helpers.AppHelper
	{
		public AppHelper()
		{
			Editors.Add(typeof(DateTime), (name, value, options) => {
				var dateTime = (DateTime)value;

				if (dateTime == null || dateTime == DateTime.MinValue) {
					var clazz = string.Empty;
					if (options != null) {
						var opt = options as IDictionary;
						clazz = GetAttributes(opt);
					}
					return string.Format("<input name=\"{0}\" id=\"{1}\" {2}/>", name, name.Replace('.', '_'), clazz);
				}

				return GetEdit(name, typeof(int), dateTime.ToShortDateString(), options);
			});
		}

		public override bool HavePermission(string controller, string action)
		{
			return false;
		}
	}
}