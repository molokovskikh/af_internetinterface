using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Models
{
	[ActiveRecord("UserWriteOffs", Schema = "internet", Lazy = true)]
	public class UserWriteOff : ActiveRecordLinqBase<UserWriteOff>
	{
		public UserWriteOff()
		{
		}

		public UserWriteOff(Client client, decimal sum, string comment, bool setRegistrator = true)
			: this(client, setRegistrator)
		{
			Sum = sum;
			Comment = comment;
		}

		public UserWriteOff(Client client, bool setRegistrator = true)
		{
			Client = client;
			Date = DateTime.Now;
			if (setRegistrator)
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

		public override string ToString()
		{
			return String.Format("{0} - {1:C}", Comment, Sum);
		}
	}
}