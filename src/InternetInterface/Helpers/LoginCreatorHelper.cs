using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using InternetInterface.Models;

namespace InternetInterface.Helpers
{
	public class LoginCreatorHelper
	{
		private static Dictionary<string, string> words;

		private static void Init()
		{
			words = new Dictionary<string, string> {
				{ "а", "a" },
				{ "б", "b" },
				{ "в", "v" },
				{ "г", "g" },
				{ "д", "d" },
				{ "е", "e" },
				{ "ё", "yo" },
				{ "ж", "zh" },
				{ "з", "z" },
				{ "и", "i" },
				{ "й", "j" },
				{ "к", "k" },
				{ "л", "l" },
				{ "м", "m" },
				{ "н", "n" },
				{ "о", "o" },
				{ "п", "p" },
				{ "р", "r" },
				{ "с", "s" },
				{ "т", "t" },
				{ "у", "u" },
				{ "ф", "f" },
				{ "х", "h" },
				{ "ц", "c" },
				{ "ч", "ch" },
				{ "ш", "sh" },
				{ "щ", "sch" },
				{ "ъ", "j" },
				{ "ы", "i" },
				{ "ь", "j" },
				{ "э", "e" },
				{ "ю", "yu" },
				{ "я", "ya" }
			};
		}

		public static string GetUniqueEnLogin(string RULogin)
		{
			if (RULogin != null) {
				Init();
				RULogin = RULogin.ToLower();
				foreach (var word in words) {
					RULogin = RULogin.Replace(word.Key, word.Value);
				}
				var unique = false;
				var rnd = new Random();
				var indexing = 2;
				while (!unique) {
					RULogin += rnd.Next(indexing);
					if (PhysicalClient.FindAllByProperty("Login", RULogin).Length == 0) {
						unique = true;
					}
					indexing++;
				}
				return RULogin;
			}
			return string.Empty;
		}
	}
}