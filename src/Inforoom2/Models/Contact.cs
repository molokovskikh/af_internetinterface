using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Inforoom2.Intefaces;
using Inforoom2.validators;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "Contacts", NameType = typeof(Contact))]
	public class Contact : BaseModel, ILogAppeal
	{
		[ManyToOne]
		public virtual Client Client { get; set; }

		[Property]
		public virtual ContactType Type { get; set; }

		//[Property(Column = "Contact"), NotNullNotEmpty(Message = "Введите номер телефона"), Pattern(@"^\d{10}$", RegexOptions.Compiled, Message = "Номер телефона введен неправильно")]
		[Property(Column = "Contact"), NotNullNotEmpty(Message = "Введите номер телефона")] //Введите контакт
		public virtual string ContactString { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[Property]
		public virtual string ContactName { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		public virtual Client GetAppealClient(ISession session)
		{
			return this.Client;
		}

		public virtual List<string> GetAppealFields()
		{
			return new List<string>() {
				"ContactString",
				"Comment",
				"ContactName",
				"Date"
			};
		}

		public virtual string GetAdditionalAppealInfo(string property, object oldPropertyValue, ISession session)
		{
			return "";
		} 
	}
}