using System;
using System.Collections;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using InternetInterface.Models;


namespace InforoomInternet.Models
{
	/*[ActiveRecord(Schema = "Internet")]
	public class Tariff : ActiveRecordValidationBase<Tariff>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property(NotNull = true)]
		public virtual string Name { get; set; }

		[Property(NotNull = true)]
		public virtual string Description { get; set; }

		[Property(NotNull = true)]
		public virtual uint Price { get; set; }
	}
	*/
	[ActiveRecord(Schema = "Internet")]
	public class Request : ActiveRecordValidationBase<Request>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property(NotNull = true), ValidateNonEmpty("Пожалуйста, укажите свои данные")]
		public virtual string ApplicantName { get; set; }

        [
            Property,
            ValidateRegExp(@"^((\d{1})-(\d{3})-(\d{3})-(\d{2})-(\d{2}))", "Ошибка фотмата телефонного номера: мобильный телефн (8-***-***-**-**))"),
            ValidateNonEmpty("Введите номер телефона")
        ]
		public virtual string ApplicantPhoneNumber { get; set; }

		[Property, ValidateEmail("Недопустимый адрес электронной почты")]
		public virtual string ApplicantEmail { get; set; }

		[Property]
		public virtual string City { get; set; }

		[Property, ValidateNonEmpty("Введите улицу")]
		public virtual string Street { get; set; }

		[Property, ValidateNonEmpty("Введите номер дома"), ValidateInteger("Должно быть введено число")]
		public virtual string House { get; set; }

		[Property]
		public virtual string CaseHouse { get; set; }

		[Property]
		public virtual DateTime ActionDate { get; set; }

		[Property, ValidateNonEmpty("Введите номер квартиры"), ValidateInteger("Должно быть введено число")]
		public virtual string Apartment { get; set; }

		[Property, ValidateNonEmpty("Введите номер подъезда"), ValidateInteger("Должно быть введено число")]
		public virtual string Entrance { get; set; }

		[Property, ValidateNonEmpty("Введите номер этажа"), ValidateInteger("Должно быть введено число")]
		public virtual string Floor { get; set; }
		/*[Property(NotNull = true), ValidateNonEmpty("Пожалуйста, укажите адрес проживания")]
		public virtual string ApplicantResidence { get; set; }*/

		[BelongsTo(NotNull = true), ValidateNonEmpty("Вы не выбрали тарифный план")]
		public virtual Tariff Tariff { get; set; }

		[Property(NotNull = true)]
		public virtual bool SelfConnect { get; set; }

		public virtual string GetValidationError(string field)
		{
			return ((ArrayList)PropertiesValidationErrorMessages[GetType().GetProperty(field)])[0].ToString();
		}

		public virtual bool InDictionaryError ( string field )
		{
			return PropertiesValidationErrorMessages.Contains(GetType().GetProperty(field));
		}
	}

	[ActiveRecord(Schema = "Internet")]
	public class Street : ActiveRecordLinqBase<Street>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property(NotNull = true)]
		public virtual string Name { get; set; }

		public static void GetStreetList (string qString, TextWriter writer)
		{
			var subs = qString.Split(' ');

			foreach (var sub in subs) {
				var tempRes = Queryable.Where(n => n.Name.Contains(sub)).ToList();
				if ( tempRes.Count == 0 )
				{
					continue;
				}
				foreach (var str in tempRes)
				{
					writer.Write("{0}\n", str.Name);
				}
			}
		}
	}
}