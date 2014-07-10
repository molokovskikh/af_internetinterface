using System;
using System.Security.Cryptography;
using System.Text;

namespace InternetInterface.Helpers
{
	public class CryptoPass
	{
		public static string GetHashString(string s)
		{
			string hash = string.Empty;
			if (s != null) {
				byte[] bytes = Encoding.Unicode.GetBytes(s);
				var CSP = new MD5CryptoServiceProvider();
				byte[] byteHash = CSP.ComputeHash(bytes);
				foreach (byte b in byteHash)
					hash += string.Format("{0:x2}", b);
			}
			return hash;
		}

		public static string GeneratePassword()
		{
			var availableChars = "23456789qwertyupasdfghjkzxcvbnmQWERTYUPASDFGHJKLZXCVBNM";
			var password = String.Empty;
			while (password.Length < 8)
				password += availableChars[RollDice(availableChars.Length - 1)];
			return password;
		}

		public static int RollDice(int numberSides)
		{
			var rngCsp = new RNGCryptoServiceProvider();
			var randomNumber = new byte[1];
			do {
				rngCsp.GetBytes(randomNumber);
			} while (!IsFairRoll(randomNumber[0], numberSides));
			return ((randomNumber[0] % numberSides) + 1);
		}

		private static bool IsFairRoll(byte roll, int numSides)
		{
			var fullSetsOfValues = Byte.MaxValue / numSides;
			return roll < numSides * fullSetsOfValues;
		}
	}
}