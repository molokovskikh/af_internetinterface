using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace InforoomInternet.Models
{
	public class CryptoPass
	{
		public static string GetHashString(string s)
		{
			string hash = string.Empty;
			if (s != null)
			{
				byte[] bytes = Encoding.Unicode.GetBytes(s);
				var CSP = new MD5CryptoServiceProvider();
				byte[] byteHash = CSP.ComputeHash(bytes);
				foreach (byte b in byteHash)
					hash += string.Format("{0:x2}", b);
			}
			return hash;
		}
	}
}