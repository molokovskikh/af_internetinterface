using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using InternetInterface.Helpers;

namespace InternetInterface.Models
{
	[ActiveRecord("SmsMessages", Schema = "internet", Lazy = true)]
	public class SmsMessage : ActiveRecordLinqBase<SmsMessage>
	{
		public SmsMessage()
		{}

		public SmsMessage(Client client, string text, DateTime? shouldBeSend = null)
		{
			if (client.Contacts != null) {
				var contact =
					client.Contacts.Where(
						c => c.Type == ContactType.SmsSending && !string.IsNullOrEmpty(c.Text) && Regex.IsMatch(c.Text, @"^(9)\d{9}")).
						FirstOrDefault();
				PhoneNumber = contact != null ? "+7" + contact.Text : null;
			}
			Client = client;
			CreateDate = DateTime.Now;
			Text = text;
			if (shouldBeSend == null) {
				var dtnAd = DateTime.Now.AddDays(1);
				ShouldBeSend = new DateTime(dtnAd.Year, dtnAd.Month, dtnAd.Day, 12, 00, 00);
			}
			else {
				ShouldBeSend = shouldBeSend;
			}
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime CreateDate { get; set; }

		[Property]
		public virtual DateTime? SendToOperatorDate { get; set; }

		[Property]
		public virtual DateTime? ShouldBeSend { get; set; }

		[Property]
		public virtual string Text { get; set; }

		[Property]
		public virtual string PhoneNumber { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }

		[Property]
		public virtual bool IsSended { get; set; }

		[Property]
		public virtual string SMSID { get; set; }

		[Property]
		public virtual int ServerRequest { get; set; }
	}
}