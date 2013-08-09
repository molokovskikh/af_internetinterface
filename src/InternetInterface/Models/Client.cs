using System;
using System.Collections.Generic;
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
using InternetInterface.Controllers;
using InternetInterface.Helpers;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Linq;

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
		public Client()
		{
			UserWriteOffs = new List<UserWriteOff>();
			ClientServices = new List<ClientService>();
			Payments = new List<Payment>();
			Contacts = new List<Contact>();
			Endpoints = new List<ClientEndpoint>();
			Appeals = new List<Appeals>();
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
			SendSmsNotifocation = true;
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

		[PrimaryKey]
		public virtual uint Id { get; set; }

		private bool _disabled;

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
		public virtual ClientType Type { get; set; }

		[BelongsTo("PhysicalClient", Lazy = FetchWhen.OnInvoke, Cascade = CascadeEnum.SaveUpdate), Auditable]
		public virtual PhysicalClient PhysicalClient { get; set; }

		[Property]
		public virtual DateTime? RatedPeriodDate { get; set; }

		[Property]
		public virtual int DebtDays { get; set; }

		[Property]
		public virtual bool ShowBalanceWarningPage { get; set; }

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

		[BelongsTo, Auditable("Статус")]
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
		public virtual bool SendEmailNotification { get; set; }

		[Property]
		public virtual bool FirstLunch { get; set; }

		[Property, Auditable("Смс рассылка")]
		public virtual bool SendSmsNotifocation { get; set; }

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

		public virtual Brigad WhoConnected {
			get
			{
				if (Endpoints.Count > 0)
					return Endpoints.First().WhoConnected;
				return null;
			}
		}

		public virtual bool NeedShowFirstLunchPage(IRequest request, ISession session)
		{
			if (FirstLunch)
				return false;

			var ip = IPAddress.Parse(request.UserHostAddress);
			var lease = session.Query<Lease>().FirstOrDefault(l => l.Ip == ip);
			if (lease != null)
				return true;
			else if (Our(ip, session))
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

		public virtual void CreateAutoEndPont(IPAddress ip, ISession session)
		{
			var lease = session.Query<Lease>().FirstOrDefault(l => l.Ip == ip);
			if (lease == null)
				throw new Exception(string.Format("Клиент {0} пришел а аренды для него нет", Id));

			var endpoint = CreateAutoEndpoint(lease, Status.Get(StatusType.Worked, session));

			session.Save(lease.Switch);
			session.Save(endpoint.PayForCon);
			session.Save(endpoint);
			session.Save(lease);
			session.Save(this);
		}

		public virtual ClientEndpoint CreateAutoEndpoint(Lease lease, Status worked)
		{
			if (string.IsNullOrEmpty(lease.Switch.Name))
				lease.Switch.Name = PhysicalClient.GetShortAdress();

			var newPoint = new ClientEndpoint(this, lease.Port, lease.Switch);
			var paymentForConnect = new PaymentForConnect(PhysicalClient.ConnectSum, newPoint);
			newPoint.PayForCon = paymentForConnect;
			lease.Endpoint = newPoint;

			if (!Status.Connected) {
				Status = worked;
				FirstLunch = true;
				Disabled = false;
				AutoUnblocked = true;
			}
			return newPoint;
		}

		public static bool Our(IPAddress ip, ISession session)
		{
			if (ip.AddressFamily != AddressFamily.InterNetwork)
				return false;

			var address = ip.Address;
			return session.Query<IpPool>().Count(p => p.Begin <= address && p.End >= address) > 0;
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
			var forbiddenByService = ClientServices.Any(s => s.Service.BlockingAll && s.Activated);
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
			if (PhysicalClient != null) {
				return PhysicalClient.GetAdress();
			}
			return String.Empty;
		}

		public virtual string GetCutAdress()
		{
			if (PhysicalClient != null)
				return PhysicalClient.GetShortAdress();
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
				if (Contacts != null) {
					var contact = Contacts.FirstOrDefault(c => c.Type == ContactType.HeadPhone);
					if (contact != null) {
						return contact.HumanableNumber;
					}
					contact = Contacts.FirstOrDefault();
					if (contact != null)
						return contact.HumanableNumber;
				}
				return String.Empty;
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

		public virtual bool CanUsedPostponedPayment()
		{
			var haveDebtWork = !ClientServices.Select(c => c.Service.Id).Contains(Service.Type<DebtWork>().Id);
			return PhysicalClient != null
				&& haveDebtWork
				&& Disabled
				&& PhysicalClient.Balance <= 0
				&& AutoUnblocked;
		}

		public virtual bool NeedShowWarningForLawyer()
		{
			if (LawyerPerson == null)
				return false;
			var haveService = ClientServices.FirstOrDefault(cs => cs.Service.Id == Service.Type<WorkLawyer>().Id);
			var needShowWarning = LawyerPerson.NeedShowWarning();
			if (haveService != null && haveService.Activated)
				return false;
			if ((haveService != null && !haveService.Activated && needShowWarning) ||
				(haveService == null && needShowWarning))
				return true;
			return needShowWarning;
		}

		public virtual bool CanUsedVoluntaryBlockin()
		{
			return new VoluntaryBlockin().CanActivate(this) && !HaveVoluntaryBlockin() && !Disabled;
		}

		public virtual bool HaveVoluntaryBlockin()
		{
			var clientServices = ClientServices.Select(s => s.Service.Id).ToList();
			var volBlockService = Service.GetByType(typeof(VoluntaryBlockin));
			return clientServices.Any(s => s == volBlockService.Id);
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

		public virtual ClientService FindService<T>()
		{
			return ClientServices.FirstOrDefault(c => c.Activated && NHibernateUtil.GetClass(c.Service) == typeof(T));
		}

		public virtual bool HaveService<T>()
		{
			return FindService<T>() != null;
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

		public virtual decimal GetInterval()
		{
			return (((DateTime)RatedPeriodDate).AddMonths(1) - (DateTime)RatedPeriodDate).Days + DebtDays;
		}

		public virtual decimal GetSumForRegularWriteOff()
		{
			var daysInInterval = GetInterval();
			var price = GetPrice();
			return price / daysInInterval;
		}

		public virtual bool CanBlock()
		{
			var cServ = ClientServices.FirstOrDefault(c => NHibernateUtil.GetClass(c.Service) == typeof(DebtWork));
			if (cServ != null && !cServ.Service.CanBlock(cServ))
				return false;

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

		public virtual IList<Order> GetOrders(ISession session, bool disabled = false)
		{
			return session.Query<Order>().Where(o => o.Client == this && o.Disabled == disabled).ToList();
		}

		public virtual IList<ClientOrderInfo> GetOrderInfo(ISession session, bool disabled = false)
		{
			var result = new List<ClientOrderInfo>();
			var orders = GetOrders(session, disabled);
			foreach (var order in orders) {
				var orderInfo = new ClientOrderInfo {
					Order = order
				};
				if(order.EndPoint != null) {
					orderInfo.ClientConnectInfo = GetSingleConnectInfo(session, order.EndPoint.Id);
				}
				else {
					orderInfo.ClientConnectInfo = new ClientConnectInfo();
				}
				result.Add(orderInfo);
			}
			return result;
		}

		private ClientConnectInfo GetSingleConnectInfo(ISession session, uint endpointId)
		{
			return GetConnectInfo(session).FirstOrDefault(i => i.endpointId == endpointId);
		}

		public virtual IList<ClientConnectInfo> GetConnectInfo(ISession session)
		{
			if ((PhysicalClient != null) || (LawyerPerson != null)) {
				var infos = session.CreateSQLQuery(String.Format(@"
select
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
			return PhysicalClient.Balance - GetPrice() / GetInterval() < 0;
		}

		public virtual decimal GetPriceForTariff()
		{
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

		public virtual decimal GetPrice()
		{
			var services = ClientServices.Where(c => c.Activated).ToArray();
			var blockingService = services.FirstOrDefault(c => c.Service.BlockingAll);
			if (blockingService != null)
				return blockingService.GetPrice() + services.Where(c => c.Service.ProcessEvenInBlock).Sum(c => c.GetPrice());

			return services.Sum(c => c.GetPrice());
		}

		public virtual void Enable()
		{
			Status = Status.Find((uint)StatusType.Worked);
			RatedPeriodDate = null;
			DebtDays = 0;
			ShowBalanceWarningPage = false;
			Disabled = false;
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
				var service = FindService<IpTvBoxRent>();
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
				? "../UserInfo/SearchUserInfo.rails?filter.ClientCode=" + Id
				: "../UserInfo/LawyerPersonInfo.rails?filter.ClientCode=" + Id;
		}

		public virtual void Activate(ClientService service)
		{
			if (ClientServices.Select(c => c.Service).Contains(service.Service))
				throw new ServiceActivationException("Невозможно использовать данную услугу");
			ClientServices.Add(service);
			service.Activate();
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
			get { return ClientServices.First(s => NHibernateUtil.GetClass(s.Service) == typeof(Internet)); }
		}

		public virtual ClientService Iptv
		{
			get { return ClientServices.First(s => NHibernateUtil.GetClass(s.Service) == typeof(IpTv)); }
		}

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

		public virtual bool AfterRegistration(Agent agent, Partner registrator)
		{
			var havePayment = PhysicalClient.Balance > 0;
			Name = string.Format("{0} {1} {2}", PhysicalClient.Surname, PhysicalClient.Name, PhysicalClient.Patronymic);
			AutoUnblocked = havePayment;
			Disabled = !havePayment;

			RegistreContacts(registrator);

			if (havePayment) {
				var payment = new Payment(this, PhysicalClient.Balance) {
					Agent = agent,
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

		public virtual void SelfRegistration(Lease lease, Status worked)
		{
			AfterRegistration(null, null);
			CreateAutoEndpoint(lease, worked);
			BeginWork = DateTime.Now;
			RatedPeriodDate = DateTime.Now;
			var payment = PhysicalClient.CalculateSelfRegistrationPayment();
			if (payment.Sum > 0)
				Payments.Add(payment);
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
	}

	public class ServiceActivationException : Exception
	{
		public ServiceActivationException(string message) : base(message)
		{
		}
	}
}