using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Web;
using Castle.Core.Logging;
using Common.Tools;
using InternetInterface.Helpers;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	public class LinksysCommutateurInfo : IPortInfo
	{
		private readonly string[] _switchModel = {"SG300"};
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

		public void GetPortInfo(ISession session, IDictionary propertyBag, ILogger logger, uint endPointId)
		{
			var point = session.Load<ClientEndpoint>(endPointId);
			propertyBag["point"] = point;
			propertyBag["lease"] = session.Query<Lease>().FirstOrDefault(l => l.Endpoint == point);
			var login = ConfigurationManager.AppSettings["linksysLogin"];
			var password = ConfigurationManager.AppSettings["linksysPassword"];
			GetSwitchCurrentModel(point.Switch.Name);
			try {
#if DEBUG
				var telnet = new TelnetConnection(CurrentSwitchModel == _switchModel[0] ? "172.16.5.122" : "172.16.5.105", 23);
				telnet.Login(login, password, 100);
				var port = 3.ToString();
#else
				var telnet = new TelnetConnection(point.Switch.IP.ToString(), 23);
				telnet.Login(login, password, 100);
				var port = point.Port.ToString();
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
				var countersForView = counters.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).ToList();
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
				var macInfo = HardwareHelper.ResultInArray(telnet.Read(), command);
				GetSnoopingInfo(macInfo, propertyBag);

				telnet.WriteLine("exit");
			}
			catch (Exception ex) {
				propertyBag["Message"] = Message.Error("Ошибка подключения к коммутатору");
				logger.Error(string.Format("Коммутатор {0} Порт {1}", point.Switch.IP, point.Port.ToString()), ex);
			}
		}

		protected void GetInterfacesInfo(string interfaces, IDictionary propertyBag)
		{
			var commandIndex = interfaces.LastIndexOf('>');
			interfaces = interfaces.Substring(commandIndex + 4, interfaces.Length - commandIndex - 4);
			var interfaceForView =
				interfaces.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
					.Select(i => i.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries))
					.ToList();
			var firstLine = new List<string> {string.Empty, string.Empty, string.Empty, string.Empty, string.Empty};
			firstLine.AddRange(interfaceForView[0].ToList());
			interfaceForView[0] = firstLine.ToArray();
			var result = new List<string[]>();
			var line = new List<string>();
			var updatedData = DeleteEmptyLines(interfaceForView).ToList();
			for (int i = 0; i < updatedData[0].Length; i++) line.Add(updatedData[0][i] + " " + updatedData[1][i]);
			result.Add(line.ToArray());
			result.Add(updatedData[2]);
			propertyBag["interfaceLines"] = result;
		}

		protected void GetCountersInfo(string counters, IDictionary propertyBag)
		{
			var countersForView = counters.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).ToList();
			var countersToTable = countersForView.GetRange(1, 6);
			var countersToTableForView =
				countersToTable.Select(i => i.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries)).ToList();
			var mashineLines = DeleteEmptyLines(countersToTableForView);
			propertyBag["countersLines"] = RenameTable(mashineLines);
		}

		protected void GetSnoopingInfo(string[] macInfo, IDictionary propertyBag)
		{
			if (macInfo.Length > 20) {
				propertyBag["IPResult"] = macInfo[21];
				propertyBag["MACResult"] = macInfo[20].Split(':').Select(i => i.ToUpper()).Implode("-");
			}
			else {
				propertyBag["Message"] = Message.Error("Соединение на порту отсутствует");
			}
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

		public string ViewName
		{
			get { return "PortInfoLinksys"; }
		}
	}
}