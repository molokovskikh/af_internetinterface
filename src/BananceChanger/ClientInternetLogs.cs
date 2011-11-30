using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using InternetInterface.Models;

namespace BananceChanger
{
	[ActiveRecord("ClientInternetLogs", Schema = "logs", Lazy = true)]
	public class ClientInternetLogs : ActiveRecordLinqBase<ClientInternetLogs>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime LogTime { get; set; }

		[BelongsTo("ClientId")]
		public virtual Client Client { get; set; }

		[Property]
		public virtual string OperatorName { get; set; }

		[Property]
		public virtual bool Disabled { get; set; }
	}
}
