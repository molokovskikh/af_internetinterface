using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Inforoom2.validators
{
	enum DateTimeType
	{
		Now = 1,
		Min = 2,
		Max = 3
	}

	class ValidatorDateTime : CustomValidator
	{
		protected DateTime Start;
		protected DateTime End;
		protected bool DateTimeFlag;

		public ValidatorDateTime(string start, string end, bool timeFlag = false)
		{
			init(DateTime.Parse(start), DateTime.Parse(end), timeFlag);
		}

		public ValidatorDateTime(string start, DateTimeType end, bool timeFlag = false)
		{
			init(DateTime.Parse(start), ParseType(end), timeFlag);
		}
		public ValidatorDateTime(DateTimeType start, string end, bool timeFlag = false)
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
				case DateTimeType.Now: return DateTime.Now;
			}
			return DateTime.Now;
		}
		protected string Stringify(DateTime date)
		{
			return DateTimeFlag ? date.ToShortTimeString() : date.ToShortDateString();
		}
	}
}