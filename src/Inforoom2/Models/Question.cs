using System;
using System.ComponentModel;
using Common.Tools;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Класс для вопроса на странице "Вопросы и ответы"
	/// </summary>
	[Class(Table = "questions", NameType = typeof(Question))]
	public class Question : BaseModel, IModelWithPriority
	{
		[Property, NotNullNotEmpty, Description("Текст вопроса")]
		public virtual string Text { get; set; }

		[Property, NotNullNotEmpty, Description("Текст ответа")]
		public virtual string Answer { get; set; }

		[Property, Description("Дата создания")]
		public virtual DateTime CreationDate { get; set; }

		[Property, Description("Маркер, отражающий, опубликован ли вопрос на странице сайта или нет")]
		public virtual bool IsPublished { get; set; }

		[Property(Unique = true), Description("Приоритет отображения вопроса на странице сайта")]
		public virtual int Priority { get; set; }

		public Question(int priority)
		{
			CreationDate = SystemTime.Now();
			Priority = priority;
		}

		public Question()
		{
		}
	}
}