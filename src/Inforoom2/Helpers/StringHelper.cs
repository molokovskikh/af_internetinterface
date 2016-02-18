using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Transform;

namespace Inforoom2.Helpers
{
	public class StringHelper
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

	public static class StringStaticHelper
	{
		public static string CutAfter(this string self, int charsNumber, string postChars = "...")
		{
			return !string.IsNullOrEmpty(self) && self.Length > charsNumber ? self.Substring(0, charsNumber) + postChars : self;
		}

		public static string CutForBrowser(this string self, int charsNumber)
		{
			var stArray = self.Split(' ');
			var stResult = "";
			for (int i = 0; i < stArray.Length; i++)
			{
				var chArray = new List<char>();
                for (int j = 0; j< stArray[i].Length; j++) {
	                if (j>= charsNumber && j % charsNumber == 0)
					{
						chArray.AddRange("<wbr>".ToCharArray()); 
					}
		                  chArray.Add(stArray[i][j]);
                }
				stArray[i] = string.Join("", chArray);
			}
			self = string.Join(" ", stArray);
            return self;
		}
	}
}