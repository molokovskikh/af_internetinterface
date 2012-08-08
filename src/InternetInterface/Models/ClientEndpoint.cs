using System.Collections.Generic;
using Castle.ActiveRecord;
using Common.Web.Ui.Helpers;
using NHibernate.Criterion;

namespace InternetInterface.Models
{
	[ActiveRecord("ClientEndpoints", Schema = "Internet", Lazy = true), Auditable]
	public class ClientEndpoint : ChildActiveRecordLinqBase<ClientEndpoint>
	{
		public ClientEndpoint()
		{}

		public ClientEndpoint(Client client, int? port, NetworkSwitches @switch)
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

		[Property, Auditable("Фиксированный IP")]
		public virtual string Ip { get; set; }

		[BelongsTo(Cascade = CascadeEnum.SaveUpdate), Auditable]
		public virtual Client Client { get; set; }

		[Property]
		public virtual int Module { get; set; }

		[Property, Auditable("Порт")]
		public virtual int? Port { get; set; }

		[BelongsTo("Switch"), Auditable("Свитч")]
		public virtual NetworkSwitches Switch { get; set; }

		[Property, Auditable("Мониторинг")]
		public virtual bool Monitoring { get; set; }

		[Property, Auditable("PackageId")]
		public virtual int? PackageId { get; set; }

		[Property]
		public virtual int? MaxLeaseCount { get; set; }

		[Property]
		public virtual uint? Pool { get; set; }

		[OneToOne]
		public virtual PaymentForConnect PayForCon { get; set; }

		[HasMany(ColumnKey = "EndPoint", OrderBy = "Ip", Lazy = true, Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public virtual IList<StaticIp> StaticIps { get; set; }

		public virtual bool IsMultilease
		{
			get { return Pool != null || (MaxLeaseCount != null && MaxLeaseCount > 1); }
		}
	}

	public class Point
	{
		public static bool isUnique(uint _Switch, int _Port)
		{
			if (ClientEndpoint.FindAll(DetachedCriteria.For(typeof(ClientEndpoint))
								.Add(Expression.Eq("Switch.Id", _Switch))
								.Add(Expression.Eq("Port", _Port))).Length == 0)
				return true;
			return false;
		}
	}
}