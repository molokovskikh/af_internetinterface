using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Audit;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Helpers;
using InternetInterface.Models.Services;
using InternetInterface.Models.Universal;
using NHibernate;

namespace InternetInterface.Models
{
	public class ClientConnectInfo
	{
		public string static_IP { get; set; }
		public string Leased_IP { get; set; }
		public int Client { get; set; }

		public int endpointId { get; set; }
		public int? ActualPackageId { get; set; }

		public string Name { get; set; }

		public string Switch { get; set; }
		public string Swith_adr { get; set; }
		public string swith_IP { get; set; }

		public int? PackageId { get; set; }

		public string Port { get; set; }
		public string Speed { get; set; }
		public bool Monitoring { get; set; }

		public decimal ConnectSum { get; set; }

		public DateTime LeaseBegin { get; set; }

		public string GetNormalSpeed()
		{
			if (!string.IsNullOrEmpty(Speed))
				return PackageSpeed.GetNormalizeSpeed(Int32.Parse(Speed));
			return string.Empty;
		}

		public IList<StaticIp> GetStaticAdreses()
		{
			var endPoint = ClientEndpoint.TryFind((uint)endpointId);
			if (endPoint != null)
				return endPoint.StaticIps;
			return new List<StaticIp>();
		}

		public virtual string ForSearchName(string query)
		{
			return TextHelper.SelectQuery(query, Name);
		}
	}


	[ActiveRecord("PhysicalClients", Schema = "internet", Lazy = true), Auditable]
	public class PhysicalClient : ValidActiveRecordLinqBase<PhysicalClient>
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

		[ValidateRegExp(@"^((\d{3})-(\d{7}))", "Ошибка формата телефонного номера: мобильный телефн (***-*******)")]
		public virtual string PhoneNumber { get; set; }

		[ValidateRegExp(@"^((\d{3})-(\d{7}))", "Ошибка формата телефонного номера (***-*******)")]
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

		[BelongsTo("Tariff", Cascade = CascadeEnum.SaveUpdate), Description("Тариф"), Auditable]
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

		[BelongsTo(Cascade = CascadeEnum.SaveUpdate), Auditable("Дом")]
		public virtual House HouseObj { get; set; }

		[Property]
		public virtual DateTime DateOfBirth { get; set; }

		[Property, ValidateNonEmpty("Введите сумму"), ValidateDecimal("Непрвильно введено значение суммы")]
		public virtual decimal ConnectSum { get; set; }

		[OneToOne(PropertyRef = "PhysicalClient")]
		public virtual Client Client { get; set; }

		[ValidateSelf]
		public virtual void Validate(ErrorSummary errors)
		{
			var internet = Client.Internet;
			if (internet.ActivatedByUser && Tariff == null) {
				errors.RegisterErrorMessage("Tariff", "Нужно выбрать тариф");
			}
		}

		public virtual string GetAdress()
		{
			return String.Format("{0} Подъезд {1} Этаж {2}",
				GetCutAdress(),
				Entrance,
				Floor);
		}

		public virtual string GetCutAdress()
		{
			return String.Format("ул. {0} д. {1}{2} кв. {3}",
				Street,
				House,
				!String.IsNullOrEmpty(CaseHouse) ? " Корп " + CaseHouse : String.Empty,
				Apartment);
		}

		public static bool RegistrLogicClient(PhysicalClient client, uint houseId,
			ValidatorRunner validator)
		{
			if (validator.IsValid(client)) {
				var house = ActiveRecordBase<House>.Find(houseId);
				client.HouseObj = house;
				client.Street = house.Street;
				client.House = house.Number;
				client.CaseHouse = house.Case;
				client.Password = CryptoPass.GetHashString(client.Password);
				client.Save();
				return true;
			}
			return false;
		}

		public virtual WriteOff WriteOff(decimal sum, bool writeoffVirtualFirst = true)
		{
			var writeoff = CalculateWriteoff(sum, writeoffVirtualFirst);

			if (writeoff == null)
				return null;

			Balance -= writeoff.WriteOffSum;
			VirtualBalance -= writeoff.VirtualSum;
			MoneyBalance -= writeoff.MoneySum;

			return writeoff;
		}

		public virtual WriteOff CalculateWriteoff(decimal sum, bool writeoffVirtualFirst = true)
		{
			if (sum <= 0)
				return null;

			decimal virtualWriteoff;
			decimal moneyWriteoff;

			if (writeoffVirtualFirst) {
				virtualWriteoff = Math.Min(sum, VirtualBalance);
			}
			else {
				virtualWriteoff = Math.Min(Math.Abs(Math.Min(MoneyBalance - sum, 0)), VirtualBalance);
			}
			moneyWriteoff = sum - virtualWriteoff;

			return new WriteOff {
				Client = Client,
				WriteOffDate = SystemTime.Now(),
				WriteOffSum = sum,
				MoneySum = moneyWriteoff,
				VirtualSum = virtualWriteoff,
				Sale = Client.Sale,
				BeforeWriteOffBalance = Client.Balance
			};
		}

		public virtual void UpdatePackageId()
		{
			Client.Endpoints.Each(UpdatePackageId);
		}

		public virtual void UpdatePackageId(ClientEndpoint clientEndpoint)
		{
			if (Tariff != null && Client.Internet.ActivatedByUser)
				clientEndpoint.PackageId = Tariff.PackageId;
			else
				clientEndpoint.PackageId = null;
		}

		public virtual void WriteOffIfTariffChanged(List<TariffChangeRule> rules)
		{
			if (!this.IsChanged(c => c.Tariff))
				return;

			if (Tariff == null)
				return;

			var oldTariff = this.OldValue(c => c.Tariff);
			if (oldTariff == null)
				return;

			var rule = rules.FirstOrDefault(r => r.FromTariff == oldTariff && r.ToTariff == Tariff);
			if (rule == null || rule.Price == 0)
				return;

			var comment = String.Format("Изменение тарифа, старый '{0}' новый '{1}'", oldTariff.Name, Tariff.Name);
			Client.UserWriteOffs.Add(new UserWriteOff(Client, rule.Price, comment, false));
		}
	}
}