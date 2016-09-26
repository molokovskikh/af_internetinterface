using System.Collections.Generic;
using System.ComponentModel;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	///		Регионы
	/// </summary>
	[Class(0, Table = "Regions", NameType = typeof(Region))]
	public class Region : BaseModel
	{
		public Region()
		{
			Streets = new List<Street>();
			Plans = new List<Plan>();
			Children = new List<Region>();
		}

		[Description("Наименование региона")]
		[Property(Column = "Region"),NotEmpty(Message = "поле должно быть заполнено")]
		public virtual string Name { get; set; }

		[Description("Город, к которому привязан регион")]
		[ManyToOne(Column = "_City", Cascade = "save-update")]
		public virtual City City { get; set; }

		//todo не используется
		[Description("Тарифные планы региона")]
		[Bag(0, Table = "region_plan")]
		[Key(1, Column = "region", NotNull = false)]
		[ManyToMany(2, Column = "Plan", ClassType = typeof(Plan))]
		public virtual IList<Plan> Plans { get; set; }

		[Description("Телефон офиса")]
		[Property(Column = "_RegionOfficePhoneNumber"), NotEmpty(Message = "поле должно быть заполнено")]
		public virtual string RegionOfficePhoneNumber { get; set; }

		[Description("Адрес офиса")]
		[Property(Column = "_OfficeAddress"), NotEmpty(Message = "поле должно быть заполнено")]
		public virtual string OfficeAddress { get; set; }

		[Description("Геометка офиса")]
		[Property(Column = "_OfficeGeomark")]
		public virtual string OfficeGeomark { get; set; }

		[Description("Улицы региона")]
		[Bag(0, Table = "Street", Cascade = "all-delete-orphan")]
		[Key(1, Column = "Region")]
		[OneToMany(2, ClassType = typeof(Street))]
		public virtual IList<Street> Streets { get; set; }

		[Description("Выводится ли регион на главной странице сайта")]
		[Property]
		public virtual bool ShownOnMainPage { get; set; }

		[Description("Дочерние регионы")]
		[Bag(0, Table = "Region", Cascade = "save-update")]
		[Key(1, Column = "Parent", ForeignKey = "Id")]
		[OneToMany(2, ClassType = typeof(Region))]
		public virtual IList<Region> Children { get; set; }

		[ManyToOne(Column = "Parent", Cascade = "save-update")]
		public virtual Region Parent { get; set; }

		[Description("Маркер, отражающий, есть ли дочерние эл-ты у региона")]
		public virtual bool HasChildren()
		{
			return Children.Count > 0;
		}
		[Property]
		public virtual bool GenerateConnectedHouses { get; set; }
	}
}