using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using InternetInterface.Models;

namespace BananceChanger
{
	[ActiveRecord("PhysicalClientInternetLogs", Schema = "logs", Lazy = true)]
	public class PhysicalClientInternetLogs : ActiveRecordLinqBase<PhysicalClientInternetLogs>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime LogTime { get; set; }

		[BelongsTo]
		public virtual PhysicalClients ClientId { get; set; }

		[Property]
		public virtual string OperatorName { get; set; }

		[Property]
		public virtual decimal? Balance { get; set; }

	}
}
