using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using InternetInterface.Models;
using Contact = Inforoom2.Models.Contact;

namespace Inforoom2.validators
{
	//Валидация контактов
	class ValidatorEmail : CustomValidator
	{
		// перечень проверок
		private static readonly Regex CheckMailFormat = new Regex(@"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#" +
						@"$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z",
						RegexOptions.IgnoreCase);  

		protected override void Run(object value)
		{  

			if (value is string) {

				if (!CheckMailFormat.Match(value as string).Success)
				{
					AddError("<strong class='msg'>Адрес email указан неверно</strong>");
				}
				
			}

		}
	}
}