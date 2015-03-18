using System;
using System.Text.RegularExpressions;
using InternetInterface.Models;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{

	[Class(0, Table = "Contacts", NameType = typeof (Contact))]
	public class Contact : BaseModel
	{
		
		[ManyToOne]
		public virtual Client Client { get; set; }

		[Property]
		public virtual ContactType Type { get; set; }

		[Property(Column = "Contact"), NotNullNotEmpty(Message = "Введите номер телефона"), Pattern(@"^\d{10}$", RegexOptions.Compiled,Message = "Номер телефона введен неправильно")]
		public virtual string ContactString { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[Property]
		public virtual string ContactName { get; set; }
		
		[Property]
		public virtual DateTime Date { get; set; }
	}
}