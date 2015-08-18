using System;
using System.ComponentModel;
using Common.Tools;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель новостного блока.
	/// </summary>
	[Class(0, Table = "newsblock", NameType = typeof(NewsBlock))]
	public class NewsBlock : BaseModel, IModelWithPriority
	{
		[Property, NotNullNotEmpty]
		public virtual string Preview { get; set; }

		[Property, Description("Текст-содержимое новостного блока")]
		public virtual string Body { get; set; }

		[Property, NotNullNotEmpty, Description("Заголовок новостного блока")]
		public virtual string Title { get; set; }

		[Property, Description("Ссылка на новостной блок")]
		public virtual string Url { get; set; }

		[ManyToOne]
		public virtual Region Region { get; set; }

		[ManyToOne(Column = "User")]
		public virtual Employee Employee { get; set; }

		[Property, Description("Дата создания новостного блока")]
		public virtual DateTime CreationDate { get; set; }

		[Property, Description("Дата опубликования новостного блока на странице сайта")]
		public virtual DateTime PublishedDate { get; set; }

		[Property, Description("Маркер, отражающий, опубликован ли новостной блок на странице сайта или нет")]
		public virtual bool IsPublished { get; set; }

		[Property, Description("Приоритет отображения новостного блока на странице сайта")]
		public virtual int Priority { get; set; }

		public NewsBlock(int priority)
		{
			CreationDate = SystemTime.Now();
			Priority = priority;
		}

		public NewsBlock()
		{
		}
	}
}