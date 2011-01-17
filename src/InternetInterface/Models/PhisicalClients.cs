using System;
using System.Collections;
using System.Collections.Generic;
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
	public class PhisicalClientConnectInfo
	{
		public string static_IP { get; set; }
		public string Leased_IP { get; set; }
		public string Client { get; set; }
		public string Switch { get; set; }
		public string Swith_adr { get; set; }
		public string swith_IP { get; set; }
		public string Port { get; set; }
		public string Speed { get; set; }
		public bool Monitoring { get; set; }
	}


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

		[BelongsTo("Status")]
		public virtual Status Status { get; set; }

		[Property]
		public virtual bool Connected { get; set; }

		public virtual bool IsConnected()
		{
			return this.Connected;
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

		public static bool RegistrLogicClient(PhisicalClients _client, uint _tariff, uint _status,
			ValidatorRunner validator, Partner hasRegistered, PaymentForConnect connectSumm)
		{
			if (validator.IsValid(_client) && validator.IsValid(connectSumm))
			{
				_client.RegDate = DateTime.Now;
				_client.Tariff = Tariff.Find(_tariff);
				_client.Status = Status.Find(_status);
				_client.Password = CryptoPass.GetHashString(_client.Password);
				_client.HasRegistered = hasRegistered;
				_client.SaveAndFlush();
				connectSumm.ClientId = _client;
				connectSumm.ManagerID = InithializeContent.partner;
				connectSumm.PaymentDate = DateTime.Now;
				connectSumm.SaveAndFlush();
				return true;
			}
			return false;
		}

		public virtual PhisicalClientConnectInfo GetConnectInfo()
		{
			if (Connected)
			{
				var client = Clients.FindAllByProperty("PhisicalClient", (uint)Id);
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

					return ConnectInfo[0];
				}
			}
			return null;
		}
	}

}