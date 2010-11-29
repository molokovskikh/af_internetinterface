using System;
using System.Collections;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{

	[ActiveRecord("PhysicalClients", Schema = "internet", Lazy = true)]
	public class PhisicalClients : ValidActiveRecordLinqBase<PhisicalClients>
	{

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите имя")]
		public virtual string Name { get; set; }

		[Property, ValidateNonEmpty("Введите фамилию")]
		public virtual string Surname { get; set; }

		[Property, ValidateNonEmpty("Введите отчество")]
		public virtual string Patronymic { get; set; }

		[Property]
		public virtual string City { get; set; }

		/*[Property, ValidateNonEmpty("Введите адрес подключения")]
		public virtual string AdressConnect { get; set; }*/
		[Property, ValidateNonEmpty("Введите улицу")]
		public virtual string Street { get; set; }

		[Property, ValidateNonEmpty("Введите номер дома"), ValidateInteger("Должно быть введено число")]
		public virtual string House { get; set; }

		[Property]
		public virtual string CaseHouse { get; set; }

		[Property, ValidateNonEmpty("Введите номер квартиры"), ValidateInteger("Должно быть введено число")]
		public virtual string Apartment { get; set; }

		[Property, ValidateNonEmpty("Введите номер подъезда"), ValidateInteger("Должно быть введено число")]
		public virtual string Entrance { get; set; }

		[Property, ValidateNonEmpty("Введите номер этажа"), ValidateInteger("Должно быть введено число")]
		public virtual string Floor { get; set; }

		[Property, ValidateRegExp(@"^((\d{1})-(\d{3})-(\d{3})-(\d{2})-(\d{2}))", "Ошибка фотмата телефонного номера: мобильный телефн (8-***-***-**-**))")
		, ValidateNonEmpty("Введите номер телефона")]
		public virtual string PhoneNumber { get; set; }

		[Property, ValidateRegExp(@"^((\d{4,5})-(\d{5,6}))", "Ошибка фотмата телефонного номера (Код города (4-5 цифр) + местный номер (5-6 цифр)")]
		public virtual string HomePhoneNumber { get; set; }

		[Property]
		public virtual string WhenceAbout { get; set; }

		[Property, ValidateRegExp(@"^(\d{4})?$", "Неправильный формат серии паспорта (4 цифры)"), ValidateNonEmpty("Поле не должно быть пустым")]
		public virtual string PassportSeries { get; set; }

		[Property, ValidateRegExp(@"^(\d{6})?$", "Неправильный формат номера паспорта (6 цифр)"), ValidateNonEmpty("Поле не должно быть пустым")]
		public virtual string PassportNumber { get; set; }

		[Property, ValidateNonEmpty("Введите дату выдачи паспорта"), ValidateDate("Ошибка формата даты **-**-****")]
		public virtual string OutputDate { get; set; }

		[Property, ValidateNonEmpty("Заполните поле 'Кем выдан паспорт'")]
		public virtual string WhoGivePassport { get; set; }

		[Property, ValidateNonEmpty("Введите адрес регистрации")]
		public virtual string RegistrationAdress { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[BelongsTo("Tariff")]
		public virtual Tariff Tariff { get; set; }

		[Property, ValidateNonEmpty("Введите сумму"), ValidateDecimal("Неверно введено число")]
		public virtual string Balance { get; set; }

		[Property, ValidateNonEmpty("Введите логин"), ValidateIsUnique("Логин должен быть уникальный")]
		public virtual string Login { get; set; }

		[Property, ValidateNonEmpty("Введите пароль")]
		public virtual string Password { get; set; }

		[BelongsTo("HasRegistered")]
		public virtual Partner HasRegistered { get; set; }

		[BelongsTo("HasConnected")]
		public virtual Brigad HasConnected { get; set; }

		[Property]
		public virtual bool Connected { get; set; }

		public virtual bool IsConnected()
		{
			return this.Connected;
		}

		public static bool RegistrLogicClient(PhisicalClients _client, uint _tariff,
			ValidatorRunner validator, Partner hasRegistered, PaymentForConnect connectSumm)
		{
				if (validator.IsValid(_client))
				{
					_client.RegDate = DateTime.Now;
					_client.Tariff = Tariff.Find(_tariff);
					_client.Password = CryptoPass.GetHashString(_client.Password);
					_client.HasRegistered = hasRegistered;
					_client.SaveAndFlush();
					if (validator.IsValid(connectSumm))
					{
						connectSumm.ClientId = _client;
						connectSumm.ManagerID = InithializeContent.partner;
						connectSumm.PaymentDate = DateTime.Now;
						connectSumm.SaveAndFlush();
						return true;
					}
				}
				return false;
		}
	}

}