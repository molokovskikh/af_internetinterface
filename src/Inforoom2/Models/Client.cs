using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Common.Tools;
using Inforoom2.Intefaces;
using Inforoom2.Models.Services;
using Inforoom2.validators;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Util;
using NHibernate.Validator.Cfg.MappingSchema;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель пользователя
	/// </summary>
	[Class(0, Table = "Clients", Schema = "internet", NameType = typeof(Client)), Description("Клиент")]
	public class Client : BaseModel, ILogAppeal, IServicemenScheduleItem
	{
		public Client()
		{
			Endpoints = new List<ClientEndpoint>();
			ClientServices = new List<ClientService>();
			Payments = new List<Payment>();
			ServicemenScheduleItems = new List<ServicemenScheduleItem>();
			Contacts = new List<Contact>();
			UserWriteOffs = new List<UserWriteOff>();
			WriteOffs = new List<WriteOff>();
			Appeals = new List<Appeal>();
			RentalHardwareList = new List<ClientRentalHardware>();

			/// из старой админки. 
			Disabled = true;
			SendSmsNotification = true;
			PercentBalance = 0.8m;
			FreeBlockDays = 28;
			CreationDate = DateTime.Now;
			/// задано по результатам анализа изменений в БД "регистрацией клиента" старой админки.
			BlockDate = DateTime.Now;
			YearCycleDate = DateTime.Now;
		}


		[Bag(0, Table = "ServicemenScheduleItems")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "Client")]
		[OneToMany(2, ClassType = typeof(ServicemenScheduleItem))]
		public virtual IList<ServicemenScheduleItem> ServicemenScheduleItems { get; set; }

		public virtual ServicemenScheduleItem ConnectionRequest
		{
			get { return ServicemenScheduleItems != null ? ServicemenScheduleItems.FirstOrDefault(s => s.RequestType == ServicemenScheduleItem.Type.ClientConnectionRequest) : null; }
			set { }
		}

		[Property, Description("Перенесено из старой админки")]
		public virtual DateTime ConnectedDate { get; set; }

		[Property, Description("Перенесено из старой админки (в старом проекте ему ничего не присваивается.)")]
		public virtual DateTime? BlockDate { get; set; }

		[Property(Column = "RegDate"), Description("Дата регистрации клиента")]
		public virtual DateTime? CreationDate { get; set; }

		[Property]
		public virtual bool Disabled { get; set; }

		[Property(NotNull = true)]
		public virtual int DebtDays { get; set; }

		///TODO: не использовать !!!!!!!
		[Property(NotNull = true)]
		public virtual ClientType Type { get; set; }

		[Property(NotNull = true)]
		public virtual bool ShowBalanceWarningPage { get; set; }

		[Property(Column = "Sale", NotNull = true)]
		public virtual int Discount { get; set; }

		[Property(NotNull = true)]
		public virtual bool AutoUnblocked { get; set; }

		[Property(NotNull = true)]
		public virtual bool DebtWork { get; set; }

		[Property(NotNull = true)]
		public virtual decimal PercentBalance { get; set; }

		[Property(NotNull = true)]
		public virtual bool PaidDay { get; set; }

		[Property(NotNull = true), Description("Бесплатные дни добровольной блокировки")]
		public virtual int FreeBlockDays { get; set; }

		[Property(NotNull = true, Column = "FirstLunch")]
		public virtual bool Lunched { get; set; }

		[Property(NotNull = false)]
		public virtual string Comment { get; set; }

		[Property]
		public virtual DateTime? StartNoBlock { get; set; }

		[Property]
		public virtual DateTime? RatedPeriodDate { get; set; }

		[Property]
		[DataType(DataType.Date)]
		public virtual DateTime? StatusChangedOn { get; set; }

		[Property(Column = "BeginWork"), Description("Дата первой аренды -проставляется DHCP-сервером во время получения клиентом первой аренды")]
		public virtual DateTime? WorkingStartDate { get; set; }

		[Property, Description("Дата, по которой определяется когда бесплатные дни должны обновиться")]
		public virtual DateTime? YearCycleDate { get; set; }

		[ManyToOne(Cascade = "save-update"), Description("Статус")]
		public virtual Status Status { get; set; }

		[ManyToOne(Column = "PhysicalClient", Cascade = "save-update")]
		public virtual PhysicalClient PhysicalClient { get; set; }

		[ManyToOne(Column = "LawyerPerson", Cascade = "save-update"), Description("Юр. лицо")]
		public virtual LegalClient LegalClient { get; set; }

		[Bag(0, Table = "ClientServices", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "Client")]
		[OneToMany(2, ClassType = typeof(ClientService))]
		public virtual IList<ClientService> ClientServices { get; set; }

		[Bag(0, Table = "Payments", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "Client")]
		[OneToMany(2, ClassType = typeof(Payment))]
		public virtual IList<Payment> Payments { get; set; }

		[Bag(0, Table = "ClientEndpoints", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "client")]
		[OneToMany(2, ClassType = typeof(ClientEndpoint))]
		public virtual IList<ClientEndpoint> Endpoints { get; set; }

		[Bag(0, Table = "Contacts", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "client")]
		[OneToMany(2, ClassType = typeof(Contact)), ValidatorContacts]
		public virtual IList<Contact> Contacts { get; set; }

		[Bag(0, Table = "UserWriteOffs", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "Client")]
		[OneToMany(2, ClassType = typeof(UserWriteOff))]
		public virtual IList<UserWriteOff> UserWriteOffs { get; set; }

		[Bag(0, Table = "WriteOff", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "Client")]
		[OneToMany(2, ClassType = typeof(WriteOff))]
		public virtual IList<WriteOff> WriteOffs { get; set; }

		[Bag(0, Table = "Appeals", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "Client")]
		[OneToMany(2, ClassType = typeof(Appeal))]
		public virtual IList<Appeal> Appeals { get; set; }

		[Bag(0, Table = "ClientRentalHardware", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "Client")]
		[OneToMany(2, ClassType = typeof(ClientRentalHardware))]
		public virtual IList<ClientRentalHardware> RentalHardwareList { get; set; }

		[Property(Column = "SendSmsNotifocation"), Description("СМС уведомление")]
		public virtual bool SendSmsNotification { get; set; }

		[ManyToOne(Column = "WhoRegistered", Cascade = "save-update")]
		public virtual Employee WhoRegistered { get; set; }

		[Property(Column = "WhoRegisteredName")]
		public virtual string WhoRegisteredName { get; set; }

		[ManyToOne(Column = "Agent", Cascade = "save-update")]
		public virtual Agent Agent { get; set; }

		public virtual bool IsNeedRecofiguration { get; set; }

		public virtual bool IsWorkStarted()
		{
			return WorkingStartDate != null;
		}

		public virtual bool IsPhysicalClient
		{
			get { return PhysicalClient == null; }
		}

		public virtual ClientService Internet
		{
			get { return ClientServices.First(s => NHibernateUtil.GetClass(s.Service) == typeof(Internet)); }
		}

		public virtual bool HasActiveService(Service service)
		{
			return ClientServices.FirstOrDefault(cs => cs.Service.Id == service.Id && cs.IsActivated) != null;
		}

		public virtual ClientService FindActiveService<T>()
		{
			return ClientServices.FirstOrDefault(c => c.IsActivated && NHibernateUtil.GetClass(c.Service) == typeof(T));
		}

		public virtual bool HasActiveService<T>()
		{
			return FindActiveService<T>() != null;
		}

		/// <summary>
		/// Метод для проверки, арендовано ли оборудование типа hwType у клиента
		/// </summary>
		public virtual bool HardwareIsRented(RentalHardware hw)
		{
			return RentalHardwareList.ToList().Exists(rh => rh.Hardware == hw && rh.IsActive);
		}

		/// <summary>
		/// Метод получения у клиента текущей услуги "Аренда оборудования" 
		/// </summary>
		public virtual ClientRentalHardware GetActiveRentalHardware(RentalHardware hw)
		{
			var thisHardware = RentalHardwareList.Where(rh => rh.Hardware == hw && rh.IsActive).ToList();
			return thisHardware.OrderBy(h => h.BeginDate).LastOrDefault();
		}

		public virtual bool CanUseService(Service service)
		{
			return service.IsActivableFor(this);
		}

		public virtual decimal GetInterval()
		{
			return (((DateTime)RatedPeriodDate).AddMonths(1) - (DateTime)RatedPeriodDate).Days + DebtDays;
		}

		/// <summary>
		/// Получить число дней работы клиента при текущем балансе до авт. блокировки (в биллинге не использовать!)
		/// </summary>
		/// <returns>Расчётное кол-во дней работы без пополнения баланса</returns>
		public virtual int GetWorkDays()
		{
			var priceInDay = Plan.Price / DateTime.Now.DaysInMonth(); // ToDo Улучшить алгоритм вычисления
			return (int)Math.Floor(Balance / priceInDay);
		}

		public virtual decimal GetSumForRegularWriteOff()
		{
			var daysInInterval = GetInterval();
			var price = GetPrice();
			return Math.Round(price / daysInInterval, 2);
		}

		public virtual decimal GetPrice()
		{
			var services = ClientServices.Where(c => c.IsActivated).ToArray();
			var blockingService = services.FirstOrDefault(c => c.Service.BlockingAll);
			if (blockingService != null)
				return blockingService.GetPrice() + services.Where(c => c.Service.ProcessEvenInBlock).Sum(c => c.GetPrice());

			return services.Sum(c => c.GetPrice());
		}

		/// <summary>
		/// Формирует итоговую цену Интернета за месяц по данному тарифному плану
		/// </summary>
		public virtual decimal GetTariffPrice(bool isBlocked = false)
		{
			if (PhysicalClient.Plan == null || (!isBlocked && (WorkingStartDate == null || Disabled)))
				return 0;

			var prePrice = AccountDiscounts(PhysicalClient.Plan.Price);
			var finalPrice = AccountDiscounts(PhysicalClient.Plan.FinalPrice);
			if ((PhysicalClient.Plan.FinalPriceInterval == 0 || PhysicalClient.Plan.FinalPrice == 0))
				return prePrice;

			if (WorkingStartDate != null && WorkingStartDate.Value.AddMonths(PhysicalClient.Plan.FinalPriceInterval) <= SystemTime.Now())
				return finalPrice;
			return prePrice;
		}

		/// <summary>
		/// Формирует итоговую цену для разблокировки клиента
		/// </summary>
		public virtual decimal GetUnlockPrice()
		{
			var sum = 0m; // Сумма для разблокировки
			if (Internet.ActivatedByUser)
				sum += GetTariffPrice(true);

			return (sum - Balance);
		}

		/// <summary>
		/// Применяет скидку клиента к некоторой цене price
		/// </summary>
		private decimal AccountDiscounts(decimal price)
		{
			if (Discount > 0)
				price *= 1 - Discount / 100;
			return price;
		}

		public virtual void SetStatus(StatusType status, ISession session)
		{
			SetStatus(session.Load<Status>((Int32)status));
		}

		public virtual void SetStatus(Status status)
		{
			if (status.Type == StatusType.VoluntaryBlocking) {
				Disabled = true;
				DebtDays = 0;
				AutoUnblocked = false;
			}
			else if (status.Type == StatusType.NoWorked) {
				Disabled = true;
				Discount = 0;
				StartNoBlock = null;
				AutoUnblocked = true;
			}
			else if (status.Type == StatusType.Worked) {
				Disabled = false;
				//если мы возобновили работу после поломки то дата начала периода тарификации не должна изменяться
				//если ее сбросить списания начнутся только когда клиент получит аренду
				if (Status.Type != StatusType.BlockedForRepair)
					RatedPeriodDate = null;
				DebtDays = 0;
				ShowBalanceWarningPage = false;
			}
			else if (status.Type == StatusType.BlockedForRepair) {
				Disabled = true;
				AutoUnblocked = false;
			}
			else if (status.Type == StatusType.Dissolved) {
				Discount = 0;
			}

			if (Status.Type != status.Type) {
				StatusChangedOn = DateTime.Now;
			}
			Status = status;
		}

		public virtual decimal Balance
		{
			get { return PhysicalClient != null ? PhysicalClient.Balance : 0; }
			set { PhysicalClient.Balance = value; }
		}

		public virtual string PhoneNumber
		{
			get { return PhysicalClient != null ? PhysicalClient.PhoneNumber : null; }
			// Контакты находятся в отдельной таблице
			//set { PhysicalClient.PhoneNumber = value; }
		}

		public virtual string Email
		{
			get { return PhysicalClient != null ? PhysicalClient.Email : null; }
			// Контакты находятся в отдельной таблице
			//set { PhysicalClient.Email = value; }
		}

		// TODO: нужно ли оно ???
		[Property(Column = "Name")]
		public virtual string _Name { get; set; }

		public virtual string Name
		{
			get { return PhysicalClient != null ? PhysicalClient.Name : _Name; }
			set { PhysicalClient.Name = value; }
		}

		public virtual string Surname
		{
			get { return PhysicalClient != null ? PhysicalClient.Surname : null; }
			set { PhysicalClient.Surname = value; }
		}

		public virtual string Patronymic
		{
			get { return PhysicalClient != null ? PhysicalClient.Patronymic : null; }
			set { PhysicalClient.Patronymic = value; }
		}

		public virtual Address Address
		{
			get { return PhysicalClient != null ? PhysicalClient.Address : null; }
		}

		public virtual DateTime LastTimePlanChanged
		{
			get { return PhysicalClient != null ? PhysicalClient.LastTimePlanChanged : DateTime.MinValue; }
		}

		public virtual Plan Plan
		{
			get { return PhysicalClient != null ? PhysicalClient.Plan : null; }
		}

		public virtual void WriteOff(decimal sum, bool isVirtual)
		{
			if (PhysicalClient != null)
				PhysicalClient.WriteOff(sum, isVirtual);
			//else
			//LawyerPerson.Balance -= sum;
		}

		public virtual Region GetRegion()
		{
			if (PhysicalClient != null) {
				if (PhysicalClient.Address != null) {
					if (PhysicalClient.Address != null) {
						return PhysicalClient.Address.House.Region ?? PhysicalClient.Address.House.Street.Region;
					}
				}
			}
			if (LegalClient != null) {
				return LegalClient.Region;
			}
			return null;
		}

		public virtual bool HasPassportData()
		{
			if (PhysicalClient == null)
				return true;
			var hasPassportData = !string.IsNullOrEmpty(PhysicalClient.PassportNumber);
			if (PhysicalClient.CertificateType == CertificateType.Passport) {
				hasPassportData = hasPassportData && !string.IsNullOrEmpty(PhysicalClient.PassportSeries);
				hasPassportData = hasPassportData && !string.IsNullOrEmpty(PhysicalClient.PassportResidention);
			}
			else
				hasPassportData = hasPassportData && !string.IsNullOrEmpty(PhysicalClient.CertificateName);

			hasPassportData = hasPassportData && PhysicalClient.PassportDate != DateTime.MinValue;
			hasPassportData = hasPassportData && PhysicalClient.BirthDate != DateTime.MinValue;
			hasPassportData = hasPassportData && !string.IsNullOrEmpty(PhysicalClient.RegistrationAddress);
			return hasPassportData;
		}

		public static Client GetClientForIp(string ipstr, ISession dbSession)
		{
			var endpoint = ClientEndpoint.GetEndpointForIp(ipstr, dbSession);
			if (endpoint != null)
				return endpoint.Client;
			return null;
		}

		//todo исправить
		[Property(Column = "Address")]
		public virtual string _oldAdressStr { get; set; }


		public virtual string Fullname
		{
			get { return PhysicalClient != null ? PhysicalClient.FullName ?? "" : _Name; }
		}

		public virtual string GetAddress()
		{
			return Address != null ? Address.GetStringForPrint() : _oldAdressStr;
		}

		public virtual Client GetClient()
		{
			return this;
		}

		public virtual string GetPhone()
		{
			var phone = "";
			if (this.PhoneNumber != string.Empty) {
				phone = this.PhoneNumber;
			}
			else {
				var contactPhone = this.Contacts.FirstOrDefault(s => s.Type == ContactType.SmsSending);
				if (contactPhone != null) {
					phone = contactPhone.ContactString;
				}
			}
			return phone;
		}

		public virtual Client GetAppealClient(ISession session)
		{
			return this;
		}

		public virtual List<string> GetAppealFields()
		{
			return new List<string>() {
				"LegalClient",
				"Status",
				"SendSmsNotification"
			};
		}

		public virtual string GetAdditionalAppealInfo(string property, object oldPropertyValue, ISession session)
		{
			return "";
		}
	}

	/// <summary>
	/// /Тип клиента
	/// </summary>
	public enum ClientType
	{
		[Description("Физичекое лицо")] PhysicalClient = 1,
		[Description("Юридическое лицо")] Lawer = 2,
	}

	public enum StatusType
	{
		[Description("Зарегистрирован")] BlockedAndNoConnected = 1,
		[Description("Не подключен")] BlockedAndConnected = 3,
		[Description("Подключен")] Worked = 5,
		[Description("Заблокирован")] NoWorked = 7,
		[Description("Добровольная блокировка")] VoluntaryBlocking = 9,
		[Description("Расторгнут")] Dissolved = 10,
		[Description("Заблокирован - Восстановление работы")] BlockedForRepair = 11
	}
}