using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Inforoom2.Models;
using Contact = Inforoom2.Models.Contact;

namespace Inforoom2.validators
{
	//Валидация контактов
	internal class ValidatorContacts : CustomValidator
	{
		// перечень проверок
		private static readonly Regex CheckMailFormat = new Regex(@"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#" +
		                                                          @"$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z",
			RegexOptions.IgnoreCase);

		private static readonly Regex CheckPhoneFormat1 = new Regex(@"^(\d{3})-(\d{7})(\*\d{3})?$");
		private static readonly Regex CheckPhoneFormat2 = new Regex(@"^(\d{4})-(\d{6})(\*\d{3})?$");

		protected override void Run(object value)
		{
			// обработка контакта
			if (value is Contact) {
				var currentContact = (Contact)value;
				RunContact(currentContact);
			}
			// обработка списка
			if (value is List<Contact> || value is IList<Contact>) {
				var contactList = (IList<Contact>)value;
				if (contactList.Count==0)
				{
					AddError("<strong class='msg'>Введите номер телефона</strong>");
				}
				foreach (var currentContact in contactList) RunContact(currentContact, true);
			}
		}

		protected void RunContact(Contact currentContact, bool htmlError = false)
		{
			// проверка email
			if (currentContact.Type == ContactType.Email) {
				// проверка NotEmpty
				if (currentContact.ContactString == string.Empty) {
					if (htmlError) {
						AddError("<strong class='msg'>Введите email адрес</strong>");
					}
					else {
						AddError("Введите email адрес");
					}
					return;
				}
				if (!CheckMailFormat.Match(currentContact.ContactString).Success) {
					if (htmlError) {
						AddError("<strong class='msg'>Адрес email указан неверно</strong>");
					}
					else {
						AddError("Адрес email указан неверно");
					}
				}
			}
			// проверка мобильного телефона
			if (currentContact.Type == ContactType.MobilePhone) {
				// проверка NotEmpty
				if (currentContact.ContactString == string.Empty) {
					if (htmlError) {
						AddError("<strong class='msg'>Введите номер телефона</strong>");
					}
					else {
						AddError("Введите номер телефона");
					}
					return;
				}
				if (!CheckPhoneFormat1.Match(currentContact.ContactString).Success &&
				    !CheckPhoneFormat2.Match(currentContact.ContactString).Success) {
					if (htmlError) {
						AddError("<strong class='msg'>Мобильный телефон указан неверно.</strong> <p> Возможные форматы:<p>" +
						         "<strong><i>xxx-xxxxxxx</i></strong></p><p><strong><i>xxxx-xxxxxx</strong></i></p></p>");
					}
					else {
						AddError("Мобильный телефон указан неверно");
					}
				}
			}
			// проверка домашнего телефона
			if (currentContact.Type == ContactType.HousePhone) {
				// проверка NotEmpty
				if (currentContact.ContactString == string.Empty) {
					if (htmlError) {
						AddError("<strong class='msg'>Введите номер телефона</strong>");
					}
					else {
						AddError("Введите номер мобильного телефона");
					}
					return;
				}
				if (!CheckPhoneFormat1.Match(currentContact.ContactString).Success &&
				    !CheckPhoneFormat2.Match(currentContact.ContactString).Success) {
					if (htmlError) {
						AddError("<strong class='msg'>Домашний телефон указан неверно.</strong> <p> Возможные форматы:<p><strong>" +
						         "<i>xxx-xxxxxxx</i></strong></p><p><strong><i>xxxx-xxxxxx</strong></i></p></p>");
					}
					else {
						AddError("Домашний телефон указан неверно");
					}
				}
			}
			// проверка мобильного телефона для рассылки смс
			if (currentContact.Type == ContactType.SmsSending) {
				// проверка NotEmpty
				if (currentContact.ContactString == string.Empty) {
					if (htmlError) {
						AddError("<strong class='msg'>Введите номер телефона</strong>");
					}
					else {
						AddError("Введите номер телефона");
					}
					return;
				}
				if (!CheckPhoneFormat1.Match(currentContact.ContactString).Success &&
				    !CheckPhoneFormat2.Match(currentContact.ContactString).Success) {
					AddError("Номер телефона введен неправильно");
				}
			}
		}
	}
}