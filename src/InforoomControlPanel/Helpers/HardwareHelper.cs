using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Linq;

namespace InforoomControlPanel.Helpers
{
	public interface IPortInfo
	{
		void GetPortInfo(ISession session, IDictionary propertyBag, int endPointId);
		void GetStateOfPort(ISession session, IDictionary propertyBag, int endPointId);
	} 

	public class HardwareHelper
	{
		public static IPortInfo GetPortInformator(ClientEndpoint point)
		{
			switch (point.Switch.Type) {
				case SwitchType.Catalyst:
					return new CatalystCommutateurInfo();
				case SwitchType.Linksys:
					return new LinksysCommutateurInfo();
				case SwitchType.Dlink:
					return new DlinkCommutateurInfo();
				case SwitchType.Huawei:
					return new HuaweiCommutateurInfo();
				default:
					return null;
			}
		}

		public static string DelCommandAndHello(string info, string command)
		{
			info = info.Replace(command, string.Empty);
			var indexHello = info.LastIndexOf("\r\n");
			return info.Substring(0, indexHello);
		}

		public static string[] ResultInArray(string info, string command)
		{
			info = DelCommandAndHello(info, command);
			return info.Replace("\r\n", string.Empty).Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
		}

		public static string GetFieldName(string mashineName)
		{
			switch (mashineName) {
				case "Port":
					return "Порт";
				case "InUcastPkts":
					return "Получено пакетов";
				case "InMcastPkts":
					return "Получено мультикаст пакетов";
				case "InBcastPkts":
					return "Получено широковещательных пакетов";
				case "InOctets":
					return "Принято байт";
				case "OutUcastPkts":
					return "Отправлено пакетов";
				case "OutMcastPkts":
					return "Отправлено мультикаст пакетов";
				case "OutBcastPkts":
					return "Отправлено широковещательных пакетов";
				case "OutOctets":
					return "Отправлено байт";
				default:
					return mashineName;
			}
		}
	}

	public class CommutateurInfo
	{
		public void GetPortBaseInfo(ISession session, IDictionary propertyBag, ClientEndpoint point)
		{
			propertyBag["MACResult"] = null;
			propertyBag["IPResult"] = null;
			propertyBag["message"] = null;
			propertyBag["attempts"] = 1;

			propertyBag["point"] = new
			{
				pointId = point.Id,
				clientId = point.Client.Id,
				clientType = point.Client.IsPhysicalClient.ToString(),
				clientName = point.Client.GetName(),
				clientSwitchType = point.Switch.Type,
				clientSwitchName = point.Switch.Name,
				clientSwitchIp = point.Switch.Ip.ToString(),
				clientSwitchId = point.Switch.Id,
				clientPort = point.Port
			};
			var lease = session.Query<Lease>().FirstOrDefault(l => l.Endpoint == point);
			propertyBag["lease"] = lease != null ?
				new { leaseBegin = lease.LeaseBegin.ToString(), leaseEnd = lease.LeaseEnd.ToString(), leaseIp = lease.Ip.ToString(), leaseMac = lease.Mac }
				: null;
		}

		public void GetPortBaseInfoShort(ISession session, IDictionary propertyBag, ClientEndpoint point)
		{
			propertyBag["message"] = null;
			propertyBag["attempts"] = 1;
			propertyBag["port"] = false;
			var leases = session.Query<Lease>().Where(l => l.Endpoint == point).ToList();
			propertyBag["lease"] = leases.Count > 0 && leases.Any(s=> s.LeaseEnd >= SystemTime.Now() &&  !s.Pool.IsGray )  ? "true" : null; 
		}

		protected List<Tuple<bool, string>> GetDataByColumnPattern(string data, ColumnPattern patternName)
		{
			var dataByPattern = new List<Tuple<bool, string>>();

			switch (patternName) {
				case ColumnPattern.GeneralInformation:
					dataByPattern = GetDataOfGeneralInformation(data);
					break;
				case ColumnPattern.PackageCounter:
					dataByPattern = GetDataOfPackageCounter(data);
					break;
				case ColumnPattern.ErrorCounter:
					dataByPattern = GetDataOfErrorCounter(data);
					break;
				default:
					break;
			}

			return dataByPattern;
		}

		protected virtual List<Tuple<bool, string>> GetDataOfGeneralInformation(string data)
		{
			throw new Exception("Метод не переопределен!");
		}

		protected virtual List<Tuple<bool, string>> GetDataOfPackageCounter(string data)
		{
			throw new Exception("Метод не переопределен!");
		}

		protected virtual List<Tuple<bool, string>> GetDataOfErrorCounter(string data)
		{
			throw new Exception("Метод не переопределен!");
		}

		protected enum ColumnPattern
		{
			GeneralInformation,
			PackageCounter,
			ErrorCounter
		}
	}
}