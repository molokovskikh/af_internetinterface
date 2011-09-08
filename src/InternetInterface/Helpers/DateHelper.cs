using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetInterface.Helpers
{
	public class Week
	{
		private Week()
		{}

		private readonly DateTime _startDate ;
		private readonly DateTime _endDate ;

		public Week(DateTime startDate, DateTime endDate)
		{
			_startDate = startDate;
			_endDate = endDate;
		}

		public DateTime StartDate
		{
			get { return _startDate; }
		}

		public DateTime EndDate
		{
			get { return _endDate; }
		}
	}

	public class DateHelper
	{
		public static Week GetWeekInterval(DateTime day)
		{
			var indexDay = (int)day.DayOfWeek;
			var startDate = day.AddDays(-indexDay + 1);
			var endDate = day.AddDays(7 - indexDay);
			return new Week(startDate, endDate);
		}
	}
}