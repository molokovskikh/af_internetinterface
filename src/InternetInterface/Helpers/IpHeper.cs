using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Castle.MonoRail.Framework.Helpers;
using InternetInterface.Models;

namespace InternetInterface.Helpers
{
	public class IpHeper: AbstractHelper
	{
		public static string GetNormalIp(string IP)
		{
			if (!string.IsNullOrEmpty(IP))
			{
				var splited = IP.Split('.');
				var valid = new Regex(NetworkSwitches.IPRegExp);
				if ((valid.IsMatch(IP)) || (splited.Length == 1))
				{
					var normalip = BitConverter.GetBytes(Convert.ToInt64(IP));
					return string.Format("{0}.{1}.{2}.{3}", normalip[3], normalip[2], normalip[1],
										 normalip[0]);
				}
				else
				{
					return IP;
				}
			}
			return string.Empty;
		}
	}
}