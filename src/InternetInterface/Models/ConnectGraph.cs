﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{

	public class Intervals
	{
		public static List<string> GetIntervals()
		{
			return new List<string>
			       	{
			       		"9:30 - 10:30",
						"10:30 - 11:30",
						"11:30 - 12:30",
						"12:30 - 13:30",
						"13:30 - 14:30",
						"14:30 - 15:30",
						"15:30 - 16:30",
						"16:30 - 17:30"
			       	};
		}
	}

	[ActiveRecord("ConnectGraph", Schema = "internet", Lazy = true)]
	public class ConnectGraph : ValidActiveRecordLinqBase<ConnectGraph>
	{
		//private List<string> _intervals;
/*
		public ConnectGraph()
		{
			_intervals.Add("9:30 - 10:30");
			_intervals.Add("10:30 - 11:30");
			_intervals.Add("11:30 - 12:30");
			_intervals.Add("12:30 - 13:30");
			_intervals.Add("13:30 - 14:30");
			_intervals.Add("14:30 - 15:30");
			_intervals.Add("15:30 - 16:30");
			_intervals.Add("16:30 - 17:30");
		}

		public virtual string GetInterval()
		{
			return _intervals[IntervalId];
		}*/

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual uint IntervalId { get; set; }

		[BelongsTo]
		public virtual Clients Client { get; set; }

		[Property]
		public virtual DateTime Day { get; set; }

		[BelongsTo]
		public virtual Brigad Brigad { get; set; }
	}

}