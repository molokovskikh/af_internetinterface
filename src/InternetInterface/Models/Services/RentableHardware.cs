using System.ComponentModel;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using NPOI.HSSF.Record.PivotTable;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(Schema = "Internet")]
	public class RentableHardware
	{
		public RentableHardware()
		{
		}

		public RentableHardware(decimal cost, string name)
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
	}
}