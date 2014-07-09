using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using Castle.Core.Smtp;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Mails;

namespace InternetInterface.Models
{
	[ActiveRecord("UserWriteOffs", Schema = "internet", Lazy = true), LogInsert(typeof(LawyerUserWriteOffNotice))]
	public class UserWriteOff : ActiveRecordLinqBase<UserWriteOff>, IWriteOff
	{
		public UserWriteOff()
		{
		}

		public UserWriteOff(Client client, decimal sum, string comment)
			: this(client)
		{
			Sum = sum;
			Comment = comment;
		}

		public UserWriteOff(Client client)
		{
			Client = client;
			Date = DateTime.Now;
			Registrator = InitializeContent.TryGetPartner();
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

		public override string ToString()
		{
			return String.Format("{0} - {1:C}", Comment, Sum);
		}

		public virtual IEmailSender Sender { get; set; }

		public virtual Appeals Cancel()
		{
			if (Client.PhysicalClient != null) {
				Client.PhysicalClient.MoneyBalance += Sum;
				Client.PhysicalClient.Balance += Sum;
			}
			else
				Client.LawyerPerson.Balance += Sum;
			return Client.CreareAppeal(String.Format("Удалено списание на сумму {0:C}", Sum));
		}
	}
}