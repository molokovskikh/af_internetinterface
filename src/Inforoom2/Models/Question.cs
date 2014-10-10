using System;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;


namespace Inforoom2.Models
{
	/// <summary>
	/// Класс для вопроса на странице "Вопросы и ответы"
	/// </summary>
	[Class(Table = "Questions", NameType = typeof (Question))]
	public class Question : BaseModel
	{
	
		[Property, NotNullNotEmpty]
		public virtual string Text { get; set; }

		[Property, NotNullNotEmpty, Email]
		public virtual string Email { get; set; }

		[Property,]
		public virtual DateTime Date { get; set; }

		[Property]
		public virtual string Answer { get; set; }

		[Property]
		public virtual DateTime? AnswerDate { get; set; }

		[Property]
		public virtual bool Notified { get; set; }

		[Property]
		public virtual bool Published { get; set; }

		[Property]
		public virtual int? Priority { get; set; }

		public Question()
		{
			Date = DateTime.Now;
			Notified = false;
			Published = true;
		}

	}

}