using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	public enum AppealType
	{
		All = 0,
		User = 1,
		System = 3
	}

	[ActiveRecord("Appeals", Schema = "internet", Lazy = true)]
	public class Appeals : ValidActiveRecordLinqBase<Appeals>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Appeal { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		[BelongsTo("Partner")]
		public virtual Partner Partner { get; set; }

		[BelongsTo("Client")]
		public virtual Client Client { get; set; }

		[Property]
		public virtual int AppealType { get; set; }

		public virtual string GetTransformedAppeal()
		{
			return AppealHelper.GetTransformedAppeal(Appeal);
		}

		public static void CreareAppeal(string message, Client client, AppealType type)
		{
			new Appeals
			{
				Appeal = message,
				Client = client,
				AppealType = (int)type,
				Date = DateTime.Now,
				Partner = InitializeContent.Partner
			}.Save();
		}

		public virtual string Type()
		{
			var type = string.Empty;
			if (AppealType == (int)Models.AppealType.User)
				type = "Пользовательское";
			if (AppealType == (int)Models.AppealType.System)
				type = "Системное";
			return type;
		}
	}
}