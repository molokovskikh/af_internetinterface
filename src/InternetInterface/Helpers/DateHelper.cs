using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetInterface.Helpers
{
	public class Week
	{
		private readonly DateTime _startDate;
		private readonly DateTime _endDate;

		public Week()
		{
			var interval = DateHelper.GetWeekInterval(DateTime.Now);
			_startDate = interval.StartDate;
			_endDate = interval.EndDate;
		}

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

		public string GetStartString()
		{
			return _startDate.ToShortDateString();
		}

		public string GetEndString()
		{
			return _endDate.ToShortDateString();
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

		public static bool IncludeDateInInterval(Week interval, DateTime date)
		{
			return interval.StartDate.Date >= date && interval.EndDate.Date <= date;
		}

		public static bool IncludeDateInCurrentInterval(DateTime date)
		{
			var interval = GetWeekInterval(DateTime.Now);
			return IncludeDateInInterval(interval, date);
		}
	}
}