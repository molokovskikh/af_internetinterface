using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;
using InternetInterface.Models.Universal;

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

		public int endpointId { get; set; }
		public string Name { get; set; }

		public string Switch { get; set; }
		public string Swith_adr { get; set; }
		public string swith_IP { get; set; }

		public int? PackageId { get; set; }

		public string Port { get; set; }
		public string Speed { get; set; }
		public bool Monitoring { get; set; }

		public string ConnectSum { get; set; }

		public DateTime LeaseBegin { get; set; }

		public string GetNormalSpeed()
		{
			if (!string.IsNullOrEmpty(Speed))
				return PackageSpeed.GetNormalizeSpeed(Int32.Parse(Speed));
			return string.Empty;
		}

		public IList<StaticIp> GetStaticAdreses()
		{
			var endPoint = ClientEndpoints.TryFind((uint) endpointId);
			if (endPoint != null)
				return endPoint.StaticIps;
			return new List<StaticIp>();
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
		public virtual int? House { get; set; }

		[Property]
		public virtual string CaseHouse { get; set; }

		[Property, ValidateNonEmpty("Введите номер квартиры"), ValidateInteger("Должно быть введено число"), Auditable("Номер квартиры")]
		public virtual int? Apartment { get; set; }

		[Property, ValidateNonEmpty("Введите номер подъезда"), ValidateInteger("Должно быть введено число"), Auditable("Номер подъезда")]
		public virtual int? Entrance { get; set; }

		[Property, ValidateNonEmpty("Введите номер этажа"), ValidateInteger("Должно быть введено число"), Auditable("Этаж")]
		public virtual int? Floor { get; set; }

		[ValidateRegExp(@"^((\d{3})-(\d{7}))", "Ошибка фотмата телефонного номера: мобильный телефн (***-*******)")]
		public virtual string PhoneNumber { get; set; }

		[ValidateRegExp(@"^((\d{3})-(\d{7}))", "Ошибка фотмата телефонного номера (***-*******)")]
		public virtual string HomePhoneNumber { get; set; }

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

		[Property]
		public virtual decimal VirtualBalance { get; set; }

		[Property]
		public virtual decimal MoneyBalance { get; set; }

		[ValidateEmail("Ошибка ввода (требуется adr@serv.dom)")]
		public virtual string Email { get; set; }

		[Property]
		public virtual string Password { get; set; }

		[Property]
		public virtual bool ConnectionPaid { get; set; }

		[BelongsTo(Cascade = CascadeEnum.SaveUpdate), Auditable("Дом")]
		public virtual House HouseObj { get; set; }

		[Property]
		public virtual DateTime DateOfBirth { get; set; }

		[Property, ValidateNonEmpty("Введите сумму"), ValidateDecimal("Непрвильно введено значение суммы")]
		public virtual decimal ConnectSum { get; set; }

		[OneToOne(PropertyRef = "PhysicalClient")]
		public virtual Client Client { get; set; }

		public virtual string GetAdress()
		{
			return String.Format("ул. {0} д. {1} {2} кв. {3} Подъезд {4} Этаж {5}",
					Street,
					House,
					!String.IsNullOrEmpty(CaseHouse) ? " Корп " + CaseHouse : String.Empty,
					Apartment,
					Entrance,
					Floor);
		}

		public virtual string GetCutAdress()
		{
			return String.Format("ул. {0} д. {1} кв. {2}", Street, House, Apartment);
		}

		public virtual string HowManyToPay(bool change)
		{
			var format = change ? "({0})" : "{0}";
			if (Tariff == null)
			{
				return String.Empty;
			}
			else
			{
				var pay = Tariff.Price - Convert.ToDecimal(Balance);
				return String.Format(format, Convert.ToString(pay <= 0 ? 0 : pay));
			}
		}


		public static bool RegistrLogicClient(PhysicalClients client, uint tariffId, uint houseId,
			ValidatorRunner validator)
		{
			if (validator.IsValid(client))
			{
				var house = ActiveRecordBase<House>.Find(houseId);
				client.HouseObj = house;
				client.Street = house.Street;
				client.House = house.Number;
				client.CaseHouse = house.Case;
				client.Tariff = Tariff.Find(tariffId);
				client.Password = CryptoPass.GetHashString(client.Password);
				client.Save();
				return true;
			}
			return false;
		}

		public virtual WriteOff WriteOff(decimal sum)
		{
			if (sum <= 0)
				return null;

			Balance -= sum;
			var virtualAndMoneyParts = false;
			var virtualWriteOffs = false;
			var physicalPart = sum;
			if (VirtualBalance > 0) {
				if (VirtualBalance - sum >= 0) {
					VirtualBalance -= sum;
					virtualWriteOffs = true;
				}
				else {
					physicalPart = sum - VirtualBalance;
					MoneyBalance -= physicalPart;
					VirtualBalance -= sum - physicalPart;
					virtualAndMoneyParts = true;
				}
			}
			else {
				MoneyBalance -= physicalPart;
			}

			return new WriteOff {
				Client = Client,
				WriteOffDate = SystemTime.Now(),
				WriteOffSum = sum,
				MoneySum = !virtualWriteOffs ? physicalPart : 0m,
				VirtualSum = virtualAndMoneyParts ? sum - physicalPart : virtualWriteOffs ? sum : 0m,
				Sale = Client.Sale
			};
		}
	}
}