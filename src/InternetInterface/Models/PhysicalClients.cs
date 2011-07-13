using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;
using Castle.MonoRail.Framework;
using Common.Models.Helpers;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models.Universal;
//using NHibernate.Transform;

namespace InternetInterface.Models
{
	public class BigEndianConverter
	{
		public static byte[] GetBytes(int value)
		{
			return new[] {
				(byte)(value >> 24),
				(byte)(value >> 16),
				(byte)(value >> 8),
				(byte)value,
			};
		}

		public static byte[] GetBytes(uint value)
		{
			return GetBytes((int)value);
		}

		public static uint ToInt32(byte[] bytes)
		{
			return (uint)(bytes[0] << 24) + (uint)(bytes[1] << 16) + (uint)(bytes[2] << 8) + (uint)bytes[3];
		}

		public static ushort ToUInt16(byte[] bytes, int i)
		{
			return (ushort)((ushort)(bytes[i] << 8) + (ushort)bytes[i + 1]);
		}
	}

	public class ClientConnectInfo
	{
		public string static_IP { get; set; }
		public string Leased_IP { get; set; }
		public int Client { get; set; }

		public int? endpointId { get; set; }
		public string Name { get; set; }

		public string Switch { get; set; }
		public string Swith_adr { get; set; }
		public string swith_IP { get; set; }

		//public int? Module { get; set; }
		public int? PackageId { get; set; }

		public string Port { get; set; }
		public string Speed { get; set; }
		public bool Monitoring { get; set; }

        public DateTime LeaseBegin { get; set; }

	    public Int32 GetNormalSpeed()
		{
			return Convert.ToInt32(Speed) / 1000000;
		}
	}


    [ActiveRecord("PhysicalClients", Schema = "internet", Lazy = true), Auditable]
	public class PhysicalClients : ValidActiveRecordLinqBase<PhysicalClients>
	{

		[PrimaryKey]
		public virtual uint Id { get; set; }

        [Property, ValidateNonEmpty("Введите имя"), Auditable("Имя")]
		public virtual string Name { get; set; }

        [Property, ValidateNonEmpty("Введите фамилию"), Auditable("Фамилия")]
		public virtual string Surname { get; set; }

        [Property, ValidateNonEmpty("Введите отчество"), Auditable("Отчество")]
		public virtual string Patronymic { get; set; }

		[Property, Auditable("Город")]
		public virtual string City { get; set; }

		[Property]
		public virtual string Street { get; set; }

		[Property, ValidateInteger("Должно быть введено число")]
		public virtual string House { get; set; }

		[Property]
		public virtual string CaseHouse { get; set; }

        [Property, ValidateNonEmpty("Введите номер квартиры"), ValidateInteger("Должно быть введено число"), Auditable("Номер квартиры")]
		public virtual string Apartment { get; set; }

        [Property, ValidateNonEmpty("Введите номер подъезда"), ValidateInteger("Должно быть введено число"), Auditable("Номер подъезда")]
		public virtual string Entrance { get; set; }

        [Property, ValidateNonEmpty("Введите номер этажа"), ValidateInteger("Должно быть введено число"), Auditable("Этаж")]
		public virtual string Floor { get; set; }

		[
			Property,
			ValidateRegExp(@"^((\d{1})-(\d{3})-(\d{3})-(\d{2})-(\d{2}))", "Ошибка фотмата телефонного номера: мобильный телефн (8-***-***-**-**))"),
            ValidateNonEmpty("Введите номер телефона"), Auditable("Номер мобильного телефона")
		]
		public virtual string PhoneNumber { get; set; }

        [Property, ValidateRegExp(@"^((\d{4,5})-(\d{5,6}))", "Ошибка фотмата телефонного номера (Код города (4-5 цифр) + местный номер (5-6 цифр)"), Auditable("Номер домашнего телефона")]
		public virtual string HomePhoneNumber { get; set; }

        [Property, Auditable("Канал продаж")]
		public virtual string WhenceAbout { get; set; }

        [Property, ValidateRegExp(@"^(\d{4})?$", "Неправильный формат серии паспорта (4 цифры)"), UserValidateNonEmpty("Поле не должно быть пустым"), Auditable("Серия наспорта")]
		public virtual string PassportSeries { get; set; }

        [Property, ValidateRegExp(@"^(\d{6})?$", "Неправильный формат номера паспорта (6 цифр)"), UserValidateNonEmpty("Поле не должно быть пустым"), Auditable("Номер паспорта")]
		public virtual string PassportNumber { get; set; }

        [Property, UserValidateNonEmpty("Введите дату выдачи паспорта"), ValidateDate("Ошибка формата даты **-**-****"), Auditable("Дата выдачи паспорта")]
        public virtual DateTime PassportDate { get; set; }

        [Property, UserValidateNonEmpty("Заполните поле 'Кем выдан паспорт'"), Auditable("Кем выдан паспорт")]
		public virtual string WhoGivePassport { get; set; }

        [Property, UserValidateNonEmpty("Введите адрес регистрации"), Auditable("Адрес регистрации")]
		public virtual string RegistrationAdress { get; set; }

        [BelongsTo("Tariff", Cascade = CascadeEnum.SaveUpdate), Auditable("Тариф")]
		public virtual Tariff Tariff { get; set; }

        [Property, ValidateNonEmpty("Введите сумму"), ValidateDecimal("Неверно введено число")]
		public virtual decimal Balance { get; set; }

        [Property, ValidateIsUnique("Email должен быть уникальный"), ValidateEmail("Ошибка ввода (требуется adr@serv.dom)"), Auditable("Email")]
		public virtual string Email { get; set; }

		[Property]
		public virtual string Password { get; set; }

		[Property]
		public virtual bool ConnectionPaid { get; set; }

        [BelongsTo, Auditable("Дом")]
        public virtual House HouseObj { get; set; }

        [Property]
        public virtual DateTime DateOfBirth { get; set; }

		[Property, ValidateNonEmpty("Введите сумму"), ValidateDecimal("Непрвильно введено значение суммы")]
		public virtual decimal ConnectSum { get; set; }

		public virtual string HowManyToPay(bool change)
		{
			var format = change ? "({0})" : "{0}";
			if (Tariff == null)
			{
				return string.Empty;
			}
			else
			{
				var pay = Tariff.Price - Convert.ToDecimal(Balance);
				return string.Format(format, Convert.ToString(pay <= 0 ? 0 : pay));
			}
		}


		public static bool RegistrLogicClient(PhysicalClients _client, uint _tariff, uint house,
			ValidatorRunner validator)
		{
			if (validator.IsValid(_client))
			{
                var _house = Models.House.Find(house);;
			    _client.HouseObj = _house;
			    _client.Street = _house.Street;
			    _client.House = _house.Number.ToString();
			    _client.CaseHouse = _house.Case;
				_client.Tariff = Tariff.Find(_tariff);
				_client.Password = CryptoPass.GetHashString(_client.Password);
				_client.SaveAndFlush();
				return true;
			}
			return false;
		}

		public virtual decimal ToPay()
		{
			return Math.Abs(Balance) + Tariff.Price;
		}
	}
}