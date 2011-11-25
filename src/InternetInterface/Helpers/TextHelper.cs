using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetInterface.Helpers
{
	public class TextHelper
	{
		public static string SelectQuery(string query, string value)
		{
			if (!string.IsNullOrEmpty(query)) {
				query = query.ToLower();
				var bufValue = value.ToLower();
				var indexQuery = bufValue.IndexOf(query);
				if (indexQuery != -1) {
					value = value.Insert(indexQuery, "<b class=\"selectorQuery\">");
					bufValue = value.ToLower();
					indexQuery = bufValue.IndexOf(query);
					return value.Insert(indexQuery + query.Length, "</b>");
				}
			}
			return value;
		}
	}
}