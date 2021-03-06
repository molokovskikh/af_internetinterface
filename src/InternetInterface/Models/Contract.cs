﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "internet")]
	public class Contract
	{
		public Contract()
		{
			Date = SystemTime.Now();
		}

		public Contract(Order order) : this()
		{
			Order = order;
			Customer = order.Client.Name;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public DateTime Date { get; set; }

		[BelongsTo(Column = "OrderId")]
		public virtual Order Order { get; set; }

		[Property]
		public string Customer { get; set; }
	}
}