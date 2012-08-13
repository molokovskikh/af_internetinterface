using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using NHibernate;
using NHibernate.Linq;

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

		[Property, ValidateRange(RangeValidationType.Decimal, 0, 10000), Description("Стоимость подключения")]
		public virtual decimal ActivationCost { get; set; }

		public static List<ChannelGroup> All(ISession dbSession)
		{
			return dbSession.Query<ChannelGroup>().OrderBy(g => g.Name).ToList();
		}

		public static List<ChannelGroup> All()
		{
			return ArHelper.WithSession(s => All(s));
		}
	}
}
