﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Models
{
	public enum ContactType
	{
		MobilePhone,
		HousePhone,
		ConnectedPhone,
		Email,
		FinancePhone
	}

	[ActiveRecord(Table = "Contacts", Schema = "Internet", Lazy = true)]
	public class Contact : ChildActiveRecordLinqBase<Contact>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }

		[Property]
		public virtual ContactType Type { get; set; }

		[Property(Column = "Contact")]
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
				return string.Format("8-{0}-{1}", Text.Substring(0, 3), Text.Substring(3, 7));
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
					return "Почтовый адрес";
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