using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;

namespace InternetInterface.Models
{
	[ActiveRecord("Requests", Schema = "Internet", Lazy = true)]
	public class Requests : ActiveRecordLinqBase<Requests>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите ФИО")]
		public virtual string ApplicantName { get; set; }

		[Property, ValidateNonEmpty("Введите номер телефона"), ValidateRegExp(@"^(([0-9]{1})-([0-9]{3})-([0-9]{3})-([0-9]{2})-([0-9]{2}))", "Ошибка фотмата телефонного номера: мобильный телефн (8-***-***-**-**))")]
		public virtual string ApplicantPhoneNumber { get; set; }

		[Property, ValidateEmail("Ошибка вооба Email (должно быть adr@domen.com)")]
		public virtual string ApplicantEmail { get; set; }

		[Property]
		public virtual string City { get; set; }

		[Property, ValidateNonEmpty("Введите улицу")]
		public virtual string Street { get; set; }

		[Property, ValidateNonEmpty("Введите номер дома"), ValidateInteger("Здесь должно быть число")]
		public virtual string House { get; set; }

		/// <summary>
		/// Корпус
		/// </summary>
		[Property]
		public virtual string CaseHouse { get; set; }

		/// <summary>
		/// Квартира
		/// </summary>
		[Property, ValidateNonEmpty("Введите номер квартиры"), ValidateInteger("Здесь должно быть число")]
		public virtual string Apartment { get; set; }

		/// <summary>
		/// Подъезд
		/// </summary>
		[Property, ValidateInteger("Здесь должно быть число")]
		public virtual string Entrance { get; set; }

		/// <summary>
		/// Этаж
		/// </summary>
		[Property, ValidateInteger("Здесь должно быть число")]
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

		[BelongsTo]
		public virtual Partner Registrator { get; set; }

		/*[Property]
		public virtual bool Registered { get; set; }*/
	}

}