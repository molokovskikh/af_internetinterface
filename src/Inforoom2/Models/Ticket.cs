using System;
using System.ComponentModel;
using Common.Tools;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель вопроса от пользователя
	/// </summary>
	[Class(Table = "tickets", NameType = typeof(Ticket))]
	public class Ticket : BaseModel
	{
		public Ticket()
		{
			CreationDate = SystemTime.Now();
		}

		[Property, NotNullNotEmpty(Message = "Текст вопроса должен быть заполнен"), Description("Текст вопроса от пользователя")]
		public virtual string Text { get; set; }

		[Property, Description("Ответ на вопрос от пользователя")]
		public virtual string Answer { get; set; }

		[Property, Description("Дата создания вопроса от пользователя")]
		public virtual DateTime CreationDate { get; set; }

		[Property, Description("Дата создания ответа на вопрос от пользователя")]
		public virtual DateTime AnswerDate { get; set; }

		[Property]
		public virtual bool IsNotified { get; set; }

		[ManyToOne(Column = "User")]
		public virtual Employee Employee { get; set; }

		[ManyToOne]
		public virtual Client Client { get; set; }

		[Property, Description("Электронная почта пользователя создавшего вопрос")]
		public virtual string Email { get; set; }
	}
}