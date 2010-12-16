﻿using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("Tariffs", Schema = "internet", Lazy = true)]
	public class Tariff : ChildActiveRecordLinqBase<Tariff>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string Description { get; set; }

		[Property]
		public virtual int Price { get; set; }

		[Property]
		public virtual int PackageId { get; set; }
	}

}