﻿using System;

namespace Inforoom2.Helpers
{
	public class StringHepler
	{
		/// <summary>
		/// Показать промежуток времени, оставшийся до некоторого события
		/// </summary>
		public static string ShowTimeLeft(TimeSpan leftEventTime)
		{
			var timeStr = "";
			if (leftEventTime.Days > 0) {
				timeStr += leftEventTime.Days;
				timeStr += (leftEventTime.Days%20 == 1) ? " день." : ((leftEventTime.Days%20 >= 5) ? " дней." : " дня.");
			}
			else {
				if (leftEventTime.Hours > 0) {
					timeStr += leftEventTime.Hours + " ч. ";
				}
				timeStr += leftEventTime.Minutes + " мин.";
			}
			return timeStr;
		}
	}
}