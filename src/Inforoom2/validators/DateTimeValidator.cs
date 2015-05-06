using System;
using Common.Tools;

namespace Inforoom2.validators
{
	enum DateTimeType
	{
		Now = 1,
		Min = 2,
		Max = 3
	}

	class DateTimeValidator : CustomValidator
	{
		protected DateTime Start;
		protected DateTime End;
		protected bool DateTimeFlag;

		public DateTimeValidator(string start, string end, bool timeFlag = false)
		{
			init(DateTime.Parse(start), DateTime.Parse(end), timeFlag);
		}

		public DateTimeValidator(string start, DateTimeType end, bool timeFlag = false)
		{
			init(DateTime.Parse(start), ParseType(end), timeFlag);
		}
		public DateTimeValidator(DateTimeType start, string end, bool timeFlag = false)
		{
			init(ParseType(start), DateTime.Parse(end), timeFlag);
		}
		protected void init(DateTime start, DateTime end, bool timeFlag)
		{
			Start = start;
			End = end;
			DateTimeFlag = timeFlag;
		}
		protected override void Run(object value)
		{
			var val = value as DateTime?;
			if (!val.HasValue)
				AddError("Отсутствует значение");
			else if (val > End || val < Start)
				AddError("Значение должно быть в диапозоне " + Stringify(Start) + " - " + Stringify(End));
		}

		protected DateTime ParseType(DateTimeType type)
		{
			switch (type)
			{
				case DateTimeType.Max: return DateTime.MaxValue;
				case DateTimeType.Min: return DateTime.MinValue;
				case DateTimeType.Now: return SystemTime.Now();
			}
			return SystemTime.Now();
		}

		protected string Stringify(DateTime date)
		{
			return DateTimeFlag ? date.ToShortTimeString() : date.ToShortDateString();
		}
	}
}