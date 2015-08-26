using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Inforoom2.validators
{
	public class ValidatorPhone : CustomValidator
	{
		private static readonly Regex CheckPhoneFormat = new Regex(@"^(\d{3})-(\d{7})(\*\d{3})?$");

		protected override void Run(object value)
		{
			if (value is string) {
				var telephone = value as string;
				// проверка NotEmpty
				if (string.IsNullOrEmpty(telephone)) {
					AddError("Введите номер телефона");
				}
				if (!CheckPhoneFormat.Match(telephone).Success) {
					AddError("Мобильный телефон указан неверно (необходим формат xxx-xxxxxxx)");
				}
			}
		}
	}
}