using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Intefaces;
using NHibernate.SqlCommand;

namespace Inforoom2.Helpers
{
	public static class AddressHelper
	{
		public static string AsString(this IList<string> list, string delimiter, int startInc, int stopExc)
		{
			if ((startInc >= list.Count) || (startInc >= stopExc))
				return "";

			StringBuilder sb = new StringBuilder();
			sb.Append(list[startInc]);

			for (int i = startInc + 1; i < stopExc; i++) {
				sb.Append(delimiter);
				sb.Append(list[i]);
			}

			return sb.ToString();
		}

		public static bool IsNumeric(this string value)
		{
			Int32 t;
			return Int32.TryParse(value, out t);
		}

		public static void SplitHouseAndHousing(string rawNumber, ref string number, ref string housing)
		{
			if(string.IsNullOrEmpty(rawNumber))
				return;
			
			foreach (var c in rawNumber) {
				if (c == ' ') {
					continue;
				}
				//если добрались до подъезда, выходим
				if (c == 'п') {
					break;
				}
				if (Char.IsDigit(c) || c == '/') {
					number = number + c;
				}
				else {
					housing = housing + c;
				}
			}
		}
	
	}
}