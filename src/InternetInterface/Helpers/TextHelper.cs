﻿using System;
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

		public static string SelectContact(string query, string value)
		{
			if (value.Contains(query)) {
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