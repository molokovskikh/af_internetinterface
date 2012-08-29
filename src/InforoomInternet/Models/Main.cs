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
	public class Street : ActiveRecordLinqBase<Street>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property(NotNull = true)]
		public virtual string Name { get; set; }

		public static void GetStreetList(string qString, TextWriter writer)
		{
			var subs = qString.Split(' ');

			foreach (var sub in subs) {
				var tempRes = Queryable.Where(n => n.Name.Contains(sub)).ToList();
				if (tempRes.Count == 0) {
					continue;
				}
				foreach (var str in tempRes) {
					writer.Write("{0}\n", str.Name);
				}
			}
		}
	}
}