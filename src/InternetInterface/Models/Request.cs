using System;
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

		[Property]
		public virtual DateTime RegDate { get; set; }

		[Property]
		public virtual decimal VirtualBonus { get; set; }
		
		[Property]
		public virtual decimal VirtualWriteOff { get; set; }

		/*[Property]
		public virtual bool Registered { get; set; }*/

		public static List<Requests> GetRequestsForInterval(Week Interval)
		{
			return Queryable.Where(
				r => r.RegDate >= Interval.StartDate && r.RegDate <= Interval.EndDate && r.Registrator == InitializeContent.partner).
				ToList();
		}

		public virtual void SetRequestBoduses()
		{
			var bonusForRequest = 0m;
			var Interval = DateHelper.GetWeekInterval(RegDate);
			var requestsInInterval = GetRequestsForInterval(Interval);
			if (requestsInInterval.Count >= 20)
				bonusForRequest += 100m;
			else
			{
				if (requestsInInterval.Count >= 10)
					bonusForRequest += 50m;
			}
			var weekBonus = true;
			for (int i = 0; i < 5; i++)
			{
				if (requestsInInterval.Count(r => r.RegDate.Date == Interval.StartDate.AddDays(i).Date) <= 0)
					weekBonus = false;
			}
			if (weekBonus)
				bonusForRequest += 50m;
			foreach (var requestse in requestsInInterval)
			{
				if (requestse.VirtualBonus > 0m && requestse.VirtualBonus < bonusForRequest)
				{
					requestse.VirtualWriteOff += bonusForRequest - requestse.VirtualBonus;
				}
				requestse.VirtualBonus = bonusForRequest;
			}
		}
	}

}