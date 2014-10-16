using System;
using NHibernate.Mapping;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;


namespace Inforoom2.Models
{
	/// <summary>
	/// Класс для вопроса на странице "Вопросы и ответы"
	/// </summary>
	[Class(Table = "questions", NameType = typeof (Question))]
	public class Question : BaseModel
	{
	
		[Property, NotNullNotEmpty]
		public virtual string Text { get; set; }
		
		[Property, NotNullNotEmpty]
		public virtual string Answer { get; set; }
		
		[Property,]
		public virtual DateTime CreationDate { get; set; }

		[Property]
		public virtual bool IsPublished { get; set; }

		[Property(Unique = true)]
		public virtual int Priority { get; set; }

		public Question(int priority)
		{
			CreationDate = DateTime.Now;
			Priority = priority;
		}

		public Question()
		{
		}
	}

}