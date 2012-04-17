using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Web.Ui.Helpers;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "Internet")]
	public class Invoice
	{
		public Invoice()
		{
			Parts = new List<InvoicePart>();
			Date = DateTime.Today;
		}

		public Invoice(Client client)
			: this()
		{
			Recipient = Recipient.Queryable.First(r => r.Name == "Инфорум");
			Client = client;
			PayerName = client.LawyerPerson.Name;
			Customer = client.LawyerPerson.Name;
			Date = DateTime.Today;
			Period = Date.ToPeriod();
		}

		public Invoice(Client client, Period period, IEnumerable<WriteOff> writeOffs)
			: this(client)
		{
			Period = period;
			var sum = writeOffs.Select(w => w.WriteOffSum).DefaultIfEmpty().Sum();
			Parts.Add(new InvoicePart(this, 1, sum, "Услуги доступа в интернет"));

			CalculateSum();
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateGreaterThanZero]
		public virtual decimal Sum { get; set; }

		[Property, ValidateNonEmpty]
		public virtual string PayerName { get; set; }

		[Property, ValidateNonEmpty]
		public virtual string Customer { get; set; }

		[Property(ColumnType = "InternetInterface.Models.PeriodUserType, InternetInterface"), ValidateNonEmpty]
		public Period Period { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }

		[BelongsTo]
		public virtual Recipient Recipient { get; set; }

		[HasMany(Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public virtual IList<InvoicePart> Parts { get; set; }

		public virtual string SumInWords()
		{
			return ViewHelper.InWords((float) Sum);
		}

		public virtual void CalculateSum()
		{
			Sum = Parts.Select(p => p.Sum).DefaultIfEmpty().Sum();
		}
	}

	[ActiveRecord(Schema = "Internet")]
	public class InvoicePart
	{
		public InvoicePart()
		{}

		public InvoicePart(Invoice invoice, uint count, decimal cost, string name)
		{
			Invoice = invoice;
			Count = count;
			Cost = cost;
			Name = name;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual decimal Cost { get; set; }

		[Property]
		public virtual uint Count { get; set; }

		public virtual decimal Sum
		{
			get { return Cost * Count; }
		}

		[BelongsTo]
		public virtual Invoice Invoice { get; set; }
	}
}