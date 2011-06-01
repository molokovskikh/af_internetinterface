using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Common.Models.Helpers;
using InternetInterface.Helpers;

namespace InternetInterface.Models
{
	public enum ClientType
	{
		Phisical = 1,
		Legal = 2
	}

	[ActiveRecord("Clients", Schema = "Internet", Lazy = true)]
	public class Clients : ChildActiveRecordLinqBase<Clients>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual bool Disabled { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual ClientType Type { get; set; }

		[BelongsTo("PhysicalClient", Lazy = FetchWhen.OnInvoke)]
		public virtual PhysicalClients PhysicalClient { get; set; }

		[Property]
		public virtual DateTime? RatedPeriodDate { get; set; }

		[Property]
		public virtual int DebtDays { get; set; }

		[Property]
		public virtual bool ShowBalanceWarningPage { get; set; }

		[Property]
		public virtual DateTime? BeginWork { get; set; }

		[BelongsTo]
		public virtual LawyerPerson LawyerPerson { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[BelongsTo("WhoRegistered")]
		public virtual Partner WhoRegistered { get; set; }

		[Property]
		public virtual string WhoRegisteredName { get; set; }

		[BelongsTo("WhoConnected")]
		public virtual Brigad WhoConnected { get; set; }

		[Property]
		public virtual string WhoConnectedName { get; set; }

		[Property]
		public virtual DateTime ConnectedDate { get; set; }

		[Property]
		public virtual bool AutoUnblocked { get; set; }

		[BelongsTo]
		public virtual Status Status { get; set; }

		[BelongsTo]
		public virtual AdditionalStatus AdditionalStatus { get; set; }

		public virtual bool AdditionalCanUsed(string aStatus)
		{
            if (AdditionalStatus != null && AdditionalStatus.Id == (uint)AdditionalStatusType.Refused)
                return false;
		    return Status.Additional.Contains(AdditionalStatus.Queryable.First(a => a.ShortName == aStatus));
		}

		[HasMany(ColumnKey = "Client", OrderBy = "PaidOn")]
		public virtual IList<Payment> Payments { get; set; }

        public virtual string ChangePhysicalClientPassword()
        {
            var pass =  CryptoPass.GeneratePassword();
            PhysicalClient.Password = CryptoPass.GetHashString(pass);
            PhysicalClient.Update();
            return pass;
        }


	    public virtual ClientType GetClientType()
		{
			if (PhysicalClient != null)
				return ClientType.Phisical;
			if (LawyerPerson != null)
				return ClientType.Legal;    
			return ClientType.Phisical;
		}

		public virtual decimal GetInterval()
		{
			return (((DateTime)RatedPeriodDate).AddMonths(1) - (DateTime)RatedPeriodDate).Days + DebtDays;
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

		public virtual PhisicalClientConnectInfo GetConnectInfo()
		{
			if ((PhysicalClient!= null && Status != null && Status.Connected) ||
				(LawyerPerson != null && Status != null && Status.Connected))
			{
				//var client = Clients.FindAllByProperty("PhysicalClient", this);
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
Id)).SetResultTransformer(
new AliasToPropertyTransformer(
typeof(PhisicalClientConnectInfo)))
.List<PhisicalClientConnectInfo>();
						ConnectInfo = query;
						return query;
					});
					if (ConnectInfo.Count != 0)
						return ConnectInfo.First();
				}
			return new PhisicalClientConnectInfo();
		}
	}
}