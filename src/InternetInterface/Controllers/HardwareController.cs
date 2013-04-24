using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using log4net;

namespace InternetInterface.Controllers
{
	[Helper(typeof(PaginatorHelper))]
	[Helper(typeof(IpHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class HardwareController : ARSmartDispatcherController
	{
		private string DelCommandAndHello(string info, string command)
		{
			info = info.Replace(command, string.Empty);
			var indexHello = info.LastIndexOf("\r\n");
			return info.Substring(0, indexHello);
		}

		private string[] ResultInArray(string info, string command)
		{
			info = DelCommandAndHello(info, command);
			return info.Replace("\r\n", string.Empty).Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
		}

		public void PortInfo(uint endPoint)
		{
			var point = ClientEndpoint.Find(endPoint);
			PropertyBag["point"] = point;
			PropertyBag["lease"] = Lease.Queryable.Where(l => l.Endpoint == point).FirstOrDefault();

			try {
#if DEBUG
				var telnet = new TelnetConnection("91.209.124.59", 23);
				//var telnet = new TelnetConnection("172.16.1.114", 23);
				telnet.Login("ii", "ii", 100);
				var port = 3.ToString();
#else
				var telnet = new TelnetConnection(point.Switch.IP.ToString(), 23);
				telnet.Login("ii", "analit", 100);
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

				PropertyBag["Info"] = info.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Select(i =>
					string.Format("<span style=\"margin-left:{0}px;\">", i.StartsWith("     ") ? 20 : 10) + i +
						"</span>" + "<br/>")
					.Implode(string.Empty);

				command = string.Format("sh int status | inc Fa0/{0}", port);
				telnet.WriteLine(command);
				var portIsActive = ResultInArray(telnet.Read(), command).Contains("connected");
				//Проверяем, что порт активен
				if (portIsActive) {
					command = string.Format("show mac address-table interface fastEthernet 0/{0} | inc a0/{0}", port);
					telnet.WriteLine(command);
					var macInfo = ResultInArray(telnet.Read(), command);
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

						PropertyBag["MACResult"] = macAddr.Replace(":", "-");

						command = string.Format("show ip dh sn bi | inc {0}", macAddr);
						telnet.WriteLine(command);
						var ipInfo = ResultInArray(telnet.Read(), command);

						PropertyBag["IPResult"] = ipInfo[1];
					}
					command = string.Format("show interfaces fastEthernet 0/{0} counters", port);
					telnet.WriteLine(command);
					var counterInfo = telnet.Read();

					PropertyBag["counterInfo"] =
						DelCommandAndHello(counterInfo, command).TrimStart('\r', '\n').TrimEnd('\r', '\n').Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
							.Select(i => i.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)).ToList();


					command = string.Format("show interfaces fastEthernet 0/{0} counters errors", port);
					telnet.WriteLine(command);
					var errorCounterInfo = telnet.Read();

					PropertyBag["errorCounterInfo"] =
						DelCommandAndHello(errorCounterInfo, command).TrimStart('\r', '\n').TrimEnd('\r', '\n').Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
							.Select(i => i.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)).ToList();
				}
				else {
					PropertyBag["Message"] = Message.Error("Соединение на порту отсутствует");
				}
				telnet.WriteLine("exit");
			}
			catch (Exception e) {
				PropertyBag["Message"] = Message.Error("Ошибка подключения к коммутатору");
				Logger.Error(string.Format("Коммутатор {0} Порт {1}", point.Switch.IP, point.Port.ToString()), e);
			}
		}
	}
}