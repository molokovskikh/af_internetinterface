using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InforoomInternet.Models
{
	[ActiveRecord(Schema = "Internet", Table = "IVRNContent")]
	public class IVRNContent : ActiveRecordLinqBase<IVRNContent>
	{

		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public string Content { get; set; }

		[Property]
		public string ViewName { get; set; }
	}
}