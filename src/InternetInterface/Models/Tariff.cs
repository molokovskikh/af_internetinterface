using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Audit;

namespace InternetInterface.Models
{
	[ActiveRecord("Tariffs", Schema = "internet", Lazy = true), Auditable]
	public class Tariff : ChildActiveRecordLinqBase<Tariff>
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
		public virtual int Price { get; set; }

		[Property]
		public virtual int PackageId { get; set; }

		[Property]
		public virtual bool Hidden { get; set; }

		[Property]
		public virtual bool CanUseForSelfConfigure { get; set; }

		[Property]
		public virtual int FinalPriceInterval { get; set; }

		[Property]
		public virtual decimal FinalPrice { get; set; }

		public static IList<Tariff> All()
		{
			return FindAllSort();
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