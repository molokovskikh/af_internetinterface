using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using Common.Web.Ui.Helpers;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	public class ConnectInfo
	{
		public string Port { get; set; }
		public uint Switch { get; set; }
		public uint Brigad { get; set; }
		public string static_IP { get; set; }
		public bool Monitoring { get; set; }
	}

    [ActiveRecord(Schema = "Internet", Table = "LawyerPerson", Lazy = true), Auditable]
	public class LawyerPerson : ValidActiveRecordLinqBase<LawyerPerson>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

        [Property, ValidateNonEmpty("Введите полное наименование"), Auditable("Полное наименование")]
		public virtual string FullName { get; set; }

        [Property, ValidateNonEmpty("Введите краткое наименование"), Auditable("Краткое наименование")]
		public virtual string ShortName { get; set; }

        [Property, Auditable("Юридический адрес")]
		public virtual string LawyerAdress { get; set; }

        [Property, Auditable("Фактический адрес")]
		public virtual string ActualAdress { get; set; }

        [Property, Auditable("ИНН")]
		public virtual string INN { get; set; }

        [Property, ValidateEmail("Ошибка воода Email (adr@dom.com)"), Auditable("Email")]
		public virtual string Email { get; set; }

        [Property, ValidateNonEmpty("Введите номер телефона"), ValidateRegExp(@"^((\d{4,5})-(\d{5,6}))|((\d{1})-(\d{3})-(\d{3})-(\d{2})-(\d{2}))", "Ошибка фотмата телефонного номера (Код города (4-5 цифр) + местный номер (5-6 цифр) или мобильный телефн (8-***-***-**-**))"), Auditable("Номер телефона")]
		public virtual string Telephone { get; set; }

        [Property, Auditable("Контактное лицо")]
        public virtual string ContactPerson { get; set; }

        [BelongsTo, Auditable("Тариф")]
		public virtual PackageSpeed Speed { get; set; }

        [Property, ValidateNonEmpty("Введите размер абонентской платы"), ValidateDecimal("Ошибка ввода суммы"), Auditable("Абон. плата")]
		public virtual decimal Tariff { get; set; }

		[Property]
		public virtual decimal Balance { get; set; }

        [Property, Auditable("Почтовый адрес")]
        public virtual string MailingAddress { get; set; }
	}
}