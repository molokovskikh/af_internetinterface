using System.ComponentModel;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Web.Ui.MonoRailExtentions;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(Schema = "Internet")]
	public class ChannelGroup
	{
		public ChannelGroup()
		{}

		public ChannelGroup(string name,
			decimal costPerMonth,
			decimal costPerMonthWithInternet)
		{
			Name = name;
			CostPerMonth = costPerMonth;
			CostPerMonthWithInternet = costPerMonthWithInternet;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty, Description("Название")]
		public virtual string Name { get; set; }

		[Property, Description("Скрыт")]
		public virtual bool Hidden { get; set; }

		[Property, ValidateRange(RangeValidationType.Decimal, 0, 10000), Description("Стоимость")]
		public virtual decimal CostPerMonth { get; set; }

		[Property, ValidateRange(RangeValidationType.Decimal, 0, 10000), Description("Стоимость если подключена услуга \"Интернет\"")]
		public virtual decimal CostPerMonthWithInternet { get; set; }
	}
}
