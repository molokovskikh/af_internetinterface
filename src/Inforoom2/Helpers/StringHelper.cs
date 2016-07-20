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
				timeStr += (leftEventTime.Days % 20 == 1) ? " день." : ((leftEventTime.Days % 20 >= 5) ? " дней." : " дня.");
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
			if (self == null)
				return self;
			var stArray = self.Split(' ');
			var stResult = "";
			for (int i = 0; i < stArray.Length; i++) {
				var chArray = new List<char>();
				for (int j = 0; j < stArray[i].Length; j++) {
					if (j >= charsNumber && j % charsNumber == 0) {
						chArray.AddRange("<wbr>".ToCharArray());
					}
					chArray.Add(stArray[i][j]);
				}
				stArray[i] = string.Join("", chArray);
			}
			self = string.Join(" ", stArray);
			return self;
		}

		public static string ReplaceSharpWithRedmine(this string self)
		{
			var splited = self.Split(separator: "#".ToCharArray(), options: StringSplitOptions.RemoveEmptyEntries);
			var itemsToReplace = new List<string>();
			foreach (var item in splited) {
				string numberText = "";
				int number = 0;
				for (int i = 0; i < item.Length; i++) {
					if (int.TryParse(item[i].ToString(), out number)) {
						numberText += item[i];
					}
					else {
						break;
					}
				}
				if (!string.IsNullOrEmpty(numberText) && !itemsToReplace.Any(s => s == numberText)) {
					itemsToReplace.Add(numberText);
				}
			}
			foreach (var item in itemsToReplace) {
				self = self.Replace("#" + item,
					String.Format(" <a target='_blank' href='http://redmine.analit.net/issues/{0}'>#{0}</a>", item));
			}
			return self;
		}
	}
}