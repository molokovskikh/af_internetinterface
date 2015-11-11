using System;
using System.Collections;
using System.Collections.Generic;
using Castle.Core.Logging;
using InternetInterface.Models;
using NHibernate;

namespace InternetInterface.Helpers
{
	public interface IPortInfo
	{
		void GetPortInfo(ISession session, IDictionary propertyBag, ILogger logger, uint endPointId);
		string ViewName { get; }
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