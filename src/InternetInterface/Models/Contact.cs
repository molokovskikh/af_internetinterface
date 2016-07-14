using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Audit;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Models
{
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

	[ActiveRecord(Table = "Contacts", Schema = "Internet", Lazy = true), Auditable]
	public class Contact
	{
		public Contact()
		{
		}

		public Contact(Partner registrator, Client client, ContactType type, string text)
			: this(client, type, text)
		{
			Registrator = registrator;
		}

		public Contact(Client client, ContactType type, string text)
		{
			Client = client;
			Type = type;
			Text = text;
			Date = DateTime.Now;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }

		[Property, Auditable("Тип")]
		public virtual ContactType Type { get; set; }

		[Property(Column = "Contact"), Auditable("Контакт")]
		public virtual string Text { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[Property, Description("ФИО")]
		public virtual string ContactName { get; set; }

		[BelongsTo]
		public virtual Partner Registrator { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		public virtual string HumanableNumber
		{
			get
			{
				if (new Regex(@"^(\d{10})$").IsMatch(Text))
					return string.Format("{0}-{1}", Text.Substring(0, 3), Text.Substring(3, 7));
				return Text;
			}
		}

		public static string GetReadbleCategorie(ContactType type)
		{
			return BindingHelper.GetDescription(type);
		}

		public virtual string GetReadbleCategorie()
		{
			return GetReadbleCategorie(Type);
		}
	}
}