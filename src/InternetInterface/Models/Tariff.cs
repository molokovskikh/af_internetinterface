﻿using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Common.Tools;
using Common.Web.Ui.Helpers;

namespace InternetInterface.Models
{
    [ActiveRecord("Tariffs", Schema = "internet", Lazy = true), Auditable]
	public class Tariff : ChildActiveRecordLinqBase<Tariff>
	{
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
		public virtual int FinalPriceInterval { get; set; }

		[Property]
		public virtual decimal FinalPrice { get; set; }

        public virtual string GetFullName()
        {
            return string.Format("{0} ({1} рублей)", Name, Price);
        }

        public virtual string GetFullName(Clients client)
		{
            return string.Format("{0} ({1} рублей)", Name, GetPrice(client).ToString("0"));
		}

		public virtual decimal GetPrice(Clients client)
		{
            if (client.VoluntaryBlockingDate != null)
            {
                if ((SystemTime.Now() - client.VoluntaryBlockingDate.Value).Days < 15)
                    return 0;
                return Service.GetByName("VoluntaryBlocking").Price;
            }

		    if (FinalPriceInterval == 0 || FinalPrice == 0)
				return Price;

			if (client.BeginWork.Value.AddMonths(FinalPriceInterval) <= SystemTime.Now())
				return FinalPrice;

			return Price;
		}

		public override string ToString()
		{
			return GetFullName();
		}
	}
}