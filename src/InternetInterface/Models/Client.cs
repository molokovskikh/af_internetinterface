using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Internal.EventListener;
using Castle.ActiveRecord.Linq;
using Castle.Components.DictionaryAdapter;
using Common.Models.Helpers;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;
using NHibernate;

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
			ClientServices = new List<ClientService>();
			Payments = new List<Payment>();
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual bool Disabled { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual ClientType Type { get; set; }

		[BelongsTo("PhysicalClient", Lazy = FetchWhen.OnInvoke, Cascade = CascadeEnum.SaveUpdate), Auditable]
		public virtual PhysicalClients PhysicalClient { get; set; }

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

		[BelongsTo("WhoConnected", Lazy = FetchWhen.OnInvoke)]
		public virtual Brigad WhoConnected { get; set; }

		[Property]
		public virtual string WhoConnectedName { get; set; }

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
		public virtual bool SendEmailNotification { get; set; }

		[Property, Auditable("Смс рассылка")]
		public virtual bool SendSmsNotifocation { get; set; }

		[OneToOne(PropertyRef = "Client")]
		public virtual Request Request { get; set; }

		[BelongsTo]
		public virtual Recipient Recipient { get; set; }

		[OneToOne(PropertyRef = "Client")]
		public virtual MessageForClient Message { get; set; }

		public virtual bool HavePaymentToStart()
		{
			var tariffSum = GetPriceForTariff();
			if (Payments == null)
				return false;
			return Payments.Sum(s => s.Sum) >= tariffSum*PercentBalance;
		}

		public virtual string GetAdress()
		{
			if (PhysicalClient != null) {
				return PhysicalClient.GetAdress();
			}
			return string.Empty;
		}

		public virtual string GetCutAdress()
		{
			if (PhysicalClient != null)
				return PhysicalClient.GetCutAdress();
			return string.Empty;
		}

		public virtual string Contact
		{
			get
			{
				if (Contacts != null) {
					var contact = Contacts.Where(c => c.Type == ContactType.HeadPhone).FirstOrDefault();
					if (contact != null) {
						return contact.HumanableNumber();
					}
					contact = Contacts.FirstOrDefault();
					if (contact != null)
						return contact.HumanableNumber();
				}
				return string.Empty;
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

		public virtual string ForSearchContact(string query)
		{
			var result = string.Empty;
			if (!string.IsNullOrEmpty(query)) {
				var contacts = Contacts.Where(c => c.Text.Contains(query));
				foreach (var contact in contacts) {
					result += TextHelper.SelectContact(query, contact.Text) + "<br/>";
				}
			}
			if (string.IsNullOrEmpty(result)) {
				return Contact;
			}
			return result;
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
			if (Payments != null)
				return Payments.Sum(p => p.Sum) >= GetPriceForTariff();
			return false;
		}

		public virtual bool CanUsedPostponedPayment()
		{
			return PhysicalClient != null &&
				!ClientServices.Select(c => c.Service).Contains(Service.GetByType(typeof(DebtWork))) && Disabled &&
				PhysicalClient.Balance < 0 &&
				AutoUnblocked && PaymentForTariff();
		}

		public virtual bool CanUsedVoluntaryBlockin()
		{

			return new VoluntaryBlockin().CanActivate(this) && !HaveVoluntaryBlockin() && !Disabled;
		}

		public virtual bool HaveVoluntaryBlockin()
		{
			var clientServices = ClientServices.Select(s => s.Service.Id).ToList();
			var volBlockService = Service.GetByType(typeof (VoluntaryBlockin));
			return clientServices.Any(s => s == volBlockService.Id);
		}

		public virtual bool AdditionalCanUsed(string aStatus)
		{
			if (AdditionalStatus != null && AdditionalStatus.Id == (uint)AdditionalStatusType.Refused)
				return false;
			return Status.Additional.Contains(AdditionalStatus.Queryable.First(a => a.ShortName == aStatus));
		}

		[HasMany(ColumnKey = "Client", OrderBy = "PaidOn", Lazy = true)]
		public virtual IList<Payment> Payments { get; set; }

		[HasMany(ColumnKey = "Client", OrderBy = "WriteOffDate", Lazy = true)]
		public virtual IList<WriteOff> WriteOffs { get; set; }

		[HasMany(ColumnKey = "Client", OrderBy = "Date", Lazy = true)]
		public virtual IList<UserWriteOff> UserWriteOffs { get; set; }

		[HasMany(ColumnKey = "Client", OrderBy = "Date")]
		public virtual IList<Contact> Contacts { get; set; }
	
		[HasMany(ColumnKey = "Client", OrderBy = "Switch", Lazy = true)]
		public virtual IList<ClientEndpoints> Endpoints { get; set; }

		public virtual ClientEndpoints FirstPoint()
		{
			return Endpoints.FirstOrDefault();
		}

		[HasMany(ColumnKey = "Client", Lazy = true, Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public virtual IList<ClientService> ClientServices { get; set; }


		public virtual string ChangePhysicalClientPassword()
		{
			var pass =  CryptoPass.GeneratePassword();
			PhysicalClient.Password = CryptoPass.GetHashString(pass);
			PhysicalClient.Update();
			return pass;
		}

		public virtual bool HaveService(Service service)
		{
			return ClientServices.Select(c => c.Service).ToList().Contains(service);
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
			return Internetsessionslog.Queryable.Where(l => l.EndpointId.Client == this).ToList();
		}

		public virtual Internetsessionslog GetFirstLease()
		{
			return GetClientLeases().FirstOrDefault();
		}

		public virtual Internetsessionslog GetLastLease()
		{
			return GetClientLeases().LastOrDefault();
		}

		public virtual bool StartWork()
		{
			return BeginWork != null;
		}

		public virtual decimal GetInterval()
		{
			return (((DateTime)RatedPeriodDate).AddMonths(1) - (DateTime)RatedPeriodDate).Days + DebtDays;
		}

		public virtual bool CanBlock()
		{
			if (ClientServices != null)
			{
				var cServ = ClientServices.FirstOrDefault(c => NHibernateUtil.GetClass(c.Service) == typeof (DebtWork));
				if (cServ != null && !cServ.Service.CanBlock(cServ))
				{
					return false;
				}
			}

			if ((Disabled) || (PhysicalClient.Balance >= 0))
				return false;
			return true;
		}


		public virtual IList<BaseWriteOff> GetWriteOffs(string groupedKey)
		{
			IList<BaseWriteOff> writeOffs = new List<BaseWriteOff>();
			var gpoupKey = "concat(YEAR(WriteOffDate),'-',MONTH(WriteOffDate),'-',DAYOFMONTH(WriteOffDate))";
			if (groupedKey == "day")
				gpoupKey = "concat(YEAR(WriteOffDate),'-',MONTH(WriteOffDate),'-',DAYOFMONTH(WriteOffDate))";
			if (groupedKey == "month")
				gpoupKey = "concat(YEAR(WriteOffDate),'-',MONTH(WriteOffDate))";
			if (groupedKey == "year")
				gpoupKey = "YEAR(WriteOffDate)";
			ARSesssionHelper<BaseWriteOff>.QueryWithSession(session =>
			{
				var query =
					session.CreateSQLQuery(string.Format(
@"SELECT 
Id, 
Sum(WriteOffSum) as WriteOffSum,
Sum(VirtualSum) as VirtualSum,
Sum(MoneySum) as MoneySum,
WriteOffDate,
Client
FROM internet.WriteOff W
where Client = :clientid and WriteOffSum > 0
group by {0} order by WriteOffDate;", gpoupKey))
				.SetParameter("clientid", Id)
				.SetResultTransformer(new AliasToPropertyTransformer(typeof (BaseWriteOff)));
				writeOffs = query.List<BaseWriteOff>();
				return query.List<BaseWriteOff>();
			});
			return writeOffs;
		}

		public virtual ClientConnectInfo ConnectInfoFirst()
		{
			return GetConnectInfo().FirstOrDefault();
		}

		public virtual IList<ClientConnectInfo> GetConnectInfo()
		{
			if ((PhysicalClient!= null && Status != null && Status.Connected) ||
				(LawyerPerson != null && Status != null && Status.Connected))
			{
				//var client = Clients.FindAllByProperty("PhysicalClient", this);
					IList<ClientConnectInfo> ConnectInfo = new List<ClientConnectInfo>();
					ARSesssionHelper<ClientConnectInfo>.QueryWithSession(session =>
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
CE.Monitoring,
CE.Id as endpointId,
pfc.`Sum` as ConnectSum
from internet.ClientEndpoints CE
left join internet.NetworkSwitches NS on NS.Id = CE.Switch
#join internet.Clients C on CE.Client = C.Id
left join internet.Leases L on L.Endpoint = CE.Id
left join internet.PackageSpeed PS on PS.PackageId = CE.PackageId
left join internet.PaymentForConnect pfc on pfc.EndPoint = CE.id
where CE.Client = {0}",
Id)).SetResultTransformer(
new AliasToPropertyTransformer(typeof(ClientConnectInfo)))
.List<ClientConnectInfo>();
						ConnectInfo = query;
						return query;
					});
					if (ConnectInfo.Count != 0)
						return ConnectInfo;
				}
			return new List<ClientConnectInfo>();
		}

		public static Lease FindByIP(string ip)
		{
			var addressValue = BigEndianConverter.ToInt32(IPAddress.Parse(ip).GetAddressBytes());
			return Lease.Queryable.FirstOrDefault(l => l.Ip == addressValue);
		}

		public virtual bool OnLine()
		{
			if (Lease.Queryable.Where(l => l.Endpoint.Client == this).FirstOrDefault() != null)
				return true;
			return false;
		}

		public virtual decimal ToPay()
		{
			var toPay =  GetPriceForTariff() - PhysicalClient.Balance;
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

			if ((PhysicalClient.Tariff.FinalPriceInterval == 0 || PhysicalClient.Tariff.FinalPrice == 0) )
				return price;

			if ((BeginWork != null && BeginWork.Value.AddMonths(PhysicalClient.Tariff.FinalPriceInterval) <= SystemTime.Now()))
				return finalPrice;
			return price;
		}

		private decimal AccountDiscounts(decimal price)
		{
			if (Sale > 0)
				price *= 1 - Sale / 100;
			return price;
		}

		public virtual decimal GetPrice()
		{
			var servisesPrice = 0m;
			if (ClientServices != null)
			{
				var blockingService = ClientServices.Where(c => c.Service.BlockingAll && c.Activated).ToList().FirstOrDefault();
				if (blockingService != null && !blockingService.Diactivated)
					return blockingService.GetPrice();

				servisesPrice = ClientServices.Where(c=> !c.Service.BlockingAll && c.Activated).Sum(c => c.GetPrice());
			}

			var price = AccountDiscounts(PhysicalClient.Tariff.Price);
			var finalPrice = AccountDiscounts(PhysicalClient.Tariff.FinalPrice);

			if ((PhysicalClient.Tariff.FinalPriceInterval == 0 || PhysicalClient.Tariff.FinalPrice == 0) && !Disabled)
				return price + servisesPrice;

			if ((BeginWork != null) && !Disabled)
			{
				var beginWorkAdded = BeginWork.Value.AddMonths(PhysicalClient.Tariff.FinalPriceInterval);

				if (beginWorkAdded > SystemTime.Now())
					return price + servisesPrice;

				if (beginWorkAdded <= SystemTime.Now())
					return finalPrice + servisesPrice;
			}

			return 0m;
		}

		public virtual decimal GetBalance()
		{
			if (PhysicalClient != null)
				return PhysicalClient.Balance;
			if (LawyerPerson != null)
				return LawyerPerson.Balance;
			return 0m;
		}

		public virtual void SetBalance(decimal sum)
		{
			if (PhysicalClient != null)
			{
				PhysicalClient.Balance = sum;
				PhysicalClient.Update();
			}
			if (LawyerPerson != null)
			{
				LawyerPerson.Balance = sum;
				LawyerPerson.Update();
			}
		}

		public virtual string Redirect()
		{
			return GetClientType() == ClientType.Phisical
			       	? "../UserInfo/SearchUserInfo.rails?filter.ClientCode=" + Id
			       	: "../UserInfo/LawyerPersonInfo.rails?filter.ClientCode=" + Id;
		}
	}
}