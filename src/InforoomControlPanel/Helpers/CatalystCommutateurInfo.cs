using System;
using System.Collections;
using System.Configuration;
using System.Linq;
using System.Threading;
using Common.Tools;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Linq;

namespace InforoomControlPanel.Helpers
{
	public class CatalystCommutateurInfo : CommutateurInfo, IPortInfo
	{
		public void GetPortInfo(ISession session, IDictionary propertyBag, int endPointId)
		{
			var point = session.Load<ClientEndpoint>(endPointId);
			GetPortBaseInfo(session, propertyBag, point);
			propertyBag["counterInfo"] = null;
			propertyBag["Info"] = null;
			propertyBag["errorCounterInfo"] = null;
			int attemptsToGetData = 5;

			var login = ConfigurationManager.AppSettings["catalystLogin"];
			var password = ConfigurationManager.AppSettings["catalystPassword"];
			while (attemptsToGetData > 0) {
				try {
#if DEBUG
					var telnet = new TelnetConnection("172.16.4.246", 23);
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

						propertyBag["counterInfo"] =
							HardwareHelper.DelCommandAndHello(counterInfo, command).TrimStart('\r', '\n').TrimEnd('\r', '\n').Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
								.Select(i => i.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)).Select(stringse => stringse.Select(s => HardwareHelper.GetFieldName(s.Trim())).ToArray()).ToList();


						command = string.Format("show interfaces fastEthernet 0/{0} counters errors", port);
						telnet.WriteLine(command);
						var errorCounterInfo = telnet.Read();

						propertyBag["errorCounterInfo"] =
							HardwareHelper.DelCommandAndHello(errorCounterInfo, command).TrimStart('\r', '\n').TrimEnd('\r', '\n').Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
								.Select(i => i.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)).ToList();
						propertyBag["message"] = null;
					}
					else {
						propertyBag["message"] = Message.Error("Соединение на порту отсутствует");
					}
					telnet.WriteLine("exit");
					break;
				}
				catch (Exception e) {
					propertyBag["attempts"] = ((int)propertyBag["attempts"]) + 1;
					attemptsToGetData--;
					if (attemptsToGetData <= 0) {
						propertyBag["message"] = Message.Error("Ошибка подключения к коммутатору");
						break;
					}
					//ожидание перед повтором
					Thread.Sleep(1000);
				}
			}
		}

		/// <summary>
		/// Опрос коммутатора
		/// </summary>
		/// <param name="login"></param>
		/// <param name="password"></param>
		/// <param name="propertyBag"></param>
		public void GetStateOfPort(ISession session, IDictionary propertyBag, int endPointId)
		{
			//кол-во попыток при неудачном опросе
			int attemptsToGetData = 5;
			var point = session.Load<ClientEndpoint>(endPointId);
			GetPortBaseInfoShort(session, propertyBag, point);
			//Авторизация, как у linksys
			var login = ConfigurationManager.AppSettings["catalystLogin"];
			var password = ConfigurationManager.AppSettings["catalystPassword"];

			while (attemptsToGetData > 0) {
				try {
#if DEBUG
					var telnet = new TelnetConnection("172.16.4.246", 23);
#else
			var telnet = new TelnetConnection(point.Switch.IP.ToString(), 23);
#endif
					try {
#if DEBUG
						telnet.Login(login, password, 100);
						var port = 2.ToString();
#else
				telnet.Login(login, password, 100);
				var port = point.Port.ToString();
#endif
						//получение сведения от коммутатора
						Thread.Sleep(1000);
						telnet.WriteLine("terminal length 0");
						var command = string.Format("show interfaces fastEthernet 0/{0}", port);
						telnet.WriteLine(command);
						Thread.Sleep(1000);
						//обработка полученных сведений 
						var interfaces = telnet.Read();
						propertyBag["port"] = interfaces.ToLower().IndexOf("is up") != -1;
					}
					finally {
						telnet.WriteLine("exit");
					}
					break;
				}
				catch (Exception ex) {
					propertyBag["attempts"] = ((int)propertyBag["attempts"]) + 1;
					attemptsToGetData--;
					if (attemptsToGetData <= 0) {
						break;
					}
					//ожидание перед повтором
					Thread.Sleep(1000);
				}
			}
		}
	}
}