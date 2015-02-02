using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Audit;
using Common.Web.Ui.NHibernateExtentions;
using ExcelLibrary.BinaryFileFormat;
using InternetInterface.Controllers;
using InternetInterface.Helpers;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;

namespace InternetInterface.Models
{
	public enum ClientType
	{
		Phisical = 1,
		Legal = 2
	}

	[ActiveRecord("Clients", Schema = "Internet", Lazy = true), Auditable]
	public class Client : ChildActiveRecordLinqBase<Client>
	{
		private bool _disabled;

		public Client()
		{
			UserWriteOffs = new List<UserWriteOff>();
			ClientServices = new List<ClientService>();
			Payments = new List<Payment>();
			Contacts = new List<Contact>();
			Endpoints = new List<ClientEndpoint>();
			Appeals = new List<Appeals>();
			Orders = new List<Order>();
			ServiceRequests = new List<ServiceRequest>();
		}

		public Client(PhysicalClient client, Settings settings, Partner registrator = null)
			: this()
		{
			if (registrator != null) {
				WhoRegistered = registrator;
				WhoRegisteredName = registrator.Name;
			}
			PhysicalClient = client;
			PhysicalClient.Client = this;
			Type = ClientType.Phisical;
			SendSmsNotification = true;
			FreeBlockDays = 28;
			YearCycleDate = DateTime.Now;
			RegDate = DateTime.Now;
			PercentBalance = 0.8m;
			Status = settings.DefaultStatus;
			Recipient = settings.DefaultRecipient;
			foreach (var defaultService in settings.DefaultServices) {
				ClientServices.Add(new ClientService(this, defaultService));
			}
		}

