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
		public static bool IsAccessibleClient(uint id, string password)
		{
			return Client.FindAll(DetachedCriteria.For(typeof(Client))
											.CreateAlias("PhysicalClient", "PC", JoinType.InnerJoin)
			                              	.Add(Restrictions.Eq("Id", id))
			                              	.Add(Restrictions.Eq("PC.Password", CryptoPass.GetHashString(password)))).Length != 0;
		}

		public static bool IsAccessiblePartner(object name)
		{
			return (name != null) && (Partner.FindAllByProperty("Login", name).Length != 0);
		}
	}
}