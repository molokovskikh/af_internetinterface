using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Audit;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "internet", Lazy = true), Auditable]
	public class Tariff
	{
		public Tariff()
		{
		}

		public Tariff(string name, int price)
		{
			Name = name;
			Description = name;
			Price = price;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string Description { get; set; }

		[Property]
		public virtual int? StoppageMonths { get; set; }

		[Property]
		public virtual int Price { get; set; }

		[Property]
		public virtual int PackageId { get; set; }
	
		[Property]
		public virtual bool Iptv { get; set; }

		[Property]
		public virtual bool Hidden { get; set; }

		[Property, Description("Доступен в личном кабинете")]
		public virtual bool CanUseForSelfConfigure { get; set; }

		[Property]
		public virtual int FinalPriceInterval { get; set; }

		[Property]
		public virtual decimal FinalPrice { get; set; }

		[BelongsTo("RegionId"), Description("Регион")]
		public virtual RegionHouse Region { get; set; }

		[Property, Obsolete("Подготовка к удалению")]
		public virtual bool CanUseForSelfRegistration { get; set; }

		[Property]
		public virtual bool IgnoreDiscount { get; set; }

		public static IList<Tariff> All(ISession session)
		{
			return session.Query<Tariff>().OrderBy(t => t.Name).ToList();
		}

		public static IList<Tariff> All()
		{
			return ArHelper.WithSession(s => All(s));
		}

		public virtual string GetFullName()
		{
			return string.Format("{0} ({1} рублей)", Name, Price);
		}

		public virtual string GetFullName(Client client)
		{
			return string.Format("{0} ({1} рублей)", Name, client.GetPrice().ToString("0"));
		}

		public override string ToString()
		{
			return GetFullName();
		}
	}
}