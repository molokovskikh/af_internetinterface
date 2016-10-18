using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Web.Services.Description;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	public enum PublicDataType
	{
		Default = 0,
		PriceList = 1
	}

	/// <summary>
	/// Модель города
	/// </summary>
	[Class(0, Table = "PublicData", NameType = typeof(PublicData))]
	public class PublicData : BaseModel, IPositionIndex
	{
		public PublicData()
		{
			Items = new List<PublicDataContext>();
		}
		
		[Property, NotNullNotEmpty(Message = "не указан заголовок")]
		public virtual string Name { get; set; }

		[ManyToOne(Column = "Region"), Description("Регион")]
		public virtual Region Region { get; set; }

		[Property, NotNull]
		public virtual DateTime LastUpdate { get; set; }

		[Property]
		public virtual PublicDataType ItemType { get; set; }

		[Property]
		public virtual bool Display { get; set; }

		[Property(Column = "RowIndex")]
		public virtual int? PositionIndex { get; set; }

		[Bag(0, Table = "PublicDataContext", Cascade = "all-delete-orphan")]
		[Key(1, Column = "ParentId")]
		[OneToMany(2, ClassType = typeof(PublicDataContext))]
		public virtual IList<PublicDataContext> Items { get; set; }
	}
}