using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using InforoomInternet.Models;
using NHibernate.Criterion;

namespace InforoomInternet.Logic
{
	public class LoginLogic
	{
		public static bool IsAccessibleClient(uint id, string password)
		{
			return PhysicalClient.FindAll(DetachedCriteria.For(typeof (PhysicalClient))
			                              	.Add(Restrictions.Eq("Id", id))
			                              	.Add(Restrictions.Eq("Password", CryptoPass.GetHashString(password)))).Length != 0;
		}
	}
}