		public Client(LawyerPerson person, Partner partner)
			: this()
		{
			WhoRegistered = partner;
			WhoRegisteredName = partner.Name;
			RegDate = DateTime.Now;
			LawyerPerson = person;
			Name = person.ShortName;
			Type = ClientType.Legal;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual bool Disabled
		{
			get { return _disabled; }
			set
			{
				if (_disabled != value) {
					if (value)
						BlockDate = SystemTime.Now();
					else
						BlockDate = null;
				}
				_disabled = value;
			}
		}

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string Address { get; set; }

		[Property]
		public virtual ClientType Type { get; set; }

		[BelongsTo("PhysicalClient", Lazy = FetchWhen.OnInvoke, Cascade = CascadeEnum.SaveUpdate), Auditable]
		public virtual PhysicalClient PhysicalClient { get; set; }

		/// <summary>
		/// Устанавливатся в dhcp в текущую дату если client.RatedPeriodDate == null && !client.Disabled
		/// </summary>
		[Property]
		public virtual DateTime? RatedPeriodDate { get; set; }

		[Property]
		public virtual int DebtDays { get; set; }

		[Property]
		public virtual bool ShowBalanceWarningPage { get; set; }

		/// <summary>
		/// Начал ли клиент работать
		/// Устанавливается в dhcp в текущую дату если client.BeginWork == null && !client.Disabled
		/// </summary>
		[Property]
		public virtual DateTime? BeginWork { get; set; }

		[BelongsTo(Cascade = CascadeEnum.SaveUpdate), Auditable]
		public virtual LawyerPerson LawyerPerson { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[BelongsTo("WhoRegistered", Lazy = FetchWhen.OnInvoke)]
		public virtual Partner WhoRegistered { get; set; }

		[Property]
		public virtual string WhoRegisteredName { get; set; }

		[Property]
		public virtual DateTime ConnectedDate { get; set; }

		[Property]
		public virtual bool AutoUnblocked { get; set; }

		[BelongsTo, Auditable("Статус"), ValidateNonEmpty]
		public virtual Status Status { get; set; }

		[BelongsTo(Lazy = FetchWhen.OnInvoke)]
		public virtual AdditionalStatus AdditionalStatus { get; set; }

		[Property]
		public virtual decimal PercentBalance { get; set; }

		[Property]
		public virtual bool PaidDay { get; set; }

		[Property]
		public virtual int FreeBlockDays { get; set; }

		[Property]
		public virtual DateTime? YearCycleDate { get; set; }

		[Property]
		public virtual decimal Sale { get; set; }

		[Property]
		public virtual DateTime? StartNoBlock { get; set; }

		[Property]
		public virtual DateTime? WhenShowWarning { get; set; }

		[Property]
		public virtual DateTime? BlockDate { get; set; }

		[Property]
		public virtual DateTime? StatusChangedOn { get; set; }

		[Property]
		public virtual bool SendEmailNotification { get; set; }

		/// <summary>
		/// Флаг, запустил ли клиент первый раз страницу личного кабинета на сайте.
		/// Судя по всему флаг обратен наименованию.
		/// Если False, то значит клиент еще не запускал личный кабинет и ему будет отображена дополнительная информация
		/// </summary>
		[Property("FirstLunch")]
		public virtual bool FirstLaunch { get; set; }

		[Property("SendSmsNotifocation"), Auditable("Смс рассылка")]
		public virtual bool SendSmsNotification { get; set; }

		[OneToOne(PropertyRef = "Client")]
		public virtual Request Request { get; set; }

		[BelongsTo]
		public virtual Recipient Recipient { get; set; }

		[OneToOne(PropertyRef = "Client")]
		public virtual MessageForClient Message { get; set; }

		[OneToOne(PropertyRef = "Client")]
		public virtual ConnectGraph ConnectGraph { get; set; }

		[Property, ValidateInteger("Должно быть введено число")]
		public virtual string RedmineTask { get; set; }

		[HasMany(ColumnKey = "Client", OrderBy = "PaidOn", Lazy = true)]
		public virtual IList<Payment> Payments { get; set; }

		[HasMany(ColumnKey = "Client", OrderBy = "RegDate", Lazy = true)]
		public virtual IList<ServiceRequest> ServiceRequests { get; set; }

		[HasMany(ColumnKey = "Client", OrderBy = "WriteOffDate", Lazy = true)]
		public virtual IList<WriteOff> WriteOffs { get; set; }

		[HasMany(ColumnKey = "Client", OrderBy = "Date", Lazy = true, Cascade = ManyRelationCascadeEnum.SaveUpdate)]
		public virtual IList<UserWriteOff> UserWriteOffs { get; set; }

		[HasMany(ColumnKey = "Client", OrderBy = "Date")]
		public virtual IList<Contact> Contacts { get; set; }

		[HasMany(ColumnKey = "Client", OrderBy = "Switch", Lazy = true, Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public virtual IList<ClientEndpoint> Endpoints { get; set; }

		[HasMany(ColumnKey = "Client", Lazy = true, Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public virtual IList<ClientService> ClientServices { get; set; }

		[HasMany(ColumnKey = "Client", OrderBy = "Date", Lazy = true, Cascade = ManyRelationCascadeEnum.SaveUpdate)]
		public virtual IList<Appeals> Appeals { get; set; }

		[HasMany(ColumnKey = "ClientId", Lazy = true)]
		public virtual IList<Order> Orders { get; set; }

		public virtual Brigad WhoConnected
		{
			get
			{
				if (Endpoints.Count > 0)
					return Endpoints.First().WhoConnected;
				return null;
			}
		}

		public virtual bool NeedShowFirstLaunchPage(IPAddress ip, ISession session)
		{
			if (FirstLaunch)
				return false;
			var lease = session.Query<Lease>().FirstOrDefault(l => l.Ip == ip);
			if (lease != null)
				return true;
			return false;
		}

		public virtual bool CanDisabled()
		{
			return PhysicalClient.Balance < GetPriceForTariff() * PercentBalance;
		}

		public virtual bool NoEndPoint()
		{
			return Endpoints.Count == 0;
		}

		public virtual void CreateAutoEndPont(IPAddress ip, Lease lease, ISession session)
		{
			var settings = new Settings(session);
			if (string.IsNullOrEmpty(lease.Switch.Name))
				lease.Switch.Name = PhysicalClient.GetShortAdress();

			var endpoint = new ClientEndpoint(this, lease.Port, lease.Switch);
			var paymentForConnect = new PaymentForConnect(PhysicalClient.ConnectSum, endpoint);
			endpoint.PayForCon = paymentForConnect;
			lease.Endpoint = endpoint;

			SetStatus(Status.Get(StatusType.Worked, session));
			AddEndpoint(endpoint, settings);

			session.Save(lease.Switch);
			session.Save(endpoint.PayForCon);
			session.Save(lease);
			session.Save(this);
		}

		public virtual string GetFreePorts()
		{
			var result = string.Empty;
			if (Endpoints.Count == 0)
				return string.Empty;
			var deniedPorts = Endpoints[0].Switch.Endpoints.Select(e => e.Port);
			for (int i = 1; i <= Endpoints[0].Switch.TotalPorts; i++) {
				if (!deniedPorts.Contains(i))
					result += string.Format("{0}, ", i);
			}
			if (!string.IsNullOrEmpty(result))
				return result.Remove(result.Length - 2, 2);
			return string.Empty;
		}

		public virtual bool HaveRed()
		{
			return !string.IsNullOrEmpty(RedmineTask);
		}

		public virtual ClientEndpoint FirstPoint()
		{
			return Endpoints.FirstOrDefault();
		}

		//клиент может редактировать свои услуги если
		//его баланс больше нуля и он не отключен
		public virtual bool CanEditServicesFromPrivateOffice
		{
			get { return PhysicalClient.Balance > 0 && !Disabled; }
		}

		public virtual bool HavePaymentToStart()
		{
			if (Status.Type == StatusType.BlockedForRepair)
				return false;
			var forbiddenByService = ClientServices.Any(s => s.Service.BlockingAll && s.IsActivated);
			if (forbiddenByService)
				return false;
			var tariffSum = GetPriceIgnoreDisabled();
			return Payments.Sum(s => s.Sum) >= tariffSum * PercentBalance;
		}

		public virtual bool IsPhysical()
		{
			return PhysicalClient != null;
		}

		public virtual string GetAdress()
		{
			if (PhysicalClient != null)
				return PhysicalClient.GetAdress();
			if (LawyerPerson != null)
				return LawyerPerson.ActualAdress;
			return String.Empty;
		}

		public virtual string GetCutAdress()
		{
			if (PhysicalClient != null)
				return PhysicalClient.GetShortAdress();
			if (LawyerPerson != null)
				return LawyerPerson.ActualAdress;
			return String.Empty;
		}

		public virtual bool NeedShowWarning()
		{
			if (!RatedPeriodDate.HasValue)
				return true;

			return NeedShowWarning(GetSumForRegularWriteOff());
		}

		public virtual bool NeedShowWarning(decimal sumForWriteOff)
		{
			return (Balance - sumForWriteOff < 0) || ShowWarningBecauseNoPassport();
		}

		public virtual bool ShowWarningBecauseNoPassport()
		{
			if (!IsPhysical())
				return false;

			if (BeginWork == null)
				return false;

			var dontHavePassportData = string.IsNullOrEmpty(PhysicalClient.PassportNumber);
			var goodMoney = Balance > 0;
			var date = SystemTime.Now() > BeginWork.Value.AddDays(7);

			return dontHavePassportData && goodMoney && date;
		}

		public virtual string Contact
		{
			get
			{
				return Contacts.Where(c => c.Type == ContactType.HeadPhone)
					.Select(c => c.HumanableNumber)
					.FirstOrDefault(Contacts.Select(c => c.HumanableNumber).FirstOrDefault());
			}
		}

		public virtual string ForSearchId(string query)
		{
			return TextHelper.SelectQuery(query, Id.ToString("00000"));
		}

		public virtual string ForSearchName(string query)
		{
			return TextHelper.SelectQuery(query, Name);
		}

		public virtual string ForSearchContactAction(string query, Action<Contact> doContact)
		{
			var result = String.Empty;
			if (!String.IsNullOrEmpty(query)) {
				var contacts = Contacts.Where(c => c.Text.Contains(query));
				foreach (var contact in contacts) {
					doContact(contact);
				}
			}
			if (String.IsNullOrEmpty(result)) {
				return Contact;
			}
			return result;
		}

		public virtual string ForSearchContact(string query)
		{
			var result = string.Empty;
			ForSearchContactAction(query, contact => {
				result += TextHelper.SelectContact(query, contact.Text) + "<br/>";
			});
			return result;
		}

		public virtual string ForSearchContactNoLight(string query)
		{
			var result = string.Empty;
			ForSearchContactAction(query, contact => {
				result += (contact.Text + "/r/n");
			});
			return result;
		}

		public virtual string GetTariffName()
		{
			if (IsPhysical())
				return PhysicalClient.Tariff != null ? PhysicalClient.Tariff.Name : string.Empty;
			if (LawyerPerson.Tariff != null)
				return string.Format("{0} руб.", LawyerPerson.Tariff.Value);
			return "Тариф не задан";
		}
		public virtual bool StatusCanChange()
		{
			if (PhysicalClient != null)
				return true;
			if (LawyerPerson != null) {
				if (LawyerPerson.Tariff != null)
					return true;
			}
			return false;
		}

		public virtual bool PaymentForTariff()
		{
			return Payments.Sum(p => p.Sum) >= GetPriceIgnoreDisabled();
		}

		public virtual bool CanUseDebtWork()
		{
			return Service.Type<DebtWork>().CanActivate(this);
		}

		public virtual bool CanUsedVoluntaryBlockin()
		{
			return Service.Type<VoluntaryBlockin>().CanActivate(this);
		}

		public virtual bool NeedShowWarningForLawyer()
		{
			if (LawyerPerson == null)
				return false;
			var haveService = ClientServices.FirstOrDefault(cs => cs.Service.Id == Service.Type<WorkLawyer>().Id);
			var needShowWarning = LawyerPerson.NeedShowWarning();
			if (haveService != null && haveService.IsActivated)
				return false;
			if ((haveService != null && !haveService.IsActivated && needShowWarning) ||
				(haveService == null && needShowWarning))
				return true;
			return needShowWarning;
		}

		public virtual bool HaveVoluntaryBlockin()
		{
			return HaveService<VoluntaryBlockin>();
		}

		public virtual bool AdditionalCanUsed(string aStatus)
		{
			if (AdditionalStatus != null && AdditionalStatus.Id == (uint)AdditionalStatusType.Refused)
				return false;
			return Status.Additional.Contains(AdditionalStatus.Queryable.First(a => a.ShortName == aStatus));
		}

		public virtual string ChangePhysicalClientPassword()
		{
			var pass = CryptoPass.GeneratePassword();
			PhysicalClient.Password = CryptoPass.GetHashString(pass);
			PhysicalClient.Update();
			return pass;
		}

		public virtual ClientService FindActiveService<T>()
		{
			return ClientServices.FirstOrDefault(c => c.IsActivated && NHibernateUtil.GetClass(c.Service) == typeof(T));
		}

		public virtual bool HaveActiveService<T>()
		{
			return FindActiveService<T>() != null;
		}

		public virtual bool HaveService<T>()
		{
			return ClientServices.Any(c => NHibernateUtil.GetClass(c.Service) == typeof(T));
		}

		public virtual ClientType GetClientType()
		{
			if (PhysicalClient != null)
				return ClientType.Phisical;
			if (LawyerPerson != null)
				return ClientType.Legal;
			return ClientType.Phisical;
		}

		public virtual List<Internetsessionslog> GetClientLeases()
		{
			return ActiveRecordLinqBase<Internetsessionslog>.Queryable.Where(l => l.EndpointId.Client == this).ToList();
		}

		public virtual Internetsessionslog GetFirstLease()
		{
			return GetClientLeases().FirstOrDefault();
		}

		public virtual Internetsessionslog GetLastLease()
		{
			return GetClientLeases().LastOrDefault();
		}

		public virtual IList<UserWriteOff> GetUserWriteOffs()
		{
			return UserWriteOffs.OrderByDescending(u => u.Date).ToList();
		}

		public virtual bool StartWork()
		{
			return BeginWork != null;
		}

		/// <summary>
		/// Получить интервал за
		/// </summary>
		/// <returns></returns>
		public virtual decimal GetInterval()
		{
			return (((DateTime)RatedPeriodDate).AddMonths(1) - (DateTime)RatedPeriodDate).Days + DebtDays;
		}

		/// <summary>
		/// Получить сумму ежедневного списания абонентской платы
		/// </summary>
		/// <returns></returns>
		public virtual decimal GetSumForRegularWriteOff()
		{
			var daysInInterval = GetInterval();
			var price = GetPrice();
			return Math.Round(price / daysInInterval, 2);
		}

		/// <summary>
		/// Биллинг этой функцией проверяет должен ли быть клиент заблокирован.
		/// </summary>
		/// <returns>True, если клиента необходимо блокировать</returns>
		public virtual bool CanBlock()
		{
			//Если у клиента подключен сервис, отменяющий блокировки, то он не должен быть заблокирован
			var cServ = ClientServices.FirstOrDefault(c => NHibernateUtil.GetClass(c.Service) == typeof(DebtWork));
			if (cServ != null && !cServ.Service.CanBlock(cServ))
				return false;

			//Если у юр. лица баланс меньше абоненской платы, помноженной на коэффициент из настроек, если у него не отключены блокировки
			if (LawyerPerson != null) {
				var serv = ClientServices.FirstOrDefault(c => NHibernateUtil.GetClass(c.Service) == typeof(WorkLawyer));
				if (serv != null)
					return false;
				var param = ConfigurationManager.AppSettings["LawyerPersonBalanceBlockingRate"];
				var rate = (decimal)float.Parse(param, CultureInfo.InvariantCulture);
				if (LawyerPerson.Tariff > 0 && LawyerPerson.Balance <= LawyerPerson.Tariff * -rate && !Disabled)
					return true;
				return false;
			}

			//Физики блокируются при отрицательном балансе
			if (Disabled || PhysicalClient.Balance >= 0)
				return false;
			return true;
		}

		public virtual IList<BaseWriteOff> GetWriteOffs(ISession session, string groupedKey, bool forIvrn = false)
		{
			var gpoupKey = string.Empty;
			if (groupedKey == "day")
				gpoupKey = "WriteOffDate";
			if (groupedKey == "month")
				gpoupKey = "concat(YEAR(WriteOffDate),'-',MONTH(WriteOffDate))";
			if (groupedKey == "year")
				gpoupKey = "YEAR(WriteOffDate)";
			if (!string.IsNullOrEmpty(gpoupKey))
				gpoupKey = "group by " + gpoupKey;
			var sumKey = string.Empty;
			if (!string.IsNullOrEmpty(gpoupKey))
				sumKey = "Sum";

			var textQuery = !forIvrn ? @"SELECT
Id,
{1}(WriteOffSum) as WriteOffSum,
{1}(VirtualSum) as VirtualSum,
{1}(MoneySum) as MoneySum,
WriteOffDate,
Client,
BeforeWriteOffBalance,
`Comment`,
false as UserWriteOff
FROM internet.WriteOff W
where Client = :clientid and WriteOffSum > 0
{0}

UNION

select
uw.Id as Id,
{1}(`sum`) as WriteOffSum,
0.0 as VirtualSum,
0.0 as MoneySum,
`date` as WriteOffDate,
Client,
0.0 as BeforeWriteOffBalance,
`Comment`,
true as UserWriteOff
from internet.UserWriteOffs uw
where uw.client = :clientid
{0}
;" :
@"SELECT
Id,
{1}(WriteOffSum) as WriteOffSum,
{1}(VirtualSum) as VirtualSum,
{1}(MoneySum) as MoneySum,
WriteOffDate,
Client,
BeforeWriteOffBalance,
`Comment`,
false as UserWriteOff
FROM internet.WriteOff W
where Client = :clientid and WriteOffSum > 0
{0}
;";
			var query = session.CreateSQLQuery(String.Format(textQuery, gpoupKey, sumKey))
				.SetParameter("clientid", Id);
			return query.ToList<BaseWriteOff>();
		}

		[Obsolete("Не использовать добавлено для обратной совместимости")]
		public virtual ClientConnectInfo ConnectInfoFirst()
		{
			return ArHelper.WithSession(s => ConnectInfoFirst(s));
		}

		public virtual ClientConnectInfo ConnectInfoFirst(ISession session)
		{
			return GetConnectInfo(session).FirstOrDefault();
		}

		public virtual IList<ClientOrderInfo> GetOrderInfo(ISession session, bool disabled = false)
		{
			var result = new List<ClientOrderInfo>();
			var orders = Orders.Where(o => o.IsDeactivated == disabled).ToList();
			var endpointInfos = GetConnectInfo(session);

			foreach (var order in orders) {
				var orderInfo = new ClientOrderInfo {
					Order = order
				};
				if(order.EndPoint != null)
					orderInfo.ClientConnectInfo = endpointInfos.FirstOrDefault(i => i.endpointId == order.EndPoint.Id);
				if (orderInfo.ClientConnectInfo == null)
					orderInfo.ClientConnectInfo = new ClientConnectInfo();
				result.Add(orderInfo);
			}
			return result;
		}

		public virtual IList<ClientConnectInfo> GetConnectInfo(ISession session)
		{
			if ((PhysicalClient != null) || (LawyerPerson != null)) {
				var infos = session.CreateSQLQuery(String.Format(@"
select distinctrow
inet_ntoa(CE.Ip) as static_IP,
inet_ntoa(L.Ip) as Leased_IP,
CE.Client,
Ce.Switch,
NS.Name as Swith_adr,
inet_ntoa(NS.ip) as swith_IP,
CE.Port,
PS.Speed,
CE.Monitoring,
CE.Id as endpointId,
CE.ActualPackageId,
pfc.`Sum` as ConnectSum,
cb.Name as WhoConnected
from internet.ClientEndpoints CE
left join internet.NetworkSwitches NS on NS.Id = CE.Switch
#join internet.Clients C on CE.Client = C.Id
left join internet.Leases L on L.Endpoint = CE.Id
left join internet.PackageSpeed PS on PS.PackageId = CE.PackageId
left join internet.PaymentForConnect pfc on pfc.EndPoint = CE.id
left join internet.ConnectBrigads cb on cb.Id = ce.WhoConnected
where CE.Client = {0}", Id))
					.ToList<ClientConnectInfo>();
				return infos;
			}
			return new List<ClientConnectInfo>();
		}

		public virtual bool OnLine()
		{
			if (ActiveRecordLinqBase<Lease>.Queryable.FirstOrDefault(l => l.Endpoint.Client == this) != null)
				return true;
			return false;
		}

		public virtual decimal ToPay()
		{
			var toPay = GetPriceIgnoreDisabled() - PhysicalClient.Balance;
			return toPay < 10 ? 10 : toPay;
		}

		public virtual bool MinimumBalance()
		{
			return PhysicalClient.Balance - GetSumForRegularWriteOff() < 0;
		}

		public virtual decimal GetPriceForTariff()
		{
			if (PhysicalClient.Tariff == null)
				throw new Exception(String.Format("Для клиента {0} не задан тариф проверь настройки", Id));

			var price = AccountDiscounts(PhysicalClient.Tariff.Price);
			var finalPrice = AccountDiscounts(PhysicalClient.Tariff.FinalPrice);

			if ((PhysicalClient.Tariff.FinalPriceInterval == 0 || PhysicalClient.Tariff.FinalPrice == 0))
				return price;

			if ((BeginWork != null && BeginWork.Value.AddMonths(PhysicalClient.Tariff.FinalPriceInterval) <= SystemTime.Now()))
				return finalPrice;
			return price;
		}

		public virtual decimal GetTariffPrice()
		{
			if (BeginWork == null)
				return 0;
			if (Disabled)
				return 0;
			return GetPriceForTariff();
		}

		private decimal AccountDiscounts(decimal price)
		{
			if (Sale > 0)
				price *= 1 - Sale / 100;
			return price;
		}

		/// <summary>
		/// Получить сумму ежемесячного списания
		/// </summary>
		/// <returns></returns>
		public virtual decimal GetPrice()
		{
			var services = ClientServices.Where(c => c.IsActivated).ToArray();
			var blockingService = services.FirstOrDefault(c => c.Service.BlockingAll);
			if (blockingService != null)
				return blockingService.GetPrice() + services.Where(c => c.Service.ProcessEvenInBlock).Sum(c => c.GetPrice());

			return services.Sum(c => c.GetPrice());
		}

		/// <summary>
		/// Разблокирует клиента
		/// </summary>
		public virtual void Enable()
		{
			SetStatus(Status.Find((uint)StatusType.Worked));
		}

		public virtual decimal GetPriceIgnoreDisabled()
		{
			decimal price = 0;
			decimal iptvPrice = 0;
			if (Internet.ActivatedByUser)
				iptvPrice += Iptv.Channels.Sum(c => c.CostPerMonthWithInternet);
			else
				iptvPrice += Iptv.Channels.Sum(c => c.CostPerMonth);
			price += iptvPrice;

			if (Internet.ActivatedByUser)
				price += GetPriceForTariff();

			if (iptvPrice == 0) {
				var service = FindActiveService<IpTvBoxRent>();
				if (service != null)
					price += service.GetPrice();
			}

			return price;
		}

		public virtual decimal Balance
		{
			get
			{
				if (PhysicalClient != null)
					return PhysicalClient.Balance;
				if (LawyerPerson != null)
					return LawyerPerson.Balance;
				return 0m;
			}
		}

		public virtual string Redirect()
		{
			return GetClientType() == ClientType.Phisical
				? "../UserInfo/ShowPhysicalClient?filter.ClientCode=" + Id
				: "../UserInfo/ShowLawyerPerson?filter.ClientCode=" + Id;
		}

		/// <summary>
		/// Активация услуги может привести к тому что нужно перенастроить sce
		/// после вызова нужно сделать проверку
		/// if (client.IsNeedRecofiguration)
		/// 	SceHelper.UpdatePackageId(DbSession, client);
		/// </summary>
		public virtual string Activate(ClientService clientService)
		{
			var serviceType = NHibernateUtil.GetClass(clientService.Service);
			if (ClientServices.Any(c => NHibernateUtil.GetClass(c.Service) == serviceType)
				&& !new[] { typeof(IpTvBoxRent), typeof(HardwareRent) }.Contains(serviceType))
				throw new ServiceActivationException(String.Format("Невозможно активировать услугу \"{0}\"", clientService.Service.HumanName));

			if (!clientService.TryActivate())
				throw new ServiceActivationException(String.Format("Невозможно активировать услугу \"{0}\"", clientService.Service.HumanName));

			ClientServices.Add(clientService);
			var message = string.Format("Услуга \"{0}\" активирована на период с {1} по {2}", clientService.Service.HumanName,
				clientService.BeginWorkDate != null
					? clientService.BeginWorkDate.Value.ToShortDateString()
					: DateTime.Now.ToShortDateString(),
				clientService.EndWorkDate != null
					? clientService.EndWorkDate.Value.ToShortDateString()
					: string.Empty);
			CreareAppeal(message, AppealType.Statistic);
			IsNeedRecofiguration = serviceType == typeof(DebtWork);
			return message;
		}

		/// <summary>
		/// Деактивация услуги может привести к тому что нужно перенастроить sce
		/// после вызова нужно сделать проверку
		/// if (client.IsNeedRecofiguration)
		/// 	SceHelper.UpdatePackageId(DbSession, client);
		/// </summary>
		public virtual string Deactivate(ClientService service)
		{
			service.ForceDeactivate();
			var message = String.Format("Услуга \"{0}\" деактивирована", service.Service.HumanName);
			CreareAppeal(message, AppealType.Statistic);
			IsNeedRecofiguration = NHibernateUtil.GetClass(service.Service) == typeof(VoluntaryBlockin);
			return message;
		}

		private bool TryActivate(Service service, ClientEndpoint endpoint = null)
		{
			if (ClientServices.Any(s => s.Endpoint == endpoint && s.Service == service))
				return false;
			var clientService = new ClientService(this, service) {
				Endpoint = endpoint
			};
			ClientServices.Add(clientService);
			return clientService.TryActivate();
		}

		private bool TryDeactivate(Service service, ClientEndpoint endpoint = null)
		{
			var clientService = ClientServices.FirstOrDefault(s => s.Endpoint == endpoint && s.Service == service);
			if (clientService == null)
				return false;
			clientService.ForceDeactivate();
			return true;
		}

		public virtual WriteOff CalculatePerDayWriteOff(decimal price, bool writeoffVirtualFirst = false)
		{
			if (price == 0)
				return null;

			var costPerDay = price / GetInterval();
			return PhysicalClient.CalculateWriteoff(costPerDay);
		}

		public virtual ClientService Internet
		{
			get { return ClientServices.FirstOrDefault(s => NHibernateUtil.GetClass(s.Service) == typeof(Internet)); }
		}

		public virtual ClientService Iptv
		{
			get { return ClientServices.FirstOrDefault(s => NHibernateUtil.GetClass(s.Service) == typeof(IpTv)); }
		}

		//флаг устанавливается в случае если нужно изменить настройки sce
		//например если была активирована услуга обещанный платеж
		public virtual bool IsNeedRecofiguration { get; set; }

		public virtual void RegistreContacts(Partner registrator)
		{
			if (!string.IsNullOrEmpty(PhysicalClient.PhoneNumber)) {
				var phone = PhysicalClient.PhoneNumber.Replace("-", string.Empty);
				var contact = new Contact(registrator, this, ContactType.MobilePhone, phone) {
					Comment = "Указан при регистрации",
				};
				Contacts.Add(contact);
				contact = new Contact(registrator, this, ContactType.SmsSending, phone) {
					Comment = "Указан при регистрации",
				};
				Contacts.Add(contact);
			}

			if (!string.IsNullOrEmpty(PhysicalClient.HomePhoneNumber)) {
				var phone = PhysicalClient.HomePhoneNumber.Replace("-", string.Empty);
				var contact = new Contact(registrator, this, ContactType.HousePhone, phone) {
					Comment = "Указан при регистрации",
				};
				Contacts.Add(contact);
			}

			if (!string.IsNullOrEmpty(PhysicalClient.Email)) {
				var contact = new Contact(registrator, this, ContactType.Email, PhysicalClient.Email) {
					Comment = "Указан при регистрации",
				};
				Contacts.Add(contact);
			}
		}

		public virtual bool AfterRegistration(Partner registrator)
		{
			var havePayment = PhysicalClient.Balance > 0;
			PostUpdate();
			AutoUnblocked = havePayment;
			Disabled = !havePayment;

			RegistreContacts(registrator);

			if (havePayment) {
				var payment = new Payment(this, PhysicalClient.Balance) {
					Agent = registrator,
					BillingAccount = true,
					RecievedOn = DateTime.Now,
				};
				Payments.Add(payment);
			}

			return havePayment;
		}

		public virtual string GeneragePassword()
		{
			var password = CryptoPass.GeneratePassword();
			PhysicalClient.Password = CryptoPass.GetHashString(password);
			return password;
		}

		public virtual void WriteOff(decimal sum, bool isVirtual)
		{
			if (PhysicalClient != null)
				PhysicalClient.WriteOff(sum, isVirtual);
			else
				LawyerPerson.Balance -= sum;
		}

		public override string ToString()
		{
			return String.Format("№{0} - {1}", Id, Name);
		}

		public virtual Appeals CreareAppeal(string message, AppealType type = AppealType.System, bool logBalance = true)
		{
			if (String.IsNullOrWhiteSpace(message))
				return null;
			if (logBalance)
				message += string.Format(". Баланс {0}.", Balance.ToString("0.00"));
			var appeal = new Appeals(message, this, type);
			Appeals.Add(appeal);
			return appeal;
		}

		public virtual bool RemoveEndpoint(ClientEndpoint endpoint)
		{
			if (Endpoints.Count > 1 || LawyerPerson != null) {
				ClientServices.RemoveEach(ClientServices.Where(s => s.Endpoint == endpoint));
				return Endpoints.Remove(endpoint);
			}
			return false;
		}

		public virtual void AddEndpoint(ClientEndpoint endpoint, Settings settings)
		{
			Endpoints.Add(endpoint);
			SyncServices(settings);
		}

		/// <summary>
		/// синхронизирует состояние услуг и состояние точки подключения
		/// </summary>
		public virtual void SyncServices(Settings settings)
		{
			if (!IsPhysical())
				return;
			var service = settings.Services.OfType<PinnedIp>().FirstOrDefault();
			if (service == null)
				return;

			foreach (var endpoint in Endpoints) {
				if (endpoint.Ip != null) {
					TryActivate(service, endpoint);
				}
				else {
					TryDeactivate(service, endpoint);
				}
			}
		}

		public virtual void UpdateStatus()
		{
			var expectedDisabled = CanDisabled();
			if (expectedDisabled != Disabled) {
				SetStatus(expectedDisabled
					? Status.Find((uint)StatusType.NoWorked)
					: Status.Find((uint)StatusType.Worked));
			}
		}

		public virtual bool ShouldNotifyOnLowBalance()
		{
			return !Disabled && Balance > 0 && SendSmsNotification && GetPossibleBlockDate() <= SystemTime.Today().AddDays(2);
		}

		public virtual DateTime GetPossibleBlockDate()
		{
			var sum = GetSumForRegularWriteOff();
			if (sum == 0)
				return DateTime.MaxValue;
			// + 1 тк предполагается что текущий уже оплачен
			return SystemTime.Today().AddDays((int)(Balance / sum) + 1).Date;
		}

		public virtual void SetStatus(StatusType status, ISession session)
		{
			SetStatus(session.Load<Status>((uint)status));
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
				Sale = 0;
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

		public virtual void PostUpdate()
		{
			if (LawyerPerson != null) {
				Name = LawyerPerson.ShortName;
				Address = LawyerPerson.ActualAdress;
			}
			if (PhysicalClient != null) {
				Name = string.Format("{0} {1} {2}", PhysicalClient.Surname, PhysicalClient.Name, PhysicalClient.Patronymic);
				Address = PhysicalClient.GetFullAddress();
			}
		}

		public virtual RegionHouse GetRegion()
		{
			if (PhysicalClient != null && PhysicalClient.HouseObj != null)
				return PhysicalClient.HouseObj.Region;
			if (LawyerPerson != null)
				return LawyerPerson.Region;
			return null;
		}

		public virtual bool IsDisabledByBilling()
		{
			return Disabled && AutoUnblocked && Status.Type == StatusType.NoWorked;
		}

		public virtual List<ClientService> GetRentServices()
		{
			var services = new[] { typeof(IpTvBoxRent), typeof(HardwareRent) };
			return ClientServices.Where(s => services.Contains(NHibernateUtil.GetClass(s.Service))).ToList();
		}

		public virtual IList<Status> GetAvailableStatuses(ISession session)
		{
			if (Status.Type == StatusType.BlockedForRepair)
				return new[] { session.Load<Status>((uint)StatusType.BlockedForRepair) };
			var statuses = session.Query<Status>().Where(s => s.ManualSet).ToList();
			statuses.Add(Status);
			return statuses.Distinct().OrderBy(s => s.Name).ToList();
		}

		public virtual bool IsBlockForRepair()
		{
			return Status.Type == StatusType.BlockedForRepair;
		}
	}

	public class ServiceActivationException : Exception
	{
		public ServiceActivationException(string message) : base(message)
		{
		}
	}
}