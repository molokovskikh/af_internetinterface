using System;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель вопроса от пользователя на странице бизнес
	/// </summary>
	[Class(Table = "callmeback_tickets", NameType = typeof(CallMeBackTicket))]
	public class CallMeBackTicket : BaseModel
	{
		public CallMeBackTicket()
		{
			CreationDate = DateTime.Now;
		}

		[Property, NotNullNotEmpty(Message = "Введите комментарий")]
		public virtual string Text { get; set; }

		[Property]
		public virtual DateTime CreationDate { get; set; }

		[Property]
		public virtual DateTime AnswerDate { get; set; }

		[ManyToOne(Column = "Employee")]
		public virtual Employee Employee { get; set; }

		[Property]
		public new virtual string Email { get; set; }

		[Property(Column = "Phone"), Pattern(@"^\d{10}$", Message = "Введите номер в десятизначном формате"), NotNullNotEmpty(Message = "Введите номер телефона")]
		public virtual string PhoneNumber { get; set; }

		[Property, NotNullNotEmpty(Message = "Введите имя")]
		public virtual string Name { get; set; }

		[ManyToOne]
		public virtual Client Client { get; set; }
	}
}