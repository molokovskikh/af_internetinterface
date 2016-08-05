using System.Linq;
using Common.Tools;
using InternetInterface;
using InternetInterface.Helpers;
using InternetInterface.Models;
using System.Net;
using System.Configuration;
using NHibernate;
using NHibernate.Linq;
using Castle.ActiveRecord;

namespace InforoomInternet.Helpers
{
	public class IpAdressHelper
	{
		private static IPAddress GetHost(string userHostAddress)
		{
			var hostAdress = userHostAddress;
#if DEBUG
			if (ConfigurationManager.AppSettings["DebugHost"] != null)
				hostAdress = ConfigurationManager.AppSettings["DebugHost"];
#endif
			return IPAddress.Parse(hostAdress);
		}

		private static Lease FindLease(string userHostAddress, ISession dbSession)
		{
			return dbSession.Query<Lease>().FirstOrDefault(l => l.Ip == GetHost(userHostAddress));
		}

		public static ClientEndpoint GetClientEndpoint(string userHostAddress, ISession dbSession)
		{
			var lease = FindLease(userHostAddress, dbSession);
			ClientEndpoint endpoint;
			if (lease != null)
				endpoint = lease.Endpoint;
			else {
				var ipAddress = GetHost(userHostAddress);
				var ips = dbSession.Query<StaticIp>().ToList();
				endpoint = ips.Where(ip => {
					if (ip.Ip == ipAddress.ToString())
						return true;
					if (ip.Mask != null) {
						var subnet = SubnetMask.CreateByNetBitLength(ip.Mask.Value);
						if (ipAddress.IsInSameSubnet(IPAddress.Parse(ip.Ip), subnet))
							return true;
					}
					return false;
				}).Select(s => s.EndPoint).FirstOrDefault(s => !s.Disabled);
			}
			return endpoint;
		}
	}
}