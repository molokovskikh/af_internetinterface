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

	public class PhisicalClientConnectInfo
	{
		public string static_IP { get; set; }
		public string Leased_IP { get; set; }
		public string Client { get; set; }

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

		public Int32 GetNormalSpeed()
		{
			return Convert.ToInt32(Speed) / 1000000;
		}
	}


	[ActiveRecord("PhysicalClients", Schema = "internet", Lazy = true)]
	public class PhysicalClients : ValidActiveRecordLinqBase<PhysicalClients>
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

		[Property, ValidateRegExp(@"^(\d{4})?$", "Неправильный формат серии паспорта (4 цифры)"), UserValidateNonEmpty("Поле не должно быть пустым")]
		public virtual string PassportSeries { get; set; }

		[Property, ValidateRegExp(@"^(\d{6})?$", "Неправильный формат номера паспорта (6 цифр)"), UserValidateNonEmpty("Поле не должно быть пустым")]
		public virtual string PassportNumber { get; set; }

		[Property, UserValidateNonEmpty("Введите дату выдачи паспорта"), ValidateDate("Ошибка формата даты **-**-****")]
		public virtual string PassportDate { get; set; }

		[Property, UserValidateNonEmpty("Заполните поле 'Кем выдан паспорт'")]
		public virtual string WhoGivePassport { get; set; }

		[Property, UserValidateNonEmpty("Введите адрес регистрации")]
		public virtual string RegistrationAdress { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[BelongsTo("Tariff")]
		public virtual Tariff Tariff { get; set; }

		[Property, ValidateNonEmpty("Введите сумму"), ValidateDecimal("Неверно введено число")]
		public virtual decimal Balance { get; set; }

		[Property, ValidateIsUnique("Email должен быть уникальный"), ValidateEmail("Ошибка ввода (требуется adr@serv.dom)")]
		public virtual string Email { get; set; }

		[Property]
		public virtual string Password { get; set; }

		[BelongsTo("WhoRegistered")]
		public virtual Partner WhoRegistered { get; set; }

		[Property]
		public virtual string WhoRegisteredName { get; set; }

		[BelongsTo("WhoConnected")]
		public virtual Brigad WhoConnected { get; set; }

		[Property]
		public virtual string WhoConnectedName { get; set; }

		[BelongsTo("Status")]
		public virtual Status Status { get; set; }

		/*[Property]
		public virtual bool Connected { get; set; }*/

		[Property, ValidateNonEmpty("Введите сумму"), ValidateDecimal("Непрвильно введено значение суммы")]
		public virtual decimal ConnectSum { get; set; }

		[Property]
		public virtual DateTime ConnectedDate { get; set; }

		[Property]
		public virtual bool ConnectionPaid { get; set; }

		[Property]
		public virtual bool AutoUnblocked { get; set; }

		[HasMany(ColumnKey = "Client", OrderBy = "PaidOn")]
		public virtual IList<Payment> Payments { get ; set; }

		public virtual bool IsConnected()
		{
			return Status.Connected;
		}

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


		public static bool RegistrLogicClient(PhysicalClients _client, uint _tariff, uint _status,
			ValidatorRunner validator, Partner hasRegistered/*, PaymentForConnect connectSumm*/)
		{
			if (validator.IsValid(_client)/* && validator.IsValid(connectSumm)*/)
			{
				_client.RegDate = DateTime.Now;
				_client.Tariff = Tariff.Find(_tariff);
				_client.Status = Status.Find((uint)StatusType.BlockedAndNoConnected);
				_client.Password = CryptoPass.GetHashString(_client.Password);
				_client.WhoRegistered = hasRegistered;
				_client.WhoRegisteredName = hasRegistered.Name;
				_client.AutoUnblocked = true;
				_client.SaveAndFlush();
				/*connectSumm.ClientId = _client;
				connectSumm.ManagerID = InithializeContent.partner;
				connectSumm.PaymentDate = DateTime.Now;
				connectSumm.SaveAndFlush();*/
				return true;
			}
			return false;
		}

		public virtual decimal ToPay()
		{
			return Math.Abs(Balance) + Tariff.Price;
		}

		public  static Lease FindByIP(string ip)
		{
			var addressValue = BigEndianConverter.ToInt32(IPAddress.Parse(ip).GetAddressBytes());
			return Lease.Queryable.FirstOrDefault(l => l.Ip == addressValue);
		}

		public virtual PhisicalClientConnectInfo GetConnectInfo()
		{
			if (Status != null && Status.Connected)
			{
				var client = Clients.FindAllByProperty("PhysicalClient", this);
				if (client.Length != 0)
				{
					IList<PhisicalClientConnectInfo> ConnectInfo = new List<PhisicalClientConnectInfo>();
					ARSesssionHelper<PhisicalClientConnectInfo>.QueryWithSession(session =>
					                                                             	{
					                                                             		var query =
					                                                             			session.CreateSQLQuery(string.Format(
																								@"
select
inet_ntoa(CE.Ip) as static_IP,
inet_ntoa(L.Ip) as Leased_IP,
CE.Client,
Ce.Switch,
NS.Name as Swith_adr,
inet_ntoa(NS.ip) as swith_IP,
CE.Port,
PS.Speed,
CE.Monitoring
from internet.ClientEndpoints CE
join internet.NetworkSwitches NS on NS.Id = CE.Switch
join internet.Clients C on CE.Client = C.Id
left join internet.Leases L on L.Endpoint = CE.Id
left join internet.PackageSpeed PS on PS.PackageId = CE.PackageId
where CE.Client = {0}",
					 				client[0].Id)).SetResultTransformer(
					 				new AliasToPropertyTransformer(
					 					typeof (PhisicalClientConnectInfo)))
					 				.List<PhisicalClientConnectInfo>();
					 		ConnectInfo = query;
							return query;
																					});
					if (ConnectInfo.Count != 0)
					return ConnectInfo.First();
				}
			}
			return new PhisicalClientConnectInfo();
		}
	}

}