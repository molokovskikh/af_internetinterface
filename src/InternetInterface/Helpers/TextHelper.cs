using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Castle.MonoRail.Framework.Helpers;

namespace InternetInterface.Helpers
{
	public class TextHelper : AbstractHelper
	{
		public static string SelectQuery(string query, string value)
		{
			if (!string.IsNullOrEmpty(query) && !string.IsNullOrEmpty(value)) {
				query = query.ToLower().Replace('ё', 'е');
				var bufValue = value.ToLower().Replace('ё', 'е');
				var indexQuery = bufValue.IndexOf(query);
				if (indexQuery != -1) {
					value = value.Insert(indexQuery, "<b class=\"selectorQuery\">");
					bufValue = value.ToLower().Replace('ё', 'е');
					indexQuery = bufValue.IndexOf(query);
					var ret = value.Insert(indexQuery + query.Length, "</b>");
					return ret;
				}
			}
			return value;
		}

		public static string SelectContact(string query, string value)
		{
			if (!string.IsNullOrEmpty(query) && value.Contains(query)) {
				var delimeterIndex = 3;
				var text = SelectQuery(query, value);
				var kolvoChisel = 0;
				if (text != value) {
					for (int i = 0; i < text.Length; i++) {
						var outI = 0;
						if (int.TryParse(text[i].ToString(), out outI)) {
							delimeterIndex = i;
							kolvoChisel++;
						}
						if (kolvoChisel >= 3)
							break;
					}
				}
				if (new Regex(@"^((\d{10}))").IsMatch(value))
					text = text.Insert(delimeterIndex + 1, "-");
				return text;
			}
			return value.Insert(3, "-");
		}
	}
}