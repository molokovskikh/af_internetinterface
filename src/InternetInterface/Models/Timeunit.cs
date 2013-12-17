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
			var begin = date.Add(partner.WorkBegin ?? Partner.DefaultWorkBegin);
			var end = date.Add(partner.WorkEnd ?? Partner.DefaultWorkEnd);
			var step = partner.WorkStep ?? Partner.DefaultWorkStep;
			return begin.Step(step).TakeWhile(t => t < end)
				.Select(d => new Timeunit(d, step, requests))
				.ToList();
		}

		public override string ToString()
		{
			return String.Format("{0} - {1}", Begin, End);
		}
	}
}