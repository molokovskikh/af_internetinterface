using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Inforoom2.Intefaces;
using Inforoom2.validators;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "Contacts", NameType = typeof (Contact)), Description("Контакты")]
	public class Contact : BaseModel, ILogAppeal
	{
		[ManyToOne]
		public virtual Client Client { get; set; }

		[Property, Description("Тип контакта")]
		public virtual ContactType Type { get; set; }

		//[Property(Column = "Contact"), NotNullNotEmpty(Message = "Введите номер телефона"), Pattern(@"^\d{10}$", RegexOptions.Compiled, Message = "Номер телефона введен неправильно")]
		[Property(Column = "Contact"), Description("значение"), NotNullNotEmpty(Message = "Введите номер телефона")]
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

		public virtual List<string> GetAppealFields()
		{
			return new List<string>()
			{
				"ContactString",
				"Comment",
				"ContactName",
				"Date",
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

		public virtual string GetAdditionalAppealInfo(string property, object oldPropertyValue, ISession session)
		{
			return "";
		}
	}

	public enum ContactType
	{
		[Description("Мобильный номер")] MobilePhone,

		[Description("Домашний номер")] HousePhone,

		[Description("Связанный номер")] ConnectedPhone,

		[Description("EMail")] Email,

		[Description("Финансовые вопросы")] FinancePhone,

		[Description("Главный телефон")] HeadPhone,

		[Description("Телефон для смс рассылки")] SmsSending,

		[Description("техническая информация")] TechPhone,

		[Description("EMail для рассылки счетов/актов")] ActEmail
	}
}