using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.Helpers;

namespace InternetInterface.Models
{
	[ActiveRecord("RequestMessages", Schema = "Internet", Lazy = true)]
	public class RequestMessage : ChildActiveRecordLinqBase<RequestMessage>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		[BelongsTo]
		public virtual Partner Registrator { get; set; }

		[BelongsTo]
		public virtual Request Request { get; set; }

		public virtual string Text
		{
			get { return AppealHelper.GetTransformedAppeal(Comment); }
		}
	}
}