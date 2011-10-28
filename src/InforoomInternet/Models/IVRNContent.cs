using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.Controllers;

namespace InforoomInternet.Models
{
	[ActiveRecord(Schema = "Internet", Table = "IVRNContent")]
	public class IVRNContent : ActiveRecordLinqBase<IVRNContent>, IContent
	{

		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public string Content { get; set; }

		[Property]
		public string ViewName { get; set; }
	}
}