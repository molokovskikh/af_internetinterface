using System;
using InternetInterface.Interfaces;
using InternetInterface.Models;

namespace InternetInterface.Helpers
{
	public class HardwareHelper
	{
		public static IPortInfo GetPortInformator(ClientEndpoint point)
		{
			switch (point.Switch.Type) {
				case SwitchType.Catalyst:
					return new CatalystCommutateurInfo();
				case SwitchType.Linksys:
					return new LinksysCommutateurInfo();
				default: return null;
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
			return info.Replace("\r\n", string.Empty).Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
		}

		public static string GetFieldName(string mashineName)
		{
			switch (mashineName) {
				case "Port":
					return "����";
				case "InUcastPkts":
					return "�������� �������";
				case "InMcastPkts":
					return "�������� ���������� �������";
				case "InBcastPkts":
					return "�������� ����������������� �������";
				case "InOctets":
					return "������� ����";
				case "OutUcastPkts":
					return "���������� �������";
				case "OutMcastPkts":
					return "���������� ���������� �������";
				case "OutBcastPkts":
					return "���������� ����������������� �������";
				case "OutOctets":
					return "���������� ����";
				default:
					return mashineName;
			}
		}
	}
}