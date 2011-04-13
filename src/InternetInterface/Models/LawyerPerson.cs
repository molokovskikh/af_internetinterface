using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "Internet", Table = "LawyerPerson")]
	public class LawyerPerson : ActiveRecordLinqBase<LawyerPerson>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public decimal Tariff { get; set; }

		[Property]
		public decimal Balance { get; set; }

		[BelongsTo]
		public Clients Client { get; set; }
	}
}