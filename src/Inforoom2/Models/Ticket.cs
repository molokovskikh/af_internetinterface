using System;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель вопроса от пользователя
	/// </summary>
	[Class(Table = "tickets", NameType = typeof (Ticket))]
	public class Ticket : BaseModel
	{
		public Ticket()
		{
			CreationDate = DateTime.Now;
		}
		 
		[Property, NotNullNotEmpty]
		public virtual string Text { get; set; }
		
		[Property, NotNullNotEmpty]
		public virtual string Answer { get; set; }
		
		[Property]
		public virtual DateTime CreationDate { get; set; }

		[Property]
		public virtual DateTime AnswerDate { get; set; }

		[Property]
		public virtual bool IsNotified { get; set; }

		[ManyToOne(Column = "User")]
		public virtual Employee Employee { get; set; }

		[Property, Email, NotNullNotEmpty(Message = "Введите Email"), Pattern(@"^\S+@\S+$",Message = "Адрес эл. почты введен неправильно")]
		public virtual string Email { get; set; }
	}
}