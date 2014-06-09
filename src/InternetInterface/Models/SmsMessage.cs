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
		{
			CreateDate = DateTime.Now;
		}

		public SmsMessage(string phone)
			: this()
		{
			PhoneNumber = phone;
			if (!PhoneNumber.StartsWith("+7")) {
				PhoneNumber = "+7" + PhoneNumber;
			}
		}

		public SmsMessage(Client client, string text, DateTime? shouldBeSend = null)
			: this()
		{
			var contact = client.Contacts
				.FirstOrDefault(c => c.Type == ContactType.SmsSending && !string.IsNullOrEmpty(c.Text) && Regex.IsMatch(c.Text, @"^(9)\d{9}"));
			if (contact == null)
				throw new Exception(String.Format("Для клиента {0} не найдена контактная информация для отправки sms", client.Id));
			PhoneNumber = "+7" + contact.Text;
			Client = client;
			Text = text;
			ShouldBeSend = shouldBeSend ?? DateTime.Today.AddDays(1).Add(new TimeSpan(12, 00, 00));
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

		public virtual bool IsFaulted { get; set; }

		[BelongsTo]
		public virtual Partner Registrator { get; set; }

		public static SmsMessage TryCreate(Client client, string text, DateTime? shouldBeSend)
		{
			var contact = client.Contacts
				.FirstOrDefault(c => c.Type == ContactType.SmsSending && !string.IsNullOrEmpty(c.Text) && Regex.IsMatch(c.Text, @"^(9)\d{9}"));
			if (contact == null)
				return null;
			return new SmsMessage(client, text, shouldBeSend);
		}
	}
}