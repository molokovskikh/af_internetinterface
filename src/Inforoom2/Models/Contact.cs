using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Web;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using Inforoom2.validators;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "Contacts", NameType = typeof(Contact)), Description("Контакты")]
	public class Contact : BaseModel, ILogAppeal
	{
		[ManyToOne]
		public virtual Client Client { get; set; }

		[Property, Description("Тип контакта")]
		public virtual ContactType Type { get; set; }

		//[Property(Column = "Contact"), NotNullNotEmpty(Message = "Введите номер телефона"), Pattern(@"^\d{10}$", RegexOptions.Compiled, Message = "Номер телефона введен неправильно")]
		[Property(Column = "Contact"), Description("значение"), NotNullNotEmpty(Message = "Введите контакт")]
		//Введите контакт
		public virtual string ContactString { get; set; }

		[Property, Description("Комментарий")]
		public virtual string Comment { get; set; }

		[Property, Description("Наименование")]
		public virtual string ContactName { get; set; }

		[Property, Description("Дата")]
		public virtual DateTime Date { get; set; }

		public virtual Client GetAppealClient(ISession session)
		{
			return this.Client;
		}

		[ManyToOne(Column = "Registrator", Cascade = "save-update")]
		public virtual Employee WhoRegistered { get; set; }

		public virtual List<string> GetAppealFields()
		{
			return new List<string>() {
				"ContactString",
				"Comment",
				"ContactName",
				"Type"
			};
		}

		//Формат номера для вывода
		public virtual string ContactPhoneSplitFormat
		{
			get
			{
				return (Type == ContactType.ConnectedPhone || Type == ContactType.HeadPhone || Type == ContactType.HousePhone ||
				        Type == ContactType.FinancePhone
				        || Type == ContactType.MobilePhone || Type == ContactType.SmsSending || Type == ContactType.TechPhone)
				       && ContactString != null && ContactString.IndexOf("-") == -1 && ContactString.Length > 4
					? ContactString.Insert(3, "-").ToString()
					: ContactString;
			}
		}

		public virtual string ContactFormatString
		{
			set
			{
				if ((Type == ContactType.ConnectedPhone || Type == ContactType.HeadPhone || Type == ContactType.HousePhone ||
				     Type == ContactType.FinancePhone
				     || Type == ContactType.MobilePhone || Type == ContactType.SmsSending || Type == ContactType.TechPhone)
				    && ContactString != null && value.IndexOf("-") == -1 && value.Length > 4) {
					ContactString = value.Insert(3, "-").ToString();
				}
				else {
					ContactString = value;
				}
			}
			get
			{
				return (Type == ContactType.ConnectedPhone || Type == ContactType.HeadPhone || Type == ContactType.HousePhone ||
				        Type == ContactType.FinancePhone
				        || Type == ContactType.MobilePhone || Type == ContactType.SmsSending || Type == ContactType.TechPhone) &&
				       ContactString != null
					? ContactString.Replace("-", "")
					: ContactString;
			}
		}

		public virtual void ClientNotificationEmailConfirmationGet(string url)
		{
			var contact = this;
			if (contact.Client != null && contact.Type == ContactType.NotificationEmailRaw) {
				var urlToConfirm = Md5.GetHash(contact.ContactString).Substring(0, 10) + Md5.GetHash(contact.Client.Id.ToString("D7")).Substring(0, 10);
				urlToConfirm = urlToConfirm.Replace(" ", "").Replace("/", "").Substring(0, 14).ToLower();
				urlToConfirm = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2Url"] + url + "/" + HttpUtility.HtmlEncode(urlToConfirm);
				EmailSender.SendEmail(contact.ContactString, "Подтверждение адреса для рассылки уведомлений",
					$"Для подтверждения адреса рассылки уведомлений от провайдера Инфорум,<br/> просьба перейти по <a href='{urlToConfirm}'>ссылке</a>.");
			}
		}

		public virtual void ClientNotificationEmailConfirmationSet(string key)
		{
			var contact = this;
			if (contact.Client != null && contact.Type == ContactType.NotificationEmailRaw && !string.IsNullOrEmpty(contact.ContactString)) {
				var keyToConfirm = Md5.GetHash(contact.ContactString).Substring(0, 10) + Md5.GetHash(contact.Client.Id.ToString("D7")).Substring(0, 10);
				keyToConfirm = keyToConfirm.Replace(" ", "").Replace("/", "").Substring(0, 14).ToLower();
				if (keyToConfirm == key) {
					contact.Type = ContactType.NotificationEmailConfirmed;
				}
			}
		}

		public virtual void ClientNotificationEmailRestore()
		{
			var contact = this;
			if (contact.Client != null && contact.Type == ContactType.NotificationEmailConfirmed) {
				contact.Type = ContactType.NotificationEmailRaw;
			}
		}

		public virtual string GetAdditionalAppealInfo(string property, object oldPropertyValue, ISession session)
		{
			return "";
		}
	}

	public enum ContactType
	{
		[Description("Мобильный номер")] MobilePhone = 0,

		[Description("Домашний номер")] HousePhone = 1,

		[Description("Связанный номер")] ConnectedPhone = 2,

		[Description("Email")] Email = 3,

		[Description("Финансовые вопросы")] FinancePhone = 4,

		[Description("Главный телефон")] HeadPhone = 5,

		[Description("Телефон для смс рассылки")] SmsSending = 6,

		[Description("Техническая информация")] TechPhone = 7,

		[Description("Email для рассылки уведомлений (не подтвержденный)")] NotificationEmailRaw = 8,

		[Description("Email для рассылки уведомлений (подтвержденный)")] NotificationEmailConfirmed = 9
	}
}