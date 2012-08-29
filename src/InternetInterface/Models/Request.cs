using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;

namespace InternetInterface.Models
{
	[ActiveRecord("Requests", Schema = "Internet", Lazy = true)]
	public class Request : ActiveRecordLinqBase<Request>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите ФИО")]
		public virtual string ApplicantName { get; set; }

		[
			Property,
			ValidateNonEmpty("Введите номер телефона"),
			ValidateRegExp(@"^(([0-9]{1})-([0-9]{3})-([0-9]{3})-([0-9]{2})-([0-9]{2}))", "Ошибка формата телефонного номера: мобильный телефн (8-***-***-**-**))")
		]
		public virtual string ApplicantPhoneNumber { get; set; }

		[Property, ValidateEmail("Ошибка вооба Email (должно быть adr@domen.com)")]
		public virtual string ApplicantEmail { get; set; }

		[Property]
		public virtual string City { get; set; }

		[Property, ValidateNonEmpty("Введите улицу")]
		public virtual string Street { get; set; }

		[Property, ValidateNonEmpty("Введите номер дома"), ValidateInteger("Здесь должно быть число")]
		public virtual int? House { get; set; }

		/// <summary>
		/// Корпус
		/// </summary>
		[Property]
		public virtual string CaseHouse { get; set; }

		/// <summary>
		/// Квартира
		/// </summary>
		[Property, ValidateNonEmpty("Введите номер квартиры"), ValidateInteger("Здесь должно быть число")]
		public virtual int? Apartment { get; set; }

		/// <summary>
		/// Подъезд
		/// </summary>
		[Property, ValidateInteger("Здесь должно быть число")]
		public virtual int? Entrance { get; set; }

		/// <summary>
		/// Этаж
		/// </summary>
		[Property, ValidateInteger("Здесь должно быть число")]
		public virtual int? Floor { get; set; }

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

		[Property]
		public virtual DateTime RegDate { get; set; }

		[Property]
		public virtual bool Archive { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }

		[Property]
		public virtual bool PaidBonus { get; set; }

		[BelongsTo]
		public virtual Client FriendThisClient { get; set; }

		[Property]
		public virtual bool PaidFriendBonus { get; set; }


		public virtual IDictionary GetValidateionErrors()
		{
			var validator = new ActiveRecordValidator(this);
			validator.IsValid();
			return validator.PropertiesValidationErrorMessages;
		}

		public virtual string GetValidationError(string field)
		{
			var errors = GetValidateionErrors();
			return ((ArrayList)errors[GetType().GetProperty(field)])[0].ToString();
		}

		public virtual bool InDictionaryError(string field)
		{
			var errors = GetValidateionErrors();
			return errors.Contains(GetType().GetProperty(field));
		}
	}
}