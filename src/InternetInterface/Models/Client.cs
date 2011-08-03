using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Internal.EventListener;
using Castle.ActiveRecord.Linq;
using Castle.Components.DictionaryAdapter;
using Common.Models.Helpers;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Helpers;

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
            //Payments = new List<Payment>();
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

        /*[Property]
        public virtual DateTime? VoluntaryBlockingDate { get; set; }

        [Property]
        public virtual DateTime? VoluntaryUnblockedDate { get; set; }*/

        [BelongsTo, Auditable("Статус")]
		public virtual Status Status { get; set; }

        /*[Property]
        public virtual DateTime? PostponedPayment { get; set; }*/

        /*[Property]
        public virtual bool DebtWork { get; set; }*/

        [BelongsTo(Lazy = FetchWhen.OnInvoke)]
        public virtual AdditionalStatus AdditionalStatus { get; set; }

        public virtual bool PaymentForTariff()
        {
            if (Payments != null)
                return Payments.Sum(p => p.Sum) >= GetPrice();
            return false;
        }

        public virtual bool CanUsedPostponedPayment()
        {
            return PhysicalClient != null &&
                   !ClientServices.Select(c => c.Service).Contains(Service.GetByType(typeof (DebtWork))) && Disabled &&
                   PhysicalClient.Balance < 0 &&
                   AutoUnblocked && PaymentForTariff();
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

        [HasMany(ColumnKey = "Client", Lazy = true, Cascade = ManyRelationCascadeEnum.All)]
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


        public virtual decimal GetInterval()
		{
			return (((DateTime)RatedPeriodDate).AddMonths(1) - (DateTime)RatedPeriodDate).Days + DebtDays;
		}

        public virtual bool CanBlock()
        {
            if (ClientServices != null)
            {
                var CServ =
                    ClientServices.Where(c => c.Service == Service.GetByType(typeof (DebtWork))).FirstOrDefault();
                if (CServ != null && !CServ.Service.CanBlock(CServ))
                {
                    return false;
                }
            }

            /*if (PostponedPayment != null && (PostponedPayment.Value.AddDays(1) - SystemTime.Now()).TotalHours >= 0)
                return false;*/

            if ((Disabled) || (PhysicalClient.Balance > 0))
                return false;
            return true;
        }


        public virtual IList<WriteOff> GetWriteOffs(string groupedKey)
        {
            IList<WriteOff> writeOffs = new List<WriteOff>();
            var gpoupKey = "concat(YEAR(WriteOffDate),'-',MONTH(WriteOffDate),'-',DAYOFMONTH(WriteOffDate))";
            if (groupedKey == "day")
                gpoupKey = "concat(YEAR(WriteOffDate),'-',MONTH(WriteOffDate),'-',DAYOFMONTH(WriteOffDate))";
            if (groupedKey == "month")
                gpoupKey = "concat(YEAR(WriteOffDate),'-',MONTH(WriteOffDate))";
            if (groupedKey == "year")
                gpoupKey = "YEAR(WriteOffDate)";
            ARSesssionHelper<WriteOff>.QueryWithSession(session =>
            {
                var query =
                    session.CreateSQLQuery(string.Format(
@"SELECT id, Sum(WriteOffSum) as WriteOffSum, WriteOffDate, Client  FROM internet.WriteOff W
where Client = :clientid
group by {0}", gpoupKey)).AddEntity(typeof(WriteOff));
                query.SetParameter("clientid", Id);
                writeOffs = query.List<WriteOff>();
                return query.List<WriteOff>();
            });
            return writeOffs;
        }

		public virtual ClientConnectInfo GetConnectInfo()
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
CE.Monitoring
from internet.ClientEndpoints CE
join internet.NetworkSwitches NS on NS.Id = CE.Switch
join internet.Clients C on CE.Client = C.Id
left join internet.Leases L on L.Endpoint = CE.Id
left join internet.PackageSpeed PS on PS.PackageId = CE.PackageId
where CE.Client = {0}",
Id)).SetResultTransformer(
new AliasToPropertyTransformer(
typeof(ClientConnectInfo)))
.List<ClientConnectInfo>();
						ConnectInfo = query;
						return query;
					});
					if (ConnectInfo.Count != 0)
						return ConnectInfo.First();
				}
			return new ClientConnectInfo();
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

        public virtual decimal GetPrice()
        {
            var servisesPrice = 0m;
            if (ClientServices != null)
            {
                var blockingService = ClientServices.Where(c => c.Service.BlockingAll).ToList().FirstOrDefault();
                if (blockingService != null)
                    return blockingService.GetPrice();

                servisesPrice = ClientServices.Sum(c => c.GetPrice());
            }

            if ((PhysicalClient.Tariff.FinalPriceInterval == 0 || PhysicalClient.Tariff.FinalPrice == 0) && !Disabled)
                return PhysicalClient.Tariff.Price + servisesPrice;

            if ((BeginWork != null && BeginWork.Value.AddMonths(PhysicalClient.Tariff.FinalPriceInterval) <= SystemTime.Now()) && !Disabled)
                return PhysicalClient.Tariff.FinalPrice + servisesPrice;

            return 0;
        }
	}
}