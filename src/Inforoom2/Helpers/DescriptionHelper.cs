using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace Inforoom2.Helpers
{
	public static class DescriptionHelper
	{
		// Расширение для получение значения атребута "Description" 
		public static string GetDescription(this object self, string fieldName = "")
		{
			//обработка полей перечислений
			if (self as Enum != null) {
				return GetDescriptionForEnum((Enum)self);
			}
			// обработка самой модели 
			if (self != null && fieldName == string.Empty) {
				return GetDescriptionForModel(self);
			}
			if (self == null) return fieldName;
			// обработка полей модели, полей с атрибутом описания
			var property = self.GetType().GetProperty(fieldName);
			var attributes = (DescriptionAttribute[])property.GetCustomAttributes(typeof(DescriptionAttribute), false);
			return (attributes.Length > 0) ? attributes[0].Description : fieldName;
		}

		/// <summary>
		/// Получения значения из атрибута описания у модели.
		/// </summary>
		/// <param name="self">Объект с атрибутом описания.</param>
		/// <returns>Описание</returns>
		private static string GetDescriptionForModel(object self)
		{
			var modelAttributes = (DescriptionAttribute[])self.GetType().GetCustomAttributes(typeof(DescriptionAttribute), false);
			return (modelAttributes.Length > 0) ? modelAttributes[0].Description : self.GetType().Name;
		}

		/// <summary>
		/// Получения значения из атрибута описания у элемента перечисления.
		/// </summary>
		/// <param name="self">Перечисление с атрибутами описания.</param>
		/// <returns>Описание переданного элемента перечисления</returns>
		private static string GetDescriptionForEnum(Enum self)
		{
			var fieldInfoEnum = self.GetType().GetField(self.ToString());
			var attributesEnum = (DescriptionAttribute[])fieldInfoEnum.GetCustomAttributes(typeof(DescriptionAttribute), false);
			return (attributesEnum.Length > 0) ? attributesEnum[0].Description : self.ToString();
		}
	}
}