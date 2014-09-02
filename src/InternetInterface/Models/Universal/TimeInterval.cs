using System;
using System.Collections.Generic;
using System.Linq;

namespace InternetInterface.Models
{
	public class TimeInterval
	{
		public TimeSpan Begin { get; set; }
		public TimeSpan End { get; set; }
		public bool Busy { get; set; }
		public ConnectGraph Request { get; set; }
		public string InStringFormat { get; set; }

		public TimeInterval(DateTime begin, TimeSpan step, IEnumerable<ConnectGraph> requests, ref uint index)
		{
			Begin = begin.TimeOfDay;
			End = begin.TimeOfDay.Add(step);
			InStringFormat = ToString();
			Request = requests.FirstOrDefault(r => r.DateAndTime >= begin && r.DateAndTime < begin.Add(step));
			Busy = Request != null;
			if (Busy)
				Request.IntervalId = index;
		}

		public static List<TimeInterval> FromRequests(DateTime date, Brigad brigad, List<ConnectGraph> requests)
		{
			var begin = date.Add(brigad.WorkBegin ?? Brigad.DefaultWorkBegin);
			var end = date.Add(brigad.WorkEnd ?? Brigad.DefaultWorkEnd);
			var step = brigad.WorkStep ?? Brigad.DefaultWorkStep;
			uint i = 0;
			return begin.Step(step).TakeWhile(t => t < end)
				.Select(d => new TimeInterval(d, step, requests, ref i))
				.ToList();
		}

		public override string ToString()
		{
			return Begin.ToString(@"hh\:mm") + " - " + End.ToString(@"hh\:mm");
		}
	}
}
