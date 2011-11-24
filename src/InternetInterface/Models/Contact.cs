using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Models
{
	public enum ContactType
	{
		[Description("Мобильный номер")]
		MobilePhone,
		[Description("Домашний номер")]
		HousePhone,
		[Description("Связанный номер")]
		ConnectedPhone,
		[Description("EMail")]
		Email,
		[Description("Финансовые вопросы")]
		FinancePhone,
		[Description("Главный телефон")]
		HeadPhone
	}

	[ActiveRecord(Table = "Contacts", Schema = "Internet", Lazy = true), Auditable]
	public class Contact : ChildActiveRecordLinqBase<Contact>
	{
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

		[BelongsTo]
		public virtual Partner Registrator { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		public virtual string HumanableNumber()
		{
			if (new Regex(@"^((\d{10}))").IsMatch(Text))
				return string.Format("{0}-{1}", Text.Substring(0, 3), Text.Substring(3, 7));
			return Text;
		}

		public static string GetReadbleCategorie(ContactType type)
		{
			switch (type) {
				case ContactType.ConnectedPhone:
					return "Связанный номер";
				case ContactType.HousePhone:
					return "Домашний номер";
				case ContactType.MobilePhone:
					return "Мобильный номер";
				case ContactType.FinancePhone:
					return "Финансовые вопросы";
				case ContactType.Email:
					return "EMail";
				case ContactType.HeadPhone:
					return "Главный телефон";
				default: 
				return "Номер без категории";
			}
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