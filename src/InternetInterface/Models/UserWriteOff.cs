﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Models
{
	[ActiveRecord("UserWriteOffs", Schema = "internet", Lazy = true)]
	public class UserWriteOff : ActiveRecordLinqBase<UserWriteOff>
	{
		public UserWriteOff()
		{}

		public UserWriteOff(Client client)
		{
			Client = client;
			Date = DateTime.Now;
			Registrator = InitializeContent.Partner;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateDecimal("Должно быть введено число"), ValidateGreaterThanZero]
		public virtual decimal Sum { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		[BelongsTo("Client")]
		public virtual Client Client { get; set; }

		[Property]
		public virtual bool BillingAccount { get; set; }

		[Property, ValidateNonEmpty("Введите комментарий")]
		public virtual string Comment { get; set; }

		[BelongsTo]
		public virtual Partner Registrator { get; set; }
	}
}