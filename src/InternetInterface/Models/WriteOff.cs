using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using InternetInterface.Helpers;

namespace InternetInterface.Models
{
	public class BaseWriteOff
	{
		public uint Id { get; set; }

		public decimal WriteOffSum { get; set; }

		public DateTime WriteOffDate { get; set; }

		public uint Client { get; set; }

		public decimal VirtualSum { get; set; }

		public decimal MoneySum { get; set; }


		public virtual string GetDate(string grouped)
		{
			if (grouped == "month")
				return string.Format("{0}.{1}", WriteOffDate.Month.ToString("00"), WriteOffDate.Year);
			if (grouped == "year")
				return string.Format("{0}", WriteOffDate.Year);
			return string.Format("{0}.{1}.{2}", WriteOffDate.Day.ToString("00"), WriteOffDate.Month.ToString("00"), WriteOffDate.Year);
		}
	}


	[ActiveRecord("WriteOff", Schema = "internet", Lazy = true)]
	public class WriteOff : ActiveRecordLinqBase<WriteOff>
	{
		public WriteOff()
		{
		}

		public WriteOff(Client client, decimal writeOffSum, DateTime date)
		{
			Client = client;
			WriteOffSum = writeOffSum;
			WriteOffDate = date;
		}

		public WriteOff(Client client, decimal writeOffSum)
			: this(client, writeOffSum, SystemTime.Now())
		{
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual decimal WriteOffSum { get; set; }

		[Property]
		public virtual decimal VirtualSum { get; set; }

		[Property]
		public virtual decimal MoneySum { get; set; }

		[Property]
		public virtual DateTime WriteOffDate { get; set; }

		[BelongsTo("Client")]
		public virtual Client Client { get; set; }

		[Property]
		public virtual decimal? Sale { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		public static IList<WriteOff> ForClient(Client client)
		{
			return Queryable.Where(w => w.Client == client).ToList();
		}

		public override string ToString()
		{
			return String.Format("{0} - {1}", WriteOffDate, WriteOffSum);
		}
	}
}