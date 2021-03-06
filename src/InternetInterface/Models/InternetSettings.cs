﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord("InternetSettings", Schema = "Internet", Lazy = true)]
	public class InternetSettings : ActiveRecordLinqBase<InternetSettings>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime NextBillingDate { get; set; }

		[Property]
		public virtual bool LastStartFail { get; set; }

		[Property, Description("Дата следующей проверки целостности БД")]
		public virtual DateTime? NextDataAuditDate { get; set; }
	}
}