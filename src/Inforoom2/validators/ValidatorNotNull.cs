using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using InternetInterface.Models;
using Contact = Inforoom2.Models.Contact;

namespace Inforoom2.validators
{
	//Проверка параметра на наличие значения
	internal class ValidatorNotNull : CustomValidator
	{
		protected override void Run(object value)
		{
			if (value == null) {
				AddError("Поле обязательно для заполнение");
			}
		}
	}
}