using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;

namespace InternetInterface.Models
{
	[ActiveRecord("Requests", Schema = "Internet", Lazy = true)]
	public class Requests : ActiveRecordLinqBase<Requests>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string ApplicantName { get; set; }

		[Property]
		public virtual string ApplicantPhoneNumber { get; set; }

		[Property]
		public virtual string ApplicantEmail { get; set; }

		[Property]
		public virtual string City { get; set; }

		[Property]
		public virtual string Street { get; set; }

		[Property]
		public virtual string House { get; set; }

		/// <summary>
		/// Корпус
		/// </summary>
		[Property]
		public virtual string CaseHouse { get; set; }

		/// <summary>
		/// Квартира
		/// </summary>
		[Property]
		public virtual string Apartment { get; set; }

		/// <summary>
		/// Подъезд
		/// </summary>
		[Property]
		public virtual string Entrance { get; set; }

		/// <summary>
		/// Этаж
		/// </summary>
		[Property]
		public virtual string Floor { get; set; }

		[Property]
		public virtual bool SelfConnect { get; set; }

		[BelongsTo("Tariff")]
		public virtual Tariff Tariff { get; set; }

		[BelongsTo("Label")]
		public virtual Label Label { get; set; }

		[Property]
		public virtual DateTime ActionDate { get; set; }

		[BelongsTo("Operator")]
		public virtual Partner Operator { get; set; }
	}

}