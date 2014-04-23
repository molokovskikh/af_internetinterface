using System;
using System.Collections;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Components.Validator;
using InternetInterface.Models;

namespace InforoomInternet.Models
{
	[ActiveRecord(Schema = "Internet")]
	public class Street
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property(NotNull = true)]
		public virtual string Name { get; set; }
	}
}