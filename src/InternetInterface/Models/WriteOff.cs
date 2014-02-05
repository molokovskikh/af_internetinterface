using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;

namespace InternetInterface.Models
{
	public interface IWriteOff
	{
		uint Id { get; set; }
		Client Client { get; set; }
		Appeals Cancel();
		decimal Sum { get; }
	}

	public class BaseWriteOff
	{
		public uint Id { get; set; }

		public decimal WriteOffSum { get; set; }

		public DateTime WriteOffDate { get; set; }

		public uint Client { get; set; }

		public decimal VirtualSum { get; set; }

		public decimal MoneySum { get; set; }

		public decimal? BeforeWriteOffBalance { get; set; }

		public string Comment { get; set; }

		public bool UserWriteOff { get; set; }

		[Style]
		public bool Commented
		{
			get { return !string.IsNullOrEmpty(Comment); }
		}

		public string GeBeforeWriteOffBalance(string grouped)
		{
			if ((grouped == "month") || (grouped == "year"))
				return "Нет данных";
			if (!BeforeWriteOffBalance.HasValue || BeforeWriteOffBalance == 0)
				return "Нет данных";
			return BeforeWriteOffBalance.Value.ToString("0.00");
		}

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
	public class WriteOff : ActiveRecordLinqBase<WriteOff>, IWriteOff
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

		public WriteOff(Client client, OrderService service)
			: this(client, service.Cost)
		{
			Comment = service.Description + " по заказу №" + service.Order.Number;
			Service = service;
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

		[Property]
		public virtual decimal? BeforeWriteOffBalance { get; set; }

		[BelongsTo]
		public virtual OrderService Service { get; set; }

		public static IList<WriteOff> ForClient(Client client)
		{
			return Queryable.Where(w => w.Client == client).ToList();
		}

		public override string ToString()
		{
			return String.Format("{0} - {1}", WriteOffDate, WriteOffSum);
		}

		public virtual Appeals Cancel()
		{
			if (Client.PhysicalClient != null) {
				Client.PhysicalClient.MoneyBalance += MoneySum;
				Client.PhysicalClient.VirtualBalance += VirtualSum;
				Client.PhysicalClient.Balance += WriteOffSum;
			}
			else
				Client.LawyerPerson.Balance += WriteOffSum;
			return Appeals.CreareAppeal(String.Format("Удалено списание на сумму {0:C}", WriteOffSum), Client);
		}

		public virtual decimal Sum
		{
			get { return WriteOffSum; }
		}
	}
}