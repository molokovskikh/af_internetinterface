using System;
using System.Collections;
using System.ComponentModel;
using Castle.ActiveRecord;
using Castle.Components.Validator;

namespace InternetInterface.Models
{
	public enum RequestType
	{
		[Description("от клиента")] FromClient = 1,
		[Description("от оператора")] FromOperator = 2
	}

	[ActiveRecord("Requests", Schema = "Internet", Lazy = true)]
	public class Request
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите ФИО"), Description("ФИО")]
		public virtual string ApplicantName { get; set; }

		[
			Property,
			ValidateNonEmpty("Введите номер телефона"),
			ValidateRegExp(@"^(([0-9]{1})-([0-9]{3})-([0-9]{3})-([0-9]{2})-([0-9]{2}))", "Ошибка формата телефонного номера: мобильный телефон (8-***-***-**-**))"),
			Description("Номер телефона")
		]
		public virtual string ApplicantPhoneNumber { get; set; }

		[Property, ValidateEmail("Ошибка ввода Email (должно быть adr@domen.com)"), Description("Электронная почта")]
		public virtual string ApplicantEmail { get; set; }

		[Property, Description("Город")]
		public virtual string City { get; set; }

		[Property, ValidateNonEmpty("Введите улицу"), Description("Улица")]
		public virtual string Street { get; set; }

		[Property, ValidateNonEmpty("Введите номер дома"), ValidateInteger("Здесь должно быть число"), Description("Дом")]
		public virtual int? House { get; set; }

		/// <summary>
		/// Корпус
		/// </summary>
		[Property, Description("Корпус")]
		public virtual string CaseHouse { get; set; }

		/// <summary>
		/// Квартира
		/// </summary>
		[Property, ValidateNonEmpty("Введите номер квартиры"), ValidateInteger("Здесь должно быть число"), Description("Квартира")]
		public virtual int? Apartment { get; set; }

		/// <summary>
		/// Подъезд
		/// </summary>
		[Property, ValidateInteger("Здесь должно быть число"), Description("Подъезд")]
		public virtual int? Entrance { get; set; }

		/// <summary>
		/// Этаж
		/// </summary>
		[Property, ValidateInteger("Здесь должно быть число"), Description("Этаж")]
		public virtual int? Floor { get; set; }

		[Property]
		public virtual bool SelfConnect { get; set; }

		[BelongsTo("Tariff"), ValidateNonEmpty, Description("Тарифный план")]
		public virtual Tariff Tariff { get; set; }

		[BelongsTo("Label")]
		public virtual Label Label { get; set; }

		[Property]
		public virtual DateTime ActionDate { get; set; }

		[BelongsTo("Operator")]
		public virtual Partner Operator { get; set; }

		[Property("_RequestSource"), ValidateNonEmpty, Description("Заявка")]
		public virtual RequestType RequestSource { get; set; }

		[BelongsTo, Description("Регистратор")]
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

		public virtual void PreInsert()
		{
			RegDate = DateTime.Now;
			ActionDate = DateTime.Now;
			ApplicantPhoneNumber = ApplicantPhoneNumber
				.Substring(2, ApplicantPhoneNumber.Length - 2).Replace("-", string.Empty);
			if (RequestSource == RequestType.FromClient)
				Registrator = null;
		}
	}
}