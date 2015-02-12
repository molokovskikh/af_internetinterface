using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Common.Tools;
using Inforoom2.Models.Services;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель пользователя
	/// </summary>
	[Class(0, Table = "Clients", Schema = "internet", NameType = typeof (Client))]
	public class Client : BaseModel
	{
		public Client()
		{
			Endpoints = new List<ClientEndpoint>();
			ClientServices = new List<ClientService>();
		}
		[Property]
		public virtual bool Disabled { get; set; }

		[Property(NotNull = true)]
		public virtual int DebtDays { get; set; }

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

		[Property(NotNull = true)]
		public virtual int FreeBlockDays { get; set; }
		
		[Property(NotNull = true,Column = "FirstLunch")]
		public virtual bool Lunched { get; set; }
		
		[Property]
		public virtual DateTime? StartNoBlock { get; set; }

		[Property]
		public virtual DateTime? RatedPeriodDate { get; set; }

		[Property]
		public virtual DateTime? StatusChangedOn { get; set; }

		[Property(Column = "BeginWork")]
		public virtual DateTime? WorkingStartDate { get; set; }

		[ManyToOne(Cascade = "save-update")]
		public virtual Status Status { get; set; }

		[ManyToOne(Column = "PhysicalClient", Cascade = "save-update")]
		public virtual PhysicalClient PhysicalClient { get; set; }

		[ManyToOne(Column = "LawyerPerson", Cascade = "save-update")]
		public virtual LegalClient LegalClient { get; set; }

		[Bag(0, Table = "ClientServices", Cascade = "all-delete-orphan")]
		[Key(1, Column = "Client")]
		[OneToMany(2, ClassType = typeof (ClientService))]
		public virtual IList<ClientService> ClientServices { get; set; }
		
		[Bag(0,Table = "ClientEndpoints", Cascade = "all-delete-orphan")]
		[Key(1, Column = "client")]
		[OneToMany(2, ClassType = typeof (ClientEndpoint))]
		public virtual IList<ClientEndpoint> Endpoints { get; set; }

		[Bag(0,Table = "Contacts", Cascade = "all-delete-orphan")]
		[Key(1, Column = "client")]
		[OneToMany(2, ClassType = typeof (Contact))]
		public virtual IList<Contact> Contacts { get; set; }
		
		[Property(Column = "SendSmsNotifocation")]
		public virtual bool SendSmsNotification { get; set; }
		
		public virtual bool IsNeedRecofiguration { get; set; }

		public virtual bool IsWorkStarted()
		{
			return WorkingStartDate != null;
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
			return ClientServices.FirstOrDefault(c => c.IsActivated && NHibernateUtil.GetClass(c.Service) == typeof (T));
		}

		public virtual bool HasActiveService<T>()
		{
			return FindActiveService<T>() != null;
		}

		public virtual bool CanUseService(Service service)
		{
			return service.IsActivableFor(this);
		}

		public virtual decimal GetInterval()
		{
			return (((DateTime) RatedPeriodDate).AddMonths(1) - (DateTime) RatedPeriodDate).Days + DebtDays;
		}

		/// <summary>
		/// Получить число дней работы клиента при текущем балансе до авт. блокировки (в биллинге не использовать!)
		/// </summary>
		/// <returns>Расчётное кол-во дней работы без пополнения баланса</returns>
		public virtual int GetWorkDays()
		{
			var priceInDay = Plan.Price/DateTime.Now.DaysInMonth();		// ToDo Улучшить алгоритм вычисления
			return (int)Math.Floor(Balance/priceInDay);
		}

		public virtual decimal GetSumForRegularWriteOff()
		{
			var daysInInterval = GetInterval();
			var price = GetPrice();
			return Math.Round(price/daysInInterval, 2);
		}

		public virtual decimal GetPrice()
		{
			var services = ClientServices.Where(c => c.IsActivated).ToArray();
			var blockingService = services.FirstOrDefault(c => c.Service.BlockingAll);
			if (blockingService != null)
				return blockingService.GetPrice() + services.Where(c => c.Service.ProcessEvenInBlock).Sum(c => c.GetPrice());

			return services.Sum(c => c.GetPrice());
		}

		public virtual void SetStatus(StatusType status, ISession session)
		{
			SetStatus(session.Load<Status>((Int32) status));
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
			if (status.Type == StatusType.BlockedForRepair) {
				Disabled = true;
				AutoUnblocked = false;
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
			get {return PhysicalClient != null ? PhysicalClient.PhoneNumber : null;}
			set { PhysicalClient.PhoneNumber = value; }
		}

		public virtual string Email
		{
			get { return PhysicalClient != null ? PhysicalClient.Email : null; }
			set { PhysicalClient.Email = value; }
		}
		public virtual string Name
		{
			get { return PhysicalClient != null ? PhysicalClient.Name : null; }
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

		public virtual string GetAddressString()
		{
			return "г. Москва, ул. Вильнюсская, д.8, к.2";
		}

		public virtual bool HasPassportData()
		{
			var hasPassportData = !string.IsNullOrEmpty(PhysicalClient.PassportNumber);
			hasPassportData = hasPassportData && !string.IsNullOrEmpty(PhysicalClient.PassportSeries);
			hasPassportData = hasPassportData && !string.IsNullOrEmpty(PhysicalClient.PassportResidention);
			hasPassportData = hasPassportData && PhysicalClient.PassportDate != DateTime.MinValue;
			hasPassportData = hasPassportData && !string.IsNullOrEmpty(PhysicalClient.RegistrationAddress);
			return hasPassportData;
		}
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