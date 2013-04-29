using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "internet")]
	public class Act
	{
		public Act()
		{
			Parts = new List<ActPart>();
			CreatedOn = DateTime.Now;
		}

		public Act(string payerName, DateTime date)
			: this()
		{
			PayerName = payerName;
			ActDate = date;
			Period = date.ToPeriod();
		}

		public Act(Client client) : this()
		{
			Recipient = Recipient.Queryable.First(r => r.Name == "Инфорум");
			PayerName = client.LawyerPerson.Name;
			Customer = client.LawyerPerson.Name;
			ActDate = DateTime.Today;
			Period = ActDate.ToPeriod();
			Client = client;
		}

		public Act(DateTime actDate, params Invoice[] invoices)
		{
			Period = invoices.Select(i => i.Period).Distinct().Single();
			Recipient = invoices.Select(i => i.Recipient).Distinct().Single();
			PayerName = invoices.Select(i => i.PayerName).Distinct().Single();
			Customer = invoices.Select(i => i.Customer).Distinct().Single();
			Client = invoices.Select(i => i.Client).Distinct().Single();

			ActDate = actDate;
			var invoiceParts = invoices.SelectMany(i => i.Parts);
			Parts = invoiceParts
				.Select(p => new ActPart(p.Name, (int)p.Count, p.Cost) {
					OrderService = p.OrderService
				})
				.ToList();
			CalculateSum();

			foreach (var invoice in invoices)
				invoice.Act = this;

			CreatedOn = DateTime.Now;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property(ColumnType = "InternetInterface.Models.PeriodUserType, InternetInterface")]
		public Period Period { get; set; }

		[Property]
		public DateTime ActDate { get; set; }

		[Property]
		public decimal Sum { get; set; }

		[BelongsTo, ValidateNonEmpty("У плательщика должен быть установлен получатель платежей")]
		public Recipient Recipient { get; set; }

		[Property]
		public string PayerName { get; set; }

		[Property]
		public string Customer { get; set; }

		[Property]
		public DateTime CreatedOn { get; set; }

		[HasMany(Lazy = true)]
		public IList<Invoice> Invoices { get; set; }

		[HasMany(Cascade = ManyRelationCascadeEnum.AllDeleteOrphan, Lazy = true)]
		public IList<ActPart> Parts { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }


		public static IEnumerable<Act> Build(List<Invoice> invoices, DateTime documentDate)
		{
			return invoices
				.Where(i => i.Act == null)
				.GroupBy(i => new { i.PayerName, i.Customer, i.Recipient, i.Client })
				.Select(g => new Act(documentDate, g.ToArray()))
				.ToList();
		}

		public void CalculateSum()
		{
			Sum = Parts.Sum(p => p.Sum);
		}

		public virtual string SumInWords()
		{
			return ViewHelper.InWords((float)Sum);
		}

		public IList<Order> Orders
		{
			get
			{
				if (Parts == null)
					return new List<Order>();
				var group = Parts.Select(p => p.OrderService).GroupBy(o => o.Order);
				return group.Select(g => g.Key).ToList();
			}
		}
	}

	[ActiveRecord(Schema = "internet")]
	public class ActPart
	{
		public ActPart()
		{
		}

		public ActPart(Act act)
		{
			Act = act;
		}

		public ActPart(string name, int count, decimal cost)
		{
			Name = name;
			Count = count;
			Cost = cost;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property, ValidateNonEmpty]
		public string Name { get; set; }

		[Property, ValidateGreaterThanZero]
		public decimal Cost { get; set; }

		[Property, ValidateGreaterThanZero]
		public int Count { get; set; }

		[BelongsTo]
		public Act Act { get; set; }

		public decimal Sum
		{
			get { return Count * Cost; }
		}

		[BelongsTo]
		public virtual OrderService OrderService { get; set; }
	}
}