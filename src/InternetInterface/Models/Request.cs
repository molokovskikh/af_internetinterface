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
	public class Requests : ActiveRecordLinqBase<Requests>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите ФИО")]
		public virtual string ApplicantName { get; set; }

		[
			Property,
			ValidateNonEmpty("Введите номер телефона"),
			ValidateRegExp(@"^(([0-9]{1})-([0-9]{3})-([0-9]{3})-([0-9]{2})-([0-9]{2}))", "Ошибка фотмата телефонного номера: мобильный телефн (8-***-***-**-**))")
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
		public virtual decimal VirtualBonus { get; set; }
		
		/*[Property]
		public virtual decimal VirtualWriteOff { get; set; }*/

		[Property]
		public virtual bool PaidBonus { get; set; }

		/*[Property]
		public virtual bool Registered { get; set; }*/

		/*public static IOrderedQueryable<Requests> Queryable
		{
			get { return ActiveRecordLinqBase<Requests>.Queryable; }
		}*/

		public virtual IDictionary GetValidateionErrors()
		{
			var validator = new ActiveRecordValidator(this);
			validator.IsValid();
			return validator.PropertiesValidationErrorMessages;
		}

		public virtual string GetValidationError(string field)
		{
			var errors =  GetValidateionErrors();
			return ((ArrayList)errors[GetType().GetProperty(field)])[0].ToString();
		}

		public virtual bool InDictionaryError(string field)
		{
			var errors = GetValidateionErrors();
			return errors.Contains(GetType().GetProperty(field));
		}


		public static List<Requests> GetRequestsForInterval(Week Interval)
		{
			return Queryable.Where(
				r => r.RegDate.Date >= Interval.StartDate.Date && r.RegDate.Date <= Interval.EndDate.Date && r.Registrator == InitializeContent.partner).
				ToList();
		}

		public virtual void SetRequestBoduses()
		{
			var bonusForRequest = 0m;
			var Interval = DateHelper.GetWeekInterval(RegDate);
			var requestsInInterval = GetRequestsForInterval(Interval);
			var for_bonus_requests = requestsInInterval.Where(r => r.Label == null || r.Label.ShortComment != "Refused").ToList();
			if (for_bonus_requests.Count >= 20)
				bonusForRequest += 100m;
			else
			{
				if (for_bonus_requests.Count >= 10)
					bonusForRequest += 50m;
			}
			var weekBonus = 0;
			for (int i = 0; i < 7; i++)
			{
				if (for_bonus_requests.Count(r => r.RegDate.Date == Interval.StartDate.AddDays(i).Date) > 0)
					weekBonus++;
			}
			if (weekBonus >= 5)
				bonusForRequest += 50m;

			foreach (var requestse in requestsInInterval.Where(r => !r.PaidBonus))
			{
				requestse.VirtualBonus = bonusForRequest;
			}

			foreach (var requestse in requestsInInterval.Where(r => r.PaidBonus))
			{
				var payment = bonusForRequest - requestse.VirtualBonus;
				//requestse.VirtualWriteOff = payment;
				requestse.VirtualBonus = bonusForRequest;
				var message = payment > 0
				              	? "Начисление за пересчет бонусов за период с {0} по {1} для заявки #{2}"
				              	: "Списание за пересчет бонусов за период с {0} по {1} для заявки #{2}";
				PaymentsForAgent.CreatePayment(requestse.Registrator, string.Format(message,
				                                                                    Interval.GetStartString(),
				                                                                    Interval.GetEndString(), requestse.Id), payment);
			}
		}
	}
}