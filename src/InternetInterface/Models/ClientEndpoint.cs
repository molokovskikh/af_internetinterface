using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Castle.ActiveRecord;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.Models.Audit;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("ClientEndpoints", Schema = "Internet", Lazy = true), Auditable]
	public class ClientEndpoint : ChildActiveRecordLinqBase<ClientEndpoint>
	{
		public ClientEndpoint()
		{
			StaticIps = new List<StaticIp>();
		}

		public ClientEndpoint(Client client, int? port, NetworkSwitch @switch)
			: this()
		{
			Client = client;
			Port = port;
			Switch = @switch;

			var physicalClient = client.PhysicalClient;
			if (physicalClient != null)
				physicalClient.UpdatePackageId(this);
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual bool Disabled { get; set; }

		[Property(ColumnType = "InternetInterface.Helpers.IPUserType, InternetInterface"), Auditable("Фиксированный IP")]
		public virtual IPAddress Ip { get; set; }

		[BelongsTo(Cascade = CascadeEnum.SaveUpdate), Auditable]
		public virtual Client Client { get; set; }

		[Property, Auditable("Порт")]
		public virtual int? Port { get; set; }

		[BelongsTo("Switch"), Auditable("Коммутатор")]
		public virtual NetworkSwitch Switch { get; set; }

		[Property, Auditable("Мониторинг")]
		public virtual bool Monitoring { get; set; }

		[Property, Auditable("PackageId")]
		public virtual int? PackageId { get; set; }

		[Property]
		public virtual int? MaxLeaseCount { get; set; }

		[Property]
		public virtual uint? Pool { get; set; }

		[BelongsTo(Lazy = FetchWhen.OnInvoke)]
		public virtual Brigad WhoConnected { get; set; }

		[OneToOne(PropertyRef = "EndPoint")]
		public virtual PaymentForConnect PayForCon { get; set; }

		[Property]
		public virtual int? ActualPackageId { get; set; }

		public virtual void UpdateActualPackageId(int? packageId)
		{
			ActualPackageId = packageId;
		}

		[HasMany(ColumnKey = "EndPoint", OrderBy = "Ip", Lazy = true, Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public virtual IList<StaticIp> StaticIps { get; set; }

		public virtual bool IsMultilease
		{
			get { return Pool != null || (MaxLeaseCount != null && MaxLeaseCount > 1); }
		}

		public static ClientEndpoint GetForIp(IPAddress ip, ISession session)
		{
			var lease = session.Query<Lease>().FirstOrDefault(l => l.Ip == ip);
			if (lease != null) {
				return session.Query<ClientEndpoint>().FirstOrDefault(e => e.Port == lease.Port && e.Switch == lease.Switch);
			}
			return null;
		}

		public static bool HavePoint(ISession session, IPAddress ip)
		{
			return GetForIp(ip, session) != null;
		}
	}
}