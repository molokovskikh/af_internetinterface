using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using InternetInterface.Models;
using Contact = Inforoom2.Models.Contact;

namespace Inforoom2.validators
{
	//Валидация контактов
	class ValidatorContacts : CustomValidator
	{
		// перечень проверок
		private static readonly Regex CheckMailFormat = new Regex(@"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#" +
						@"$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z",
						RegexOptions.IgnoreCase); 
		private static readonly Regex CheckPhoneFormat1 = new Regex(@"^(\d{3})-(\d{7})(\*\d{3})?$");
		private static readonly Regex CheckPhoneFormat2 = new Regex(@"^(\d{4})-(\d{6})(\*\d{3})?$"); 

		protected override void Run(object value)
		{  

			if (value is List<Contact>) {

				var contactList= (List<Contact>)value;
				foreach (var currentContact in contactList)
				{
				// проверка email
				if (currentContact.Type == ContactType.Email) {
					if (!CheckMailFormat.Match(currentContact.ContactString).Success)
					{
						AddError("<strong class='msg'>Адрес email указан неверно</strong>");
					}
				}
				// проверка мобильного телефона
				if (currentContact.Type == ContactType.MobilePhone)
				{
					if (!CheckPhoneFormat1.Match(currentContact.ContactString).Success &&
						!CheckPhoneFormat2.Match(currentContact.ContactString).Success)
					{
						AddError("<strong class='msg'>Мобильный телефон указан неверно.</strong> <p> Возможные форматы:<p>" + 
						         "<strong><i>xxx-xxxxxxx</i></strong></p><p><strong><i>xxxx-xxxxxx</strong></i></p></p>");
					}
				}
				// проверка домашнего телефона
				if (currentContact.Type == ContactType.HousePhone)
				{
					if (!CheckPhoneFormat1.Match(currentContact.ContactString).Success &&
						!CheckPhoneFormat2.Match(currentContact.ContactString).Success)
					{
						AddError("<strong class='msg'>Домашний телефон указан неверно.</strong> <p> Возможные форматы:<p><strong>" +
							"<i>xxx-xxxxxxx</i></strong></p><p><strong><i>xxxx-xxxxxx</strong></i></p></p>");
					}
				}
				}
				
			}

		}
	}
}