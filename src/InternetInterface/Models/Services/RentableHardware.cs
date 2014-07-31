using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Web.Ui.ActiveRecordExtentions;
using NHibernate.Linq;
using NPOI.HSSF.Record.PivotTable;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(Schema = "Internet")]
	public class RentDocItem
	{
		public RentDocItem()
		{
		}

		public RentDocItem(string name, int count)
		{
			Name = name;
			Count = count;
		}

		[PrimaryKey]
		public virtual int Id { get; set; }

		[Property, ValidateNonEmpty]
		public virtual string Name { get; set; }

		[Property, ValidateNonEmpty]
		public virtual int Count { get; set; }

		[BelongsTo]
		public RentableHardware RentableHardware { get; set; }
	}

	[ActiveRecord(Schema = "Internet")]
	public class RentableHardware
	{
		public RentableHardware()
		{
			DefaultDocItems = new List<RentDocItem>();
		}

		public RentableHardware(decimal cost, string name)
			: this()
		{
			Cost = cost;
			Name = name;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, Description("Стоимость")]
		public virtual decimal Cost { get; set; }

		[Property, Description("Название"), ValidateIsUnique("Оборудование с таким именем уже существует"), ValidateNonEmpty]
		public virtual string Name { get; set; }

		[HasMany(Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public virtual IList<RentDocItem> DefaultDocItems { get; set; }

		public static List<RentableHardware> All()
		{
			return ArHelper.WithSession(s => s.Query<RentableHardware>().OrderBy(h => h.Name).ToList());
		}
	}
}