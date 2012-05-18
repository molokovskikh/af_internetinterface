﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Models
{
	[ActiveRecord("MessagesForClients", Schema = "internet", Lazy = true), Auditable]
	public class MessageForClient : ActiveRecordLinqBase<MessageForClient>
	{
		public MessageForClient()
		{
			RegDate = DateTime.Now;
			EndDate = DateTime.Now.AddDays(1);
			Enabled = true;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, Auditable("Текст сообщения"), ValidateNonEmpty("Введите сообщение")]
		public virtual string Text { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }

		[Property, Auditable("Показывать")]
		public virtual bool Enabled { get; set; }

		[Property, Auditable("Дата отключения"), ValidateDate("Неравильный формат даты")]
		public virtual DateTime EndDate { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[BelongsTo]
		public virtual Partner Registrator { get; set; }
	}
}