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
				long intIp;
				if (((splited.Length == 1) && long.TryParse(splited.First(), out intIp)))
				{
					var normalip = BitConverter.GetBytes(intIp);
					return string.Format("{0}.{1}.{2}.{3}", normalip[3], normalip[2], normalip[1], normalip[0]);
				}
				else
				{
					if (valid.IsMatch(IP))
						return IP;
				}
			}
			return string.Empty;
		}
	}
}