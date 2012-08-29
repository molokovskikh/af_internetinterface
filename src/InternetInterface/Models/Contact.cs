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
		[Description("Мобильный номер")] MobilePhone,

		[Description("Домашний номер")] HousePhone,

		[Description("Связанный номер")] ConnectedPhone,

		[Description("EMail")] Email,

		[Description("Финансовые вопросы")] FinancePhone,

		[Description("Главный телефон")] HeadPhone,

		[Description("Телефон для смс рассылки")] SmsSending,

		[Description("техническая информация")] TechPhone,
	}

	[ActiveRecord(Table = "Contacts", Schema = "Internet", Lazy = true), Auditable]
	public class Contact : ChildActiveRecordLinqBase<Contact>
	{
		public Contact()
		{
		}

		public Contact(Client client, ContactType type, string text)
		{
			Client = client;
			Type = type;
			Text = text;
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

		public static void SaveNew(Client client, string contact, string comment, ContactType type)
		{
			new Contact {
				Date = DateTime.Now,
				Registrator = InitializeContent.Partner,
				Client = client,
				Text = contact,
				Comment = comment,
				Type = type
			}.Save();
		}
	}
}