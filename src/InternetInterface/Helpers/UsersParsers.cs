
namespace InternetInterface.Helpers
{
	public class UsersParsers
	{
		public static string MobileTelephoneParcer(string number)
		{
			if (number.Length == 15) {
				number = number.Replace("-", string.Empty);
				number = number.Substring(1, number.Length - 1);
				return number.Substring(0, 3) + "-" + number.Substring(3, 7);
			}
			return number;
		}

		public static string HomeTelephoneParser(string number)
		{
			if (number.Length == 5)
			{
				return "473-54" + number;
			}
			return number;
		}
	}
}