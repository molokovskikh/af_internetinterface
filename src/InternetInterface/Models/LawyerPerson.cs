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
		public uint PackageId { get; set; }
	}

	[ActiveRecord(Schema = "Internet", Table = "LawyerPerson", Lazy = true), Auditable]
	public class LawyerPerson : ValidActiveRecordLinqBase<LawyerPerson>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property("FullName"), ValidateNonEmpty("Введите полное наименование"), Auditable("Полное наименование")]
		public virtual string Name { get; set; }

		[Property, ValidateNonEmpty("Введите краткое наименование"), Auditable("Краткое наименование")]
		public virtual string ShortName { get; set; }

		[Property, Auditable("Юридический адрес")]
		public virtual string LawyerAdress { get; set; }

		[Property, Auditable("Фактический адрес")]
		public virtual string ActualAdress { get; set; }

		[Property, Auditable("ИНН")]
		public virtual string INN { get; set; }

		[ValidateEmail("Ошибка воода Email (adr@dom.com)")]
		public virtual string Email { get; set; }

		[/*ValidateNonEmpty("Введите номер телефона"),*/ ValidateRegExp(@"^((\d{3})-(\d{7}))", "Ошибка фотмата телефонного номера (***-*******)")]
		public virtual string Telephone { get; set; }

		[Property, Auditable("Контактное лицо")]
		public virtual string ContactPerson { get; set; }

		[Property, ValidateDecimal("Ошибка ввода суммы"), ValidateGreaterThanZero, Auditable("Абон. плата")]
		public virtual decimal? Tariff { get; set; }

		[Property]
		public virtual decimal Balance { get; set; }

		[Property, Auditable("Почтовый адрес")]
		public virtual string MailingAddress { get; set; }

		[BelongsTo]
		public virtual Recipient Recipient { get; set; }

		public virtual Client client
		{
			get { return Client.Queryable.Where(c => c.LawyerPerson == this).FirstOrDefault(); }
		}
	}
}