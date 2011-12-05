using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "Internet", Table = "Leases")]
	public class Lease : ActiveRecordLinqBase<Lease>
	{
		private const string IPRegExp =
	@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";

		[PrimaryKey]
		public uint Id { get; set; }

		[Property("`ip`")]
		public uint Ip { get; set; }

		[BelongsTo(Cascade = CascadeEnum.SaveUpdate)]
		public ClientEndpoints Endpoint { get; set; }

		[Property]
		public int Port { get; set; }

		[BelongsTo]
		public NetworkSwitches Switch { get; set; }

		[Property]
		public DateTime LeaseBegin { get; set; }

		[Property]
		public DateTime LeaseEnd { get; set; }

		[Property]
		public string LeasedTo { get; set; }

		[Property]
		public virtual uint? Pool { get; set; }

		[Property]
		public virtual int Module { get; set; }
	}
}