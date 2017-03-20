using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Common.Tools;
using Inforoom2.Helpers;
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
	[Class(0, Table = "Clients", Schema = "internet", NameType = typeof(Client),OptimisticLock = OptimisticLockMode.Version, DynamicUpdate = true), Description("Клиент")]
	public class Client : BaseModel, ILogAppeal, IServicemenScheduleItem, IClientExpander
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
			ServiceRequests = new List<ServiceRequest>();

			/// из старой админки. 
			Disabled = true;
			SendSmsNotification = true;
			PercentBalance = 0.8m;
			FreeBlockDays = 28;
			CreationDate = DateTime.Now;
			/// задано по результатам анализа изменений в БД "регистрацией клиента" старой админки.
			BlockDate = DateTime.Now;
			YearCycleDate = DateTime.Now;

			//перенесено из старой админки (нужен при проверке на необходимость показывать Варнинг)
			LegalClientOrders = new List<ClientOrder>();
		}


		[Bag(0, Table = "ServicemenScheduleItems")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "Client")]
		[OneToMany(2, ClassType = typeof(ServicemenScheduleItem))]
		public virtual IList<ServicemenScheduleItem> ServicemenScheduleItems { get; set; }

		/// <summary>
		/// Поле необходимо для получения статистики СЗ до добавления ServicemenScheduleItems 
		/// </summary>
		[Bag(0, Table = "ServiceRequest")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "Client")]
		[OneToMany(2, ClassType = typeof(ServiceRequest))]
		public virtual IList<ServiceRequest> ServiceRequests { get; protected set; }

		public virtual ServicemenScheduleItem ConnectionRequest
		{
			get
			{
				return ServicemenScheduleItems != null
					? ServicemenScheduleItems.FirstOrDefault(s => s.RequestType == ServicemenScheduleItem.Type.ClientConnectionRequest)
					: null;
			}
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
		
		[Property(Column = "ShowBalanceWarningPage")]
		public virtual bool _showBalanceWarningPage { get; set; }

		public virtual bool ShowBalanceWarningPage
		{
			get { return WarningPackageIdGet(); }
			set { WarningPackageIdUpdate(value); }
		}

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

		[Version(Name = "Version", Column = "Version")]
		public virtual int Version { get; set; }

		[Property(NotNull = true), Description("Бесплатные дни добровольной блокировки")]
		public virtual int FreeBlockDays { get; set; }

		[Property(NotNull = true, Column = "FirstLunch")]
		public virtual bool Lunched { get; set; }

		[Property(NotNull = false)]
		public virtual string Comment { get; set; }

		[Property]
		public virtual DateTime? StartNoBlock { get; set; }
		
		/// <summary>
		/// !!! Влияет на активацию списаний
		/// </summary>
		[Property]
		public virtual DateTime? RatedPeriodDate { get; set; }

		[Property]
		[DataType(DataType.Date)]
		public virtual DateTime? StatusChangedOn { get; set; }

		//дата начала работы клиента, выставляется, когда активируется точка подключения Биллингом, потом начинаются списания. 
		[Property(Column = "BeginWork")] //раньше Description("Дата первой аренды -проставляется DHCP-сервером во время получения клиентом первой аренды")
		public virtual DateTime? WorkingStartDate { get; set; }

		[Property, Description("Дата, по которой определяется когда бесплатные дни должны обновиться")]
		public virtual DateTime? YearCycleDate { get; set; }

		[ManyToOne(Cascade = "save-update"), Description("Статус")]
		public virtual Status Status { get; set; }

		[ManyToOne(Column = "PhysicalClient", Cascade = "save-update"), Description("Физ. лицо")]
		public virtual PhysicalClient PhysicalClient { get; set; }

		[OneToOne(PropertyRef = "Client")]
		public virtual ClientRequest ClientRequest { get; set; }

		[ManyToOne(Column = "LawyerPerson", Cascade = "save-update"), Description("Юр. лицо")]
		public virtual LegalClient LegalClient { get; set; }

		[OneToOne(PropertyRef = "Client")]
		public virtual PrivateMessage Message { get; set; }

		[Property(Column = "RedmineTask"), Description("Задача в Redmine")]
		public virtual string RedmineTask { get; set; }

		[ManyToOne(Column = "Recipient", Cascade = "save-update")]
		public virtual Recipient Recipient { get; set; }

		[Bag(0, Table = "ClientServices", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "Client")]
		[OneToMany(2, ClassType = typeof(ClientService))]
		public virtual IList<ClientService> ClientServices { get; set; }

		[Bag(0, Table = "Payments")]
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

		//TODO: перенесено из старой админки (нужен рефакторинг)
		[Bag(0, Table = "Orders", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "ClientId")]
		[OneToMany(2, ClassType = typeof(ClientOrder)), Description("Заказы")]
		public virtual IList<ClientOrder> LegalClientOrders { get; set; }

		public virtual string ClientId
		{
			get
			{
				var mask = "00000";
				var idString = Id.ToString();
				return mask.Length - idString.Length > 0 ? mask.Substring(idString.Length) + idString : idString;
			}
		}

		public virtual List<Appeal> ClientAppeals()
		{
			return Appeals.Where(s => s.AppealType == AppealType.ClientToRead || s.AppealType == AppealType.ClientToReadDone)
				.OrderBy(s => s.Date).ToList();
		}

		public virtual List<Appeal> ClientAppealsNotShown()
		{
			return Appeals.Where(s => s.AppealType == AppealType.ClientToRead).OrderBy(s => s.Date).ToList();
		}

		public virtual List<Appeal> ClientAppealsShown()
		{
			return Appeals.Where(s => s.AppealType == AppealType.ClientToReadDone).OrderBy(s => s.Date).ToList();
		}

		protected virtual bool WarningPackageIdGet()
		{
			if (this.Endpoints.Any(s => s.Disabled == false && s.IsEnabled.HasValue && s.IsEnabled.Value && s.WarningShow && s.PackageId == 10)) {
				return true;
			}
			return false;
		}

		protected virtual void WarningPackageIdUpdate(bool warninig)
		{
			if (Disabled == false)
				foreach (var item in this.Endpoints.Where(s=> s.Disabled == false && s.IsEnabled.HasValue && s.IsEnabled.Value).ToList()) {
					if (item.WarningShow && warninig) {
						item.PackageId = 10;
					} else {
						item.PackageId = item.StableTariffPackageId;
					}
				}
		}

		public virtual bool HasRentalHardWare
		{
			get { return RentalHardwareList != null && RentalHardwareList.Count(s => s.IsActive) > 0; }
		}

		public virtual bool IsWorkStarted()
		{
			return WorkingStartDate != null;
		}

		public virtual bool IsPhysicalClient => PhysicalClient != null;

		public virtual bool IsLegalClient => LegalClient != null;

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


		public virtual bool IsDisabledByBilling()
		{
			return Disabled && AutoUnblocked && Status.Type == StatusType.NoWorked;
		}

		/// <summary>
		/// ///							
		/// синхронизирует состояние услуг и состояние точки подключения. Вызывается иногда из Background.
		/// ///
		/// </summary>
		public virtual void SyncServices(ISession session, SettingsHelper settings)
		{
			if (PhysicalClient == null)
				return;
			var service = settings.Services.OfType<FixedIp>().FirstOrDefault();
			if (service == null)
				return;

			foreach (var endpoint in Endpoints.Where(s => !s.Disabled).ToList()) {
				if (endpoint.Ip != null) {
					TryActivate(session, service, endpoint);
				}
				else {
					TryDeactivate(session, service, endpoint);
				}
			}
		}

		/// <summary>
		/// Метод для проверки, арендовано ли оборудование типа hwType у клиента
		/// </summary>
		public virtual bool HardwareIsRented(RentalHardware hw)
		{
			return RentalHardwareList.ToList().Exists(rh => rh.Hardware == hw && rh.IsActive);
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

		public virtual decimal? GetSumWriteOffRemained()
		{
			if (!IsPhysicalClient)
				return null;
			
			if (!RatedPeriodDate.HasValue)
				return null;

			var daysInIntervalA = (((DateTime) RatedPeriodDate).AddMonths(1) - (DateTime) RatedPeriodDate).Days + DebtDays;
			var daysInIntervalB = (((DateTime) RatedPeriodDate).AddMonths(1) - SystemTime.Now()).Days + DebtDays;
			var price = GetPrice();
			var CurrentPrice = price/daysInIntervalA;
			var sum = daysInIntervalB*CurrentPrice;
			return Math.Round(sum, 2);
		}

		public virtual decimal GetSumForRegularWriteOff(int? customInterval = null, decimal? customPrice = null)
		{
			var daysInInterval = customInterval ?? GetInterval();
			var price = customPrice ?? GetPrice();
			return Math.Round(price / daysInInterval, 2);
		}

	    public virtual decimal GetPrice()
	    {
	        var services = ClientServices.Where(c => c.IsActivated).ToArray();
            //по задаче redmine 50439 зависимость оплаты от флага "Disabled"
            var blockingService = services.FirstOrDefault(c => c.Service.BlockingAll);
            if (Disabled && blockingService != null)
                return blockingService.GetPrice();
            return services.Sum(c => c.GetPrice());
	    }

		///////////////////////////////////////////////////////////////////////////////////Это вроде бы для физика.=>
		/// <summary>
		/// Формирует итоговую цену Интернета за месяц по данному тарифному плану
		/// </summary>
		public virtual decimal GetTariffPrice(bool isBlocked = false)
		{
			if (PhysicalClient == null)
				return 0;

			if (PhysicalClient.Plan == null || (!isBlocked && (WorkingStartDate == null || Disabled)))
				return 0;

			var prePrice = PhysicalClient.Plan.IgnoreDiscount ? PhysicalClient.Plan.Price : AccountDiscounts(PhysicalClient.Plan.Price);
			var finalPrice = PhysicalClient.Plan.IgnoreDiscount ? PhysicalClient.Plan.Price : AccountDiscounts(PhysicalClient.Plan.FinalPrice);
			if ((PhysicalClient.Plan.FinalPriceInterval == 0 || PhysicalClient.Plan.FinalPrice == 0))
				return prePrice;

			if (WorkingStartDate != null &&
			    WorkingStartDate.Value.AddMonths(PhysicalClient.Plan.FinalPriceInterval) <= SystemTime.Now())
				return finalPrice;
			return prePrice;
		}

		public virtual decimal ToPay(bool isBlocked = false)
		{
			var toPay = GetTariffPrice(isBlocked) - PhysicalClient.Balance;
			return toPay <= 0m ? 0m : toPay < 10m ? 10m : toPay;
		}

		///////////////////////////////////////////////////////////////////////////////////Это вроде бы для физика.  <=

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
		protected virtual decimal AccountDiscounts(decimal price)
		{
			if (Discount > 0)
				price *= 1 - ((decimal)Discount) / 100;
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
					RatedPeriodDate = SystemTime.Now();
				DebtDays = 0;
				ShowBalanceWarningPage = false;
			}
			else if (status.Type == StatusType.Dissolved) {
				Discount = 0;
			}

			if (Status.Type != status.Type) {
				StatusChangedOn = SystemTime.Now();
			}
			Status = status;
		}

		public virtual decimal Balance
		{
			get { return PhysicalClient != null ? PhysicalClient.Balance : LegalClient != null ? LegalClient.Balance : 0; }
			//юрикам пока в новой админке ничего не присваивается*
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
			else
				LegalClient.Balance -= sum;
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

		public virtual bool AbsentPassportData(bool checkDateOfBirth = false)
		{
			if (PhysicalClient == null)
				return true;
			var hasNoPassportData = string.IsNullOrEmpty(PhysicalClient.PassportNumber);
			if (PhysicalClient.CertificateType == CertificateType.Passport) {
				hasNoPassportData = hasNoPassportData && string.IsNullOrEmpty(PhysicalClient.PassportSeries);
				hasNoPassportData = hasNoPassportData && string.IsNullOrEmpty(PhysicalClient.PassportResidention);
			}
			else
				hasNoPassportData = hasNoPassportData && string.IsNullOrEmpty(PhysicalClient.CertificateName);

			hasNoPassportData = hasNoPassportData && PhysicalClient.PassportDate == DateTime.MinValue;
			hasNoPassportData = hasNoPassportData && string.IsNullOrEmpty(PhysicalClient.RegistrationAddress);
			hasNoPassportData = hasNoPassportData || (checkDateOfBirth && (PhysicalClient.BirthDate == DateTime.MinValue));
			return hasNoPassportData;
		}

		public static Client GetClientForIp(string ipstr, ISession dbSession)
		{
			var endpoint = ClientEndpoint.GetEndpointForIp(ipstr, dbSession);
			if (endpoint != null)
				return endpoint.Client;
			return null;
		}

		public virtual bool IsOnlineCheck()
		{
			var isOnLineFilter = Endpoints.FirstOrDefault(s => !s.Disabled)?.LeaseList.FirstOrDefault()?.LeaseEnd;
			if (isOnLineFilter != null) {
				return isOnLineFilter.Value >= SystemTime.Now();
			}
			return false;
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
			if (!String.IsNullOrEmpty(this.PhoneNumber)) {
				phone = this.PhoneNumber;
			}
			else {
				var contactPhone = this.Contacts.FirstOrDefault(s => s.Type == ContactType.SmsSending);
				if (contactPhone != null) {
					phone = contactPhone.ContactString;
				}
				if (phone == "") {
					var contactMobPhone = this.Contacts.FirstOrDefault(s => s.Type == ContactType.MobilePhone);
					if (contactMobPhone != null) {
						phone = contactMobPhone.ContactString;
					}
				}
				if (phone == "") {
					var contactMobPhone = this.Contacts.FirstOrDefault(s => s.Type == ContactType.HeadPhone);
					if (contactMobPhone != null) {
						phone = contactMobPhone.ContactString;
					}
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
				"RedmineTask"
				// В старую админку приходит оповещение с формы (после переноса страницы клиента, можно будет раскомментить)
				//"SendSmsNotification"
			};
		}

		public virtual string GetAdditionalAppealInfo(string property, object oldPropertyValue, ISession session)
		{
			return "";
		}

		public virtual object GetExtendedClient => PhysicalClient != null ? (object)PhysicalClient : LegalClient;

		public virtual IList<Contact> GetContacts()
		{
			return ((IClientExpander)GetExtendedClient).GetContacts();
		}

		public virtual string GetConnetionAddress()
		{
			return ((IClientExpander)GetExtendedClient).GetConnetionAddress();
		}

		public virtual string GetName()
		{
			return ((IClientExpander)GetExtendedClient).GetName();
		}

		public virtual DateTime? GetRegistrationDate()
		{
			return ((IClientExpander)GetExtendedClient).GetRegistrationDate();
		}

		public virtual DateTime? GetDissolveDate()
		{
			return ((IClientExpander)GetExtendedClient).GetDissolveDate();
		}

		public virtual string GetPlan()
		{
			return ((IClientExpander)GetExtendedClient).GetPlan();
		}

		public virtual decimal GetBalance()
		{
			return ((IClientExpander)GetExtendedClient).GetBalance();
		}

		public virtual StatusType GetStatus()
		{
			return ((IClientExpander)GetExtendedClient).GetStatus();
		}

		public virtual List<string> GetFreePorts()
		{
			var result = new List<string>();
			if (Endpoints.Count(s => !s.Disabled) == 0 || Endpoints.FirstOrDefault(s => !s.Disabled).Switch == null)
				return result;
			var deniedPorts = Endpoints.FirstOrDefault(s => !s.Disabled).Switch.Endpoints.Select(e => e.Port);
			for (int i = 1; i <= Endpoints.FirstOrDefault(s => !s.Disabled).Switch.PortCount; i++) {
				if (!deniedPorts.Contains(i))
					result.Add(i.ToString());
			}
			return result;
		}

		public virtual bool HaveService<T>()
		{
			return ClientServices.Any(c => NHibernateUtil.GetClass(c.Service) == typeof(T));
		}

		public virtual bool TryActivate(ISession dbSession, Service service, ClientEndpoint endpoint = null, Employee employee = null)
		{
			if (ClientServices.Any(s => s.Client.Endpoints.Where(f => !f.Disabled).Contains(endpoint) && s.Service == service))
				return false;
			var clientService = new ClientService() {
				Client = this,
				Service = service,
				Endpoint = endpoint,
				Employee = employee
			};
			ClientServices.Add(clientService);
			return clientService.TryActivate(dbSession);
		}

		public virtual bool TryDeactivate(ISession dbSession, Service service, ClientEndpoint endpoint = null)
		{
			var clientService = ClientServices.FirstOrDefault(s => s.Endpoint == endpoint && s.Endpoint != null && !s.Endpoint.Disabled && s.Service == service);
			if (clientService == null)
				return false;
			clientService.Deactivate(dbSession);
			return true;
		}

		public virtual string RemoveEndpoint(ClientEndpoint endpoint, ISession dbSession)
		{
			var connectionAddress = $" коммутатор: {endpoint.Switch?.Id} - {endpoint.Switch?.Name}, порт: {endpoint.Port}";
			endpoint.Switch = null;
			endpoint.Port = 0;

			endpoint.Disabled = true;
            endpoint.Ip = null;
            endpoint.StaticIpList.RemoveEach(endpoint.StaticIpList);
            dbSession.Save(endpoint); 
            dbSession.Flush();
			return connectionAddress;
		}

		/// <summary>
		/// Изменение статуса.  ВАЖНО! ОТКАТ DbSession.Refresh(client) невозможен: TODO: Нужно избавиться от CreateSQLQuery после полного перехода в новую админку!  
		/// </summary>
		/// <param name="dbSession"></param>
		/// <param name="NewStatus"></param>
		/// <param name="employee"></param>
		/// <returns></returns>
		public virtual string TryToChangeStatus(ISession dbSession, Status NewStatus, Employee employee, ref bool updateSce)
		{
			string message = "";
			var oldStatus = Status;
			if (oldStatus != null && oldStatus != NewStatus) {
				// BlockedAndNoConnected = "зарегистрирован", BlockedAndConnected = "не подключен"
				var isDissolved = NewStatus.Type == StatusType.Dissolved;
				var setStatusToDissolved = isDissolved &&
				                           (oldStatus.Type == StatusType.BlockedAndNoConnected ||
				                            oldStatus.Type == StatusType.VoluntaryBlocking);

				if (oldStatus.ManualSet || oldStatus.Type == StatusType.BlockedAndConnected || setStatusToDissolved) {
					if (isDissolved && HasRentalHardWare) {
						message =
							"Договор не может быть расторгнут, т.к. у клиента имеется арендованное оборудование. Перед расторжением договора нужно изъять оборудование.";
					}
				}
				else {
					///!		client.Status = oldStatus;
					message =
						string.Format(
							"Статус не был изменен, т.к. нельзя изменить статус '{0}' вручную. Остальные данные были сохранены.",
							NewStatus.Name);
				}
				if (!string.IsNullOrEmpty(message)) {
					return message;
				}

				if (NewStatus.Type == StatusType.NoWorked && !Disabled) {
					Appeals.Add(new Appeal("Клиент был заблокирован оператором", this, AppealType.Statistic, employee));
				}
				if (NewStatus.Type != StatusType.Dissolved) {
					AutoUnblocked = true;
					if (Disabled)
						Appeals.Add(new Appeal("Клиент был разблокирован оператором", this, AppealType.Statistic, employee));
					if (ShowBalanceWarningPage)
						Appeals.Add(new Appeal("Оператором отключена страница Warning", this, AppealType.Statistic, employee));
					Disabled = false;
					ShowBalanceWarningPage = false;
				}
					decimal? lastTotalSum = 0m;
				if (NewStatus.Type == StatusType.Dissolved) {
					lastTotalSum = GetSumWriteOffRemained();
					if (HaveService<BlockAccountService>()) {
						var thisService = ClientServices
							.Where(cs => NHibernateUtil.GetClass(cs.Service) == typeof (BlockAccountService) && cs.IsActivated)
							.ToList().FirstOrDefault();
						if (thisService != null) {
							thisService.Deactivate(dbSession);
							SetStatus(StatusType.Dissolved, dbSession);
						}
					}
					var endpointLog =
						Endpoints.Where(e => !e.Disabled && e.Switch != null)
							.Implode(e => String.Format("Удалено подключение: коммутатор {0} порт {1}", e.Switch.Name, e.Port), Environment.NewLine);
					Appeals.Add(new Appeal(endpointLog, this, AppealType.System, employee));
					Endpoints.Each(s => {
						s.MacFull = null;
						s.Switch = null;
						s.Port = 0;
						s.Disabled = true;
                        s.Ip = null;
                        s.StaticIpList.RemoveEach(s.StaticIpList);
                        dbSession.Save(s); 
					});
					Discount = 0;
					Disabled = true;
					AutoUnblocked = false;
					//затираем дату начала работы, чтобы не списывалась абон плата
					WorkingStartDate = null;
				}
				SetStatus(NewStatus.Type, dbSession);

				if (lastTotalSum.HasValue && lastTotalSum.Value > 0) {
					var dissolvedTotalSumWriteOff = PhysicalClient.WriteOff(lastTotalSum.Value, false);
					dissolvedTotalSumWriteOff.Comment = "Расторжение договора.";
					dbSession.Save(dissolvedTotalSumWriteOff);
				}

				if (Status.Type == StatusType.NoWorked) {
					AutoUnblocked = false;
				}
				updateSce = true;
			}
			return message;
		}


		private bool NeedShowLegalWarning()
		{
			if (LegalClient == null)
				return false;
			var haveService = ClientServices.FirstOrDefault(cs => cs.Service.Id == Service.GetIdByType(typeof (WorkLawyer)));
			var needShowWarning = LegalClient.NeedShowWarning();
			if (haveService != null && haveService.IsActivated)
				return false;
			if ((haveService != null && !haveService.IsActivated && needShowWarning) ||
				(haveService == null && needShowWarning))
				return true;
			return needShowWarning;
		}

		public virtual WarningState GetWarningState()
		{
			//Проверка юр.лица
			if (LegalClient != null) {
				if (Disabled) {
					return WarningState.LawDisabled;
				}
				if (NeedShowLegalWarning()) {
					return WarningState.LawLowBalance;
				}
			}
			//Проверка физ.лица
			else if (PhysicalClient != null)
			{
				//если у клиента есть непрочитанные сообщения на сегодняшний день
				if (PhysicalClient.HasNotShownClientAppeals()) {
					return WarningState.AppealsForToday;
				}
				//елси заблокирован
				if (Disabled) {
					//и статус VoluntaryBlocking
					if (Status.Type == StatusType.VoluntaryBlocking) {
						return WarningState.PhysVoluntaryBlocking;
					}
					//и баланс меньше 0
					else if (Balance < 0 && Payments.Count != 0) {
						return WarningState.PhysLowBalance;
					}
					//клиенту со статусом "Зарегистрирован" необходимо заполнить паспортные данные 
					if (Status.Type == StatusType.BlockedAndNoConnected && !HasPassportData()) {
						return WarningState.PhysPassportData;
					}
					//и количество выплат (перечислений) = 0
					else if (Payments.Count == 0) {
						return WarningState.PhysFirstPayment;
					}
				}
				//или отсутствуют паспортные данные
				else {
					if (!HasPassportData()) {
						return WarningState.PhysPassportData;
					}
				}
				//если статус BlockedForRepair
				if (Status.Type == StatusType.BlockedForRepair) {
					return WarningState.PhysBlockedForRepair;
				}
			}
			return WarningState.NoWarning;
		}
	}

	/// <summary>
	/// Состояние клиента (элементы идентичны названиям экшенов варнинга)
	/// </summary>
	public enum WarningState
	{
		[Description("Отсутствие варнинга")] NoWarning,
		[Description("Юр. лицо деактивированный")] LawDisabled,
		[Description("Юр. лицо с низким балансом")] LawLowBalance,
		[Description("Физ. лицо с ремонтными работами")] PhysBlockedForRepair,
		[Description("Физ. лицо с добровольной блокировкой")] PhysVoluntaryBlocking,
		[Description("Физ. лицо с низким балансом")] PhysLowBalance,
		[Description("Физ. лицо без первого платежа")] PhysFirstPayment,
		[Description("Физ. лицо без паспортных данных")] PhysPassportData,
		[Description("Физ. лицо с непросмотренным сообщением")] AppealsForToday
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