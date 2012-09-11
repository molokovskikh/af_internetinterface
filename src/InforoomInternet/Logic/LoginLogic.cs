using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using InforoomInternet.Models;
using InternetInterface;
using InternetInterface.Models;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace InforoomInternet.Logic
{
	public class LoginLogic
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