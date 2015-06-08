using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace Inforoom2.Helpers
{
	public static class DescriptionHelper
	{
		// Получение значения атребута "Description"
		public static string GetDescription(this object self, string fieldName = "")
		{
			// когда передана только модель 
			if (self != null && fieldName == string.Empty) {
				var modelAttributes = (DescriptionAttribute[])self.GetType().GetCustomAttributes(typeof(DescriptionAttribute), false);
				return (modelAttributes.Length > 0) ? modelAttributes[0].Description : self.GetType().Name;
			}
			var property = self.GetType().GetProperty(fieldName);
			var attributes = (DescriptionAttribute[])property.GetCustomAttributes(typeof(DescriptionAttribute), false);
			return (attributes.Length > 0) ? attributes[0].Description : fieldName;
		}
	}
}