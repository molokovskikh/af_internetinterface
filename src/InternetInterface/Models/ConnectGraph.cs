using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Castle.ActiveRecord;
using ExcelLibrary.BinaryFileFormat;
using InternetInterface.Models.Universal;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("ConnectGraph", Schema = "internet", Lazy = true)]
	public class ConnectGraph : ValidActiveRecordLinqBase<ConnectGraph>, ISerializable
	{
		public ConnectGraph()
		{
		}

		public ConnectGraph(Client client, DateTime day, Brigad brigad)
		{
			Client = client;
			DateAndTime = day;
			Brigad = brigad;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual uint IntervalId { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }

		[Property]
		public virtual DateTime DateAndTime { get; set; }

		[Property]
		public virtual bool IsReserved { get; set; }

		[BelongsTo]
		public virtual Brigad Brigad { get; set; }

		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("interval", IntervalId, typeof(string));
			info.AddValue("isReserved", IsReserved, typeof(bool));
			info.AddValue("clientId", Client.Id, typeof(string));
			info.AddValue("Id", Id, typeof(string));
		}
	}
}