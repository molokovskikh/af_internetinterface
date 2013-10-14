using System.Linq;
using InternetInterface;
using InternetInterface.Models;

namespace InforoomInternet.Helpers
{
	public class LoginHelper
	{
		public static Client IsAccessibleClient(uint id, string password)
		{
			return Client.Queryable.FirstOrDefault(c =>
				c.Id == id &&
					c.PhysicalClient != null &&
					c.PhysicalClient.Password == CryptoPass.GetHashString(password));
		}

		public static bool IsAccessiblePartner(object name)
		{
			return (name != null) && (Partner.FindAllByProperty("Login", name).Length != 0);
		}
	}
}