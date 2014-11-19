using System;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель новостного блока.
	/// </summary>
	[Class(0, Table = "newsblock", NameType = typeof (NewsBlock))]
	public class NewsBlock : BaseModel, IModelWithPriority
	{
		[Property, NotNullNotEmpty]
		public virtual string Preview { get; set; }

		[Property]
		public virtual string Body { get; set; }

		[Property, NotNullNotEmpty]
		public virtual string Title { get; set; }

		[Property]
		public virtual string Url { get; set; }

		[ManyToOne(Column = "Employee")]
		public virtual Employee Employee { get; set; }

		[Property]
		public virtual DateTime CreationDate { get; set; }

		[Property]
		public virtual DateTime PublishedDate { get; set; }

		[Property]
		public virtual bool IsPublished { get; set; }

		[Property]
		public virtual int Priority { get; set; }

		public NewsBlock(int priority)
		{
			CreationDate = DateTime.Now;
			Priority = priority;
		}

		public NewsBlock()
		{
		}
	}
}