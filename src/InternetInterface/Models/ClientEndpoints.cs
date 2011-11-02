using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Linq;
using NHibernate.Criterion;

namespace InternetInterface.Models
{
	[ActiveRecord("ClientEndpoints", Schema = "Internet", Lazy = true)]
	public class ClientEndpoints : ChildActiveRecordLinqBase<ClientEndpoints>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual bool Disabled { get; set; }

		[Property]
		public virtual string Ip { get; set; }

		[BelongsTo("Client", Cascade = CascadeEnum.SaveUpdate)]
		public virtual Client Client { get; set; }

		[Property]
		public virtual int Module { get; set; }

		[Property]
		public virtual int? Port { get; set; }

		[BelongsTo("Switch")]
		public virtual NetworkSwitches Switch { get; set; }

		[Property]
		public virtual bool Monitoring { get; set; }

		[Property]
		public virtual int PackageId { get; set; }

		[Property]
		public virtual int? MaxLeaseCount { get; set; }

		[Property]
		public virtual uint? Pool { get; set; }

		[OneToOne]
		public virtual PaymentForConnect PayForCon { get; set; }

		[HasMany(ColumnKey = "EndPoint", OrderBy = "Ip", Lazy = true)]
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
			if (ClientEndpoints.FindAll(DetachedCriteria.For(typeof(ClientEndpoints))
								.Add(Expression.Eq("Switch.Id", _Switch))
								.Add(Expression.Eq("Port", _Port))).Length == 0)
				return true;
			return false;
		}
	}
}