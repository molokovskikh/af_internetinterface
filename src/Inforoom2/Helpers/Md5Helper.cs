using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Inforoom2.Helpers
{
	public static class Md5
	{
		public static string GetHash(string text)
		{
			string hash = string.Empty;
			if (text != null) {
				byte[] bytes = Encoding.Unicode.GetBytes(text);
				var CSP = new MD5CryptoServiceProvider();
				byte[] byteHash = CSP.ComputeHash(bytes);
				foreach (byte b in byteHash)
					hash += string.Format("{0:x2}", b);
			}
			return hash;
		}
	}
}