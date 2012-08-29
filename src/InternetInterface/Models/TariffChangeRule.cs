using System.Collections.Generic;
using Castle.ActiveRecord;
using Common.Tools;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "Internet")]
	public class TariffChangeRule
	{
		public TariffChangeRule()
		{
		}

		public TariffChangeRule(Tariff fromTariff, Tariff toTariff, decimal price)
		{
			FromTariff = fromTariff;
			ToTariff = toTariff;
			Price = price;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual Tariff FromTariff { get; set; }

		[BelongsTo]
		public virtual Tariff ToTariff { get; set; }

		[Property]
		public virtual decimal Price { get; set; }

		public static int IndexOfRule(List<TariffChangeRule> rules, Tariff from, Tariff to)
		{
			return rules.IndexOf(r => r.FromTariff == from && r.ToTariff == to);
		}
	}
}