using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "Internet", Table = "LawyerPerson", Lazy = true)]
	public class LawyerPerson : ValidActiveRecordLinqBase<LawyerPerson>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите полное наименование")]
		public virtual string FullName { get; set; }

		[Property, ValidateNonEmpty("Введите краткое наименование")]
		public virtual string ShortName { get; set; }

		[Property]
		public virtual string LawyerAdress { get; set; }

		[Property]
		public virtual string ActualAdress { get; set; }

		[Property]
		public virtual string INN { get; set; }

		[Property, ValidateEmail("Ошибка воода Email (adr@dom.com)")]
		public virtual string Email { get; set; }

		[Property, ValidateNonEmpty("Введите номер телефона"), ValidateRegExp(@"^((\d{4,5})-(\d{5,6}))|((\d{1})-(\d{3})-(\d{3})-(\d{2})-(\d{2}))", "Ошибка фотмата телефонного номера (Код города (4-5 цифр) + местный номер (5-6 цифр) или мобильный телефн (8-***-***-**-**))")]
		public virtual string Telephone { get; set; }

		[BelongsTo]
		public virtual PackageSpeed Speed { get; set; }

		[Property, ValidateNonEmpty("Введите размер абонентской платы"), ValidateDecimal("Ошибка ввода суммы")]
		public virtual decimal Tariff { get; set; }

		[Property]
		public virtual decimal Balance { get; set; }

		[BelongsTo]
		public virtual Clients Client { get; set; }
	}
}