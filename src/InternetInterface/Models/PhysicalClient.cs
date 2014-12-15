using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.Models.Audit;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Helpers;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	public class ClientOrderInfo
	{
		public Order Order { get; set; }
		public ClientConnectInfo ClientConnectInfo { get; set; }
	}

	public class ClientConnectInfo
	{
		public string static_IP { get; set; }
		public string Leased_IP { get; set; }
		public int Client { get; set; }

		public int endpointId { get; set; }
		public int? ActualPackageId { get; set; }

		public string Name { get; set; }

		public uint? Pool { get; set; }

		public string Switch { get; set; }
		public string Swith_adr { get; set; }
		public string swith_IP { get; set; }

		public int? PackageId { get; set; }

		public string Port { get; set; }
		public string Speed { get; set; }
		public bool Monitoring { get; set; }
		public string WhoConnected { get; set; }

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
			ClientEndpoint endPoint = ClientEndpoint.TryFind((uint) endpointId);
			if (endPoint != null)
				return endPoint.StaticIps;
			return new List<StaticIp>();
		}

		public virtual string ForSearchName(string query)
		{
			return TextHelper.SelectQuery(query, Name);
		}
	}

	public enum IdDocType
	{
		[Description("Паспорт РФ")] LocalId,
		[Description("Иной документ")] Over
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

		[Property, ValidateNonEmpty("Введите номер квартиры"), Auditable("Номер квартиры")]
		public virtual string Apartment { get; set; }

		[Property]
		public virtual string Additional { get; set; }

		[Property, ValidateNonEmpty("Введите номер подъезда"), ValidateInteger("Должно быть введено число"),
		 Auditable("Номер подъезда")]
		public virtual int? Entrance { get; set; }

		[Property, ValidateNonEmpty("Введите номер этажа"), ValidateInteger("Должно быть введено число"), Auditable("Этаж")]
		public virtual int? Floor { get; set; }

		[ValidateNonEmpty(RunWhen = RunWhen.Insert),
		 ValidateRegExp(@"^((\d{3})-(\d{7}))", "Ошибка формата телефонного номера: мобильный телефон (000-0000000)")]
		public virtual string PhoneNumber { get; set; }

		[ValidateRegExp(@"^((\d{3})-(\d{7}))", "Ошибка формата телефонного номера (***-*******)")]
		public virtual string HomePhoneNumber { get; set; }

		[Property, Description("Документ удостоверяющий личность"), Auditable]
		public virtual IdDocType IdDocType { get; set; }

		[Property, Description("Название документа удостоверяющего личность"), Auditable]
		public virtual string IdDocName { get; set; }

		[Property, UserValidateNonEmpty("Поле не должно быть пустым"), Auditable("Серия паспорта")]
		public virtual string PassportSeries { get; set; }

		[Property, UserValidateNonEmpty("Поле не должно быть пустым"), Auditable("Номер паспорта")]
		public virtual string PassportNumber { get; set; }

		[Property, UserValidateNonEmpty("Введите дату выдачи паспорта"), ValidateDate("Ошибка формата даты **-**-****"),
		 Auditable("Дата выдачи паспорта")]
		public virtual DateTime? PassportDate { get; set; }

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
		public virtual DateTime? DateOfBirth { get; set; }

		[Property, ValidateNonEmpty("Введите сумму"), ValidateDecimal("Неправильно введено значение суммы")]
		public virtual decimal ConnectSum { get; set; }

		[Property, Description("Номер абонента Ситилайн"), ValidateIsUnique("Абонент с таким номером уже зарегистрирован")]
		public virtual int? ExternalClientId { get; set; }

		[Property, Auditable("Проверен")]
		public virtual bool Checked { get; set; }

		//внешний номер договора обязателен только если клиент регистрируется самостоятельно
		public virtual bool ExternalClientIdRequired { get; set; }

		[OneToOne(PropertyRef = "PhysicalClient")]
		public virtual Client Client { get; set; }

		public virtual string SafeIdDocName
		{
			get
			{
				if (IdDocType == IdDocType.LocalId)
					return "Паспорт";
				return IdDocName;
			}
		}

		[ValidateSelf]
		public virtual void Validate(ErrorSummary errors)
		{
			var internet = Client.Internet;
			if (internet.ActivatedByUser && Tariff == null) {
				errors.RegisterErrorMessage("Tariff", "Нужно выбрать тариф");
			}

			if (ExternalClientIdRequired && ExternalClientId == null) {
				errors.RegisterErrorMessage("ExternalClientId", "Нужно указать номер абонента");
			}

			if (Client.Status.Type != StatusType.Dissolved
			    && (Client.AdditionalStatus == null || Client.AdditionalStatus.Id != (uint) AdditionalStatusType.Refused)) {
				if (HouseObj == null)
					errors.RegisterErrorMessage("HouseObj", "Нужно выбрать дом");
			}

			if (IdDocType == IdDocType.LocalId) {
				if (!new RegularExpressionValidator(@"^(\d{6})?$").IsValid(this, PassportNumber)) {
					errors.RegisterErrorMessage("PassportNumber", "Неправильный формат номера паспорта (6 цифр)");
				}
				if (!new RegularExpressionValidator(@"^(\d{4})?$").IsValid(this, PassportSeries)) {
					errors.RegisterErrorMessage("PassportSeries", "Неправильный формат серии паспорта (4 цифры)");
				}
			}
			else if (!new NonEmptyValidator().IsValid(this, IdDocName)) {
				errors.RegisterErrorMessage("IdDocName", "Заполнение поля обязательно");
			}
		}

		public virtual string GetFullAddress()
		{
			return String.Format("улица {0} дом {1}{2} квартира {3} подъезд {4} этаж {5} {6}",
				Street, House, String.IsNullOrEmpty(CaseHouse) ? "" : " корпус " + CaseHouse,
				Apartment, Entrance, Floor, Additional);
		}

		public virtual string GetAdress()
		{
			return String.Format("{0} Подъезд {1} Этаж {2} {3}",
				GetShortAdress(),
				Entrance,
				Floor, Additional);
		}

		public virtual string GetShortAdress()
		{
			if (String.IsNullOrEmpty(Street))
				return "";

			return String.Format("ул. {0} д. {1}{2} кв. {3}",
				Street,
				House,
				!String.IsNullOrEmpty(CaseHouse) ? " Корп " + CaseHouse : String.Empty,
				Apartment);
		}

		public virtual void UpdateHouse(House house)
		{
			if (house == null)
				return;
			HouseObj = house;
			Street = house.Street;
			House = house.Number;
			CaseHouse = house.Case;
		}

		public virtual WriteOff WriteOff(decimal sum, bool writeoffVirtualFirst = false)
		{
			var writeoff = CalculateWriteoff(sum, writeoffVirtualFirst);

			if (writeoff == null)
				return null;

			Balance -= writeoff.WriteOffSum;
			VirtualBalance -= writeoff.VirtualSum;
			MoneyBalance -= writeoff.MoneySum;

			return writeoff;
		}

		public virtual WriteOff CalculateWriteoff(decimal sum, bool writeoffVirtualFirst = false)
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
				WriteOffSum = Math.Round(sum, 2),
				MoneySum = Math.Round(moneyWriteoff, 2),
				VirtualSum = Math.Round(virtualWriteoff, 2),
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
			Client.UserWriteOffs.Add(new UserWriteOff(Client, rule.Price, comment));
		}

		public virtual void AccountPayment(Payment newPayment)
		{
			Balance += Convert.ToDecimal(newPayment.Sum);
			if (newPayment.Virtual) {
				VirtualBalance += newPayment.Sum;
				if (Balance > 0 && Client.Disabled)
					Client.Enable();
			}
			else {
				MoneyBalance += newPayment.Sum;
			}
		}

		public virtual Payment CalculateSelfRegistrationPayment()
		{
			//По требованию #18207 Было сделано 3 дня
			const int days = 3;
			var dayInMonth = (DateTime.Today.LastDayOfMonth() - DateTime.Today.FirstDayOfMonth()).Days + 1;
			var sum = (Client.GetPriceIgnoreDisabled()/dayInMonth)*days;
			var payment = new Payment(Client, sum) {
				Virtual = true,
				Comment = "Бонус при самостоятельной регистрации"
			};
			return payment;
		}

		public virtual void AfterSave()
		{
			if (HouseObj == null)
				return;

			if (HouseObj.Region != null
			    && HouseObj.Region.IsExternalClientIdMandatory
			    && ExternalClientId == null) {
				ExternalClientId = (int?) Client.Id;
			}
		}

		public virtual bool CanChangeTariff(Tariff tariff, ErrorSummary sum)
		{
			if (Client.Iptv != null && Client.Iptv.IsActivated && !tariff.Iptv) {
				sum.RegisterErrorMessage("tariff", "Переход на этот тариф не возможен с включенной услугой IPTV");
				return false;
			}
			return true;
		}
	}
}