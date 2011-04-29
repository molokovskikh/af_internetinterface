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

		/*[Property]
		public virtual DateTime PreRatedPeriodDate { get; set; }*/

		[Property]
		public virtual int DebtDays { get; set; }

		/*[Property]
		public virtual bool FirstLease { get; set; }*/

		[Property]
		public virtual bool ShowBalanceWarningPage { get; set; }

		/*[Property]
		public virtual bool SayDhcpIsNewClient  { get; set; }*/

		[Property]
		public virtual DateTime? BeginWork { get; set; }

		[BelongsTo]
		public virtual LawyerPerson LawyerPerson { get; set; }

		/*[Property]
		public virtual bool SayBillingIsNewClient  { get; set; }*/


		public virtual decimal GetInterval()
		{
			return (((DateTime)RatedPeriodDate).AddMonths(1) - (DateTime)RatedPeriodDate).Days + DebtDays;
		}


		public virtual PhisicalClientConnectInfo GetConnectInfo()
		{
			if ((PhysicalClient!= null && PhysicalClient.Status != null && PhysicalClient.Status.Connected) ||
				(LawyerPerson != null && LawyerPerson.Status != null && LawyerPerson.Status.Connected))
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