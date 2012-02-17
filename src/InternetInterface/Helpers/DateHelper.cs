using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetInterface.Helpers
{
	public class Week
	{
		private DateTime _startDate;
		private DateTime _endDate;

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
			set { _startDate = value; }
		}

		public DateTime EndDate
		{
			get { return _endDate; }
			set { _endDate = value; }
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

	public static class DateHelper
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

		public static int TotalMonth(this DateTime lValue, DateTime rValue)
		{
			var spin = (lValue - rValue).Ticks;
			var interval = new DateTime(spin);
			return (interval.Month - 1) + 12 * (interval.Year - 1);
			//return Math.Abs((lValue.Month - rValue.Month) + 12 * (lValue.Year - rValue.Year));
		}
	}
}