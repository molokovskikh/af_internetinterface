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
			Editors.Add(typeof(DateTime?), (name, value, options) => {
				var dateTime = (DateTime?)value;

				if (dateTime == null)
					return GetEdit(name, typeof(int), string.Empty, options);

				return helper.TextFieldValue(name, dateTime.Value.ToShortDateString(), options as IDictionary);
			});
		}

		public override bool HavePermission(string controller, string action)
		{
			return false;
		}
	}
}