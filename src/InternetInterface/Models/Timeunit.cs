using System;
using System.Collections.Generic;
using System.Linq;

namespace InternetInterface.Models
{
	public class Timeunit
	{
		public Timeunit(DateTime begin, TimeSpan step, IEnumerable<ServiceRequest> requests)
		{
			Begin = begin.TimeOfDay;
			End = begin.TimeOfDay.Add(step);
			Request = requests.FirstOrDefault(r => r.PerformanceDate >= begin && r.PerformanceDate < begin.Add(step));
			Busy = Request != null;
		}

		public TimeSpan Begin { get; set; }
		public TimeSpan End { get; set; }
		public bool Busy { get; set; }
		public ServiceRequest Request { get; set; }

		public static List<Timeunit> FromRequests(DateTime date, Partner partner, List<ServiceRequest> requests)
		{
			var begin = date.Add(partner.WorkBegin);
			var end = date.Add(partner.WorkEnd);
			return begin.Step(partner.WorkStep).TakeWhile(t => t < end)
				.Select(d => new Timeunit(d, partner.WorkStep, requests))
				.ToList();
		}

		public override string ToString()
		{
			return String.Format("{0} - {1}", Begin, End);
		}
	}
}