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
	public class Act : ActiveRecordLinqBase<Act>
	{
		public Act()
		{
			Parts = new List<ActPart>();
			CreatedOn = DateTime.Now;
		}

		public Act(string payerName, DateTime date)
			: this()
		{
			//SetPayer(payerName);
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

		public Act(Client client, Orders order) : this(client)
		{
			Order = order;
		}

		public Act(DateTime actDate, params Invoice[] invoices)
		{
			Period = invoices.Select(i => i.Period).Distinct().Single();
			Recipient = invoices.Select(i => i.Recipient).Distinct().Single();
			PayerName = invoices.Select(i => i.PayerName).Distinct().Single();
			Customer = invoices.Select(i => i.Customer).Distinct().Single();

			ActDate = actDate;
			var invoiceParts = invoices.SelectMany(i => i.Parts);
			//if (Payer.InvoiceSettings.DoNotGroupParts) {
			Parts = invoiceParts
				.Select(p => new ActPart(p.Name, (int)p.Count, p.Cost) {
					OrderService = p.OrderService
				})
				.ToList();
			//}
			//else {
			//	Parts = invoiceParts
			//		.GroupBy(p => new { p.Name, p.Cost })
			//		.Select(g => new ActPart(g.Key.Name, g.Sum(i => i.Count), g.Key.Cost))
			//		.ToList();
			//}
			CalculateSum();

			//foreach (var part in invoiceParts.Where(p => p.Ad != null))
			//	part.Ad.Act = this;

			foreach (var invoice in invoices)
				invoice.Act = this;

			CreatedOn = DateTime.Now;
		}

		//public void SetPayer(string payer)
		//{
		//	Payer = payer;
		//	Recipient = payer.Recipient;
		//	PayerName = payer.Name;
		//	Customer = payer.Customer;
		//}

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

		[BelongsTo(Column = "OrderId")]
		public virtual Orders Order { get; set; }

		public static IEnumerable<Act> Build(List<Invoice> invoices, DateTime documentDate)
		{
			return invoices
				.Where(i => i.Act == null)
				.GroupBy(i => new { i.PayerName, i.Customer, i.Recipient })
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