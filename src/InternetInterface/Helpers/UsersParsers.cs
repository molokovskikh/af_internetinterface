
namespace InternetInterface.Helpers
{
	public class UsersParsers
	{
		public static string MobileTelephoneParcer(string number)
		{
			if (number.Length == 10)
			{
				return "8-" + number.Substring(0, 3) + "-" + number.Substring(3, 3) + "-" + number.Substring(6, 2) + "-" +
					   number.Substring(8, 2);
			}
			return number;
		}

		public static string HomeTelephoneParser(string number)
		{
			if (number.Length == 5)
			{
				return "47354-" + number;
			}
			return number;
		}
	}
}