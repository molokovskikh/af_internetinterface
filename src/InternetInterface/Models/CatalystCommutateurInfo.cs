using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using Castle.Core.Logging;
using Common.Tools;
using InternetInterface.Helpers;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	public class CatalystCommutateurInfo : IPortInfo
	{
		public void GetPortInfo(ISession session, IDictionary propertyBag, ILogger logger, uint endPointId)
		{
			var point = session.Load<ClientEndpoint>(endPointId);
			propertyBag["point"] = point;
			propertyBag["lease"] = session.Query<Lease>().FirstOrDefault(l => l.Endpoint == point);
			var login = ConfigurationManager.AppSettings["catalystLogin"];
			var password = ConfigurationManager.AppSettings["catalystPassword"];

			try {
#if DEBUG
				var telnet = new TelnetConnection("172.16.1.112", 23);
				telnet.Login(login, password, 100);
				var port = 2.ToString();
#else
				var telnet = new TelnetConnection(point.Switch.IP.ToString(), 23);
				telnet.Login(login, password, 100);
				var port = point.Port.ToString();
#endif
				//Грязный хак, чтобы поиск осуществлять по нужному порту
				if (port.Length == 1)
					port += ' ';

				telnet.WriteLine("terminal length 0");
				var command = string.Format("show interfaces fastEthernet 0/{0}", port);
				telnet.WriteLine(command);

				var info = telnet.Read();
				var beginNameIndex = info.IndexOf("\r\n");
				var endNameIndex = info.IndexOf("#");
				var terminalName = info.Substring(beginNameIndex, endNameIndex - beginNameIndex);
				info = info.Remove(0, endNameIndex + command.Length + 3).Replace(terminalName + "#", string.Empty);

				propertyBag["Info"] = info.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Select(i =>
					string.Format("<span style=\"margin-left:{0}px;\">", i.StartsWith("     ") ? 20 : 10) + i +
						"</span>" + "<br/>")
					.Implode(string.Empty);

				command = string.Format("sh int status | inc Fa0/{0}", port);
				telnet.WriteLine(command);
				var portIsActive = HardwareHelper.ResultInArray(telnet.Read(), command).Contains("connected");
				//Проверяем, что порт активен
				if (portIsActive) {
					command = string.Format("show mac address-table interface fastEthernet 0/{0} | inc a0/{0}", port);
					telnet.WriteLine(command);
					var macInfo = HardwareHelper.ResultInArray(telnet.Read(), command);
					if (macInfo.Length > 0) {
						var macAddr = string.Empty;
						var chetFlag = 0;
						foreach (var _char in macInfo[1].Replace(".", string.Empty)) {
							if (chetFlag < 2) {
								macAddr += _char;
								chetFlag++;
							}
							else {
								chetFlag = 1;
								macAddr += ":" + _char;
							}
						}
						macAddr = macAddr.ToUpper();

						propertyBag["MACResult"] = macAddr.Replace(":", "-");

						command = string.Format("show ip dh sn bi | inc {0}", macAddr);
						telnet.WriteLine(command);
						var ipInfo = HardwareHelper.ResultInArray(telnet.Read(), command);

						propertyBag["IPResult"] = ipInfo[1];
					}
					command = string.Format("show interfaces fastEthernet 0/{0} counters", port);
					telnet.WriteLine(command);
					var counterInfo = telnet.Read();

					//TODO заменить кодировку counterInfo - выводится ерунда
					//Encoding iso = Encoding.GetEncoding("ISO-8859-1");
					//Encoding utf8 = Encoding.UTF8;
					//byte[] utfBytes = utf8.GetBytes(Message);
					//byte[] isoBytes = Encoding.Convert(utf8, iso, utfBytes);
					//string msg = iso.GetString(isoBytes);

					propertyBag["counterInfo"] =
						HardwareHelper.DelCommandAndHello(counterInfo, command).TrimStart('\r', '\n').TrimEnd('\r', '\n').Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
							.Select(i => i.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)).Select(stringse => stringse.Select(s => HardwareHelper.GetFieldName(s.Trim())).ToArray()).ToList();


					command = string.Format("show interfaces fastEthernet 0/{0} counters errors", port);
					telnet.WriteLine(command);
					var errorCounterInfo = telnet.Read();

					propertyBag["errorCounterInfo"] =
						HardwareHelper.DelCommandAndHello(errorCounterInfo, command).TrimStart('\r', '\n').TrimEnd('\r', '\n').Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
							.Select(i => i.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)).ToList();
				}
				else {
					propertyBag["Message"] = Message.Error("Соединение на порту отсутствует");
				}
				telnet.WriteLine("exit");
			}
			catch (Exception e) {
				propertyBag["Message"] = Message.Error("Ошибка подключения к коммутатору");
				logger.Error(string.Format("Коммутатор {0} Порт {1}", point.Switch.IP, point.Port.ToString()), e);
			}
		}

		public string ViewName
		{
			get { return "PortInfoCatalyst"; }
		}
	}
}