using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using Common.Tools;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Linq;

namespace InforoomControlPanel.Helpers
{
	public class LinksysCommutateurInfo : CommutateurInfo, IPortInfo
	{
		private readonly string[] _switchModel = { "SG300" };
		private string CurrentSwitchModel { get; set; }

		private void GetSwitchCurrentModel(string switchName)
		{
			CurrentSwitchModel = "";
			foreach (string t in _switchModel) {
				if (switchName.IndexOf(t) != -1) {
					CurrentSwitchModel = t;
					break;
				}
			}
		}

		public void GetStateOfCable(ISession session, IDictionary propertyBag, int endPointId)
		{
			propertyBag["state"] = "Не поддерживается";
			var point = session.Load<ClientEndpoint>(endPointId);
			int attemptsToGetData = 5;
			var login = ConfigurationManager.AppSettings["linksysLogin"];
			var password = ConfigurationManager.AppSettings["linksysPassword"];
			if (point.Switch.Name.IndexOf("SF300") == -1)
				return;

			while (attemptsToGetData > 0) {
				try {
#if DEBUG
					var telnet = new TelnetConnection("172.16.5.105", 23);
					telnet.Login(login, password, 100);
					var port = 3.ToString();
#else
				var telnet = new TelnetConnection(point.Switch.Ip.ToString(), 23);
				telnet.Login(login, password, 100);
				var port = (point.Port).ToString();
#endif

					Thread.Sleep(1000);
					telnet.WriteLine("N");
					Thread.Sleep(1000);
					var command = string.Format("test cable-diagnostics tdr interface FastEthernet {0}", port);
					telnet.WriteLine(command);
					Thread.Sleep(15000);
					var rowGeneralInformation = telnet.Read().ToLower();
					telnet.WriteLine("exit");
					GetCabelStateInformation(rowGeneralInformation, propertyBag);
					break;
				}
				catch (Exception ex) {
					attemptsToGetData--;
					if (attemptsToGetData <= 0) {
						break;
					}
					//ожидание перед повтором
					Thread.Sleep(1000);
				}
			}
		}

		public void GetPortInfo(ISession session, IDictionary propertyBag, int endPointId)
		{
			var point = session.Load<ClientEndpoint>(endPointId);
			GetPortBaseInfo(session, propertyBag, point);
			int attemptsToGetData = 5;


			var login = ConfigurationManager.AppSettings["linksysLogin"];
			var password = ConfigurationManager.AppSettings["linksysPassword"];
			GetSwitchCurrentModel(point.Switch.Name);
			while (attemptsToGetData > 0) {
				try {
#if DEBUG
					var telnet = new TelnetConnection(CurrentSwitchModel == _switchModel[0] ? "172.16.5.122" : "172.16.5.105", 23);
					telnet.Login(login, password, 100);
					var port = 3.ToString();
#else
				var telnet = new TelnetConnection(point.Switch.Ip.ToString(), 23);
				telnet.Login(login, password, 100);
				var port = (CurrentSwitchModel == _switchModel[0] ? point.Port-49 : point.Port).ToString();
#endif
					telnet.WriteLine("terminal length 0");

					var command = string.Format("show interfaces status {0} {1}",
						CurrentSwitchModel == _switchModel[0] ? "GigabitEthernet" : "FastEthernet", port);
					telnet.WriteLine(command);
					Thread.Sleep(500);
					var interfaces = HardwareHelper.DelCommandAndHello(telnet.Read(), command);
					GetInterfacesInfo(interfaces, propertyBag);


					command = string.Format("show interfaces counters {0} {1}",
						CurrentSwitchModel == _switchModel[0] ? "GigabitEthernet" : "FastEthernet", port);
					telnet.WriteLine(command);
					Thread.Sleep(1000);
					var counters = HardwareHelper.DelCommandAndHello(telnet.Read(), command);
					var countersForView = counters.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
					GetCountersInfo(counters, propertyBag);

					command = string.Format(" ");
					telnet.WriteLine(command);
					Thread.Sleep(500);
					var transmittedPauseFramesText = telnet.Read();
					var interfaceCountersItems = countersForView.GetRange(7, countersForView.Count - 7);
					const string transmittedText = "Transmitted Pause Frames: ";
					var transmittedIndex = transmittedPauseFramesText.IndexOf(transmittedText);
					var nextTransSpace = transmittedPauseFramesText.IndexOf("\r\n", transmittedIndex + transmittedText.Length);
					interfaceCountersItems.Add(transmittedPauseFramesText.Substring(transmittedIndex, nextTransSpace - transmittedIndex));
					propertyBag["interfaceCounters"] = interfaceCountersItems;

					command = string.Format("show ip dhcp snooping binding {0} {1}",
						CurrentSwitchModel == _switchModel[0] ? "GigabitEthernet" : "FastEthernet", port);
					telnet.WriteLine(command);
					Thread.Sleep(500);
					var macInfo = telnet.Read();
					macInfo = macInfo.Substring(macInfo.IndexOf("IP Address"));
          GetSnoopingInfo(point, macInfo.Split('\n'), propertyBag);

					telnet.WriteLine("exit");
					propertyBag["message"] = null;
					break;
				}
				catch (Exception ex) {
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
			var login = ConfigurationManager.AppSettings["linksysLogin"];
			var password = ConfigurationManager.AppSettings["linksysPassword"];

			while (attemptsToGetData > 0) {
				try {
#if DEBUG
					var telnet = new TelnetConnection(CurrentSwitchModel == _switchModel[0] ? "172.16.5.122" : "172.16.5.105", 23);
#else
				var telnet = new TelnetConnection(point.Switch.Ip.ToString(), 23);
#endif
					try {
#if DEBUG
						telnet.Login(login, password, 100);
						var port = 3.ToString();
#else
				telnet.Login(login, password, 100);
				var port = (CurrentSwitchModel == _switchModel[0] ? point.Port-49 : point.Port).ToString();
#endif
						telnet.WriteLine("terminal length 0");

						var command = string.Format("show interfaces status {0} {1}",
							CurrentSwitchModel == _switchModel[0] ? "GigabitEthernet" : "FastEthernet", port);
						telnet.WriteLine(command);
						Thread.Sleep(500);
						var interfaces = telnet.Read();
						propertyBag["port"] = interfaces.ToLower().IndexOf(" up ") != -1;
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

		protected void GetInterfacesInfo(string interfaces, IDictionary propertyBag)
		{
			interfaces = interfaces.Substring(interfaces.IndexOf("Flow Link"));
			var commandIndex = interfaces.LastIndexOf('>');
			if (commandIndex != -1) {
				interfaces = interfaces.Substring(commandIndex + 4, interfaces.Length - commandIndex - 4);
			}
			var interfaceForView =
				interfaces.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
					.Select(i => i.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries))
					.ToList();
			var firstLine = new List<string> { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
			firstLine.AddRange(interfaceForView[0].ToList());
			interfaceForView[0] = firstLine.ToArray();
			var result = new List<string[]>();
			var line = new List<string>();
			var updatedData = DeleteEmptyLines(interfaceForView).ToList();
			for (int i = 0; i < updatedData[0].Length; i++) {
				line.Add(updatedData[0][i] + " " + updatedData[1][i]);
			}
			result.Add(line.ToArray());
			result.Add(updatedData[2]);
			propertyBag["interfaceLines"] = result;
		}

		protected void GetCountersInfo(string counters, IDictionary propertyBag)
		{
			var countersForView = counters.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
			var countersToTable = countersForView.GetRange(1, 6);
			var countersToTableForView =
				countersToTable.Select(i => i.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)).ToList();
			var mashineLines = DeleteEmptyLines(countersToTableForView);
			propertyBag["countersLines"] = RenameTable(mashineLines);
		}

		protected void GetSnoopingInfo(ClientEndpoint endpoint, string[] macInfo, IDictionary propertyBag)
		{
			var list = new List<Tuple<string, string, string, string, string, string>>();
			if (macInfo.Length == 0) {
				propertyBag["message"] = Message.Error("Соединение на порту отсутствует");
				return;
			}
			foreach (var item in macInfo) {
				var val = item.Replace("\r", "").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if ((val.FirstOrDefault() ?? "").Count(s => s == ':') > 0) {
					var ip = val[1];
					var mac = val[0].Split(':').Implode("-").ToUpper();
					var lease = endpoint.LeaseList.FirstOrDefault(s => s.Mac == mac && s.Ip.ToString() == ip);
					if (lease != null) {
						list.Add(new Tuple<string, string, string, string, string, string>(
							mac, ip, lease.Mac, lease.Ip.ToString(), lease.LeaseBegin.ToString(), lease.LeaseEnd.ToString()));
					}
					else {
						list.Add(new Tuple<string, string, string, string, string, string>(
							mac, ip, "", "", "", ""));
					}
				}
			}
			list.AddRange(endpoint.LeaseList.Where(s => !list.Any(d => d.Item1 == s.Mac && d.Item2 == s.Ip.ToString())).Select(
				s => new Tuple<string, string, string, string, string, string>("", "", s.Mac, s.Ip.ToString(), s.LeaseBegin.ToString(), s.LeaseEnd.ToString())));
			propertyBag["message"] = null;
			propertyBag["AddressList"] = list;
		}

		private IEnumerable<string[]> DeleteEmptyLines(IEnumerable<string[]> data)
		{
			foreach (var stringse in data) {
				if (stringse.All(s => s.Contains("-")))
					continue;
				yield return stringse;
			}
		}

		private IEnumerable<string[]> RenameTable(IEnumerable<string[]> data)
		{
			return data.Select(stringse => stringse.Select(s => HardwareHelper.GetFieldName(s.Trim())).ToArray()).ToList();
		}

		private void GetCabelStateInformation(string rowGeneralInformation, IDictionary propertyBag)
		{
			if (rowGeneralInformation.IndexOf("not connected") != -1)
				propertyBag["state"] = "Не подключен";
			if (rowGeneralInformation.IndexOf("bad") != -1)
				propertyBag["state"] = "Поврежден";
			if (rowGeneralInformation.IndexOf("good") != -1)
				propertyBag["state"] = "";
			if (rowGeneralInformation.IndexOf("mismatch") != -1) {
				var cut1 = rowGeneralInformation.Substring(rowGeneralInformation.IndexOf("mismatch"));
				var at = cut1.IndexOf("at");
				var atL = "at".Length;
				var value1 = cut1.Substring(at + atL, cut1.IndexOf(" m") - at - atL).Trim();
				propertyBag["state"] = $"Поврежден на {value1}м. ";
			}
			if (rowGeneralInformation.IndexOf("circuit") != -1) {
				var cut1 = rowGeneralInformation.Substring(rowGeneralInformation.IndexOf("circuit"));
				var at = cut1.IndexOf("at");
				var atL = "at".Length;
				var value1 = cut1.Substring(at + atL, cut1.IndexOf(" m") - at - atL).Trim();
				propertyBag["state"] = $"Замыкание на {value1}м. ";
			}
			if (rowGeneralInformation.IndexOf("is open") != -1) {
				var cut1 = rowGeneralInformation.Substring(rowGeneralInformation.IndexOf("is open"));
				var at = cut1.IndexOf("at");
				var atL = "at".Length;
				var value1 = cut1.Substring(at + atL, cut1.IndexOf(" m") - at - atL).Trim();
				propertyBag["state"] = $"Разрыв на {value1}м. ";
			}
		}
	}
}