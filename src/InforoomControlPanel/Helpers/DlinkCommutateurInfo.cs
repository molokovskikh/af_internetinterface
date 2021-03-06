﻿using System;
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
	public class DlinkCommutateurInfo : CommutateurInfo, IPortInfo
	{
		private readonly string[] _switchModel = { "3028", "3526" };
		private string CurrentSwitchModel { get; set; }

		public void CleanErrors(ISession session, int endPointId)
		{
			var rowGeneralInformation = "";
			//коммутатор выводит на 1 таб больше в инф. о пакетах
			int attemptsToGetData = 5;
			var point = session.Load<ClientEndpoint>(endPointId);

			//Авторизация, как у linksys
			var login = ConfigurationManager.AppSettings["linksysLogin"];
			var password = ConfigurationManager.AppSettings["linksysPassword"];
			while (attemptsToGetData > 0) {
				try {
#if DEBUG
					//для модели Des-3028
					var telnet = new TelnetConnection("172.16.4.130", 23);
					//для модели Des-3526
					if (CurrentSwitchModel == _switchModel[1]) {
						telnet = new TelnetConnection("172.16.5.115", 23);
					}
#else
			var telnet = new TelnetConnection(point.Switch.Ip.ToString(), 23);
#endif
					try {
#if DEBUG
						telnet.Login(login, password, 100);
						var port = 3.ToString();
#else
				telnet.Login(login, password, 100);
				var port = point.Port.ToString();
#endif
						//общие сведения
						Thread.Sleep(4000);
						var command = string.Format("clear counters ports {0}", port);
						telnet.WriteLine(command);
						telnet.WriteLine("q");
						Thread.Sleep(2000);
						rowGeneralInformation = telnet.Read().ToLower();
					}
					finally {
						telnet.WriteLine("logout");
					}
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

		private void GetSwitchCurrentModel(string switchName)
		{
			foreach (string t in _switchModel) {
				if (switchName.IndexOf(t) != -1) {
					CurrentSwitchModel = t;
					break;
				}
			}
		}

		public void GetStateOfCable(ISession session, IDictionary propertyBag, int endPointId)
		{
			propertyBag["state"] = "Не опрошен";
			var rowGeneralInformation = "";
			//коммутатор выводит на 1 таб больше в инф. о пакетах
			PropertyBag = propertyBag;
			int attemptsToGetData = 5;
			var point = session.Load<ClientEndpoint>(endPointId);

			//Авторизация, как у linksys
			var login = ConfigurationManager.AppSettings["linksysLogin"];
			var password = ConfigurationManager.AppSettings["linksysPassword"];
			while (attemptsToGetData > 0) {
				try {
#if DEBUG
					//для модели Des-3028
					var telnet = new TelnetConnection("172.16.4.130", 23);
					//для модели Des-3526
					if (CurrentSwitchModel == _switchModel[1]) {
						telnet = new TelnetConnection("172.16.5.115", 23);
					}
#else
			var telnet = new TelnetConnection(point.Switch.Ip.ToString(), 23);
#endif
					try {
#if DEBUG
						telnet.Login(login, password, 100);
						var port = 3.ToString();
#else
				telnet.Login(login, password, 100);
				var port = point.Port.ToString();
#endif
						//общие сведения
						Thread.Sleep(2000);
						var command = string.Format("cable_diag ports {0}", port);
						telnet.WriteLine(command);
						telnet.WriteLine("q");
						Thread.Sleep(20000);
						rowGeneralInformation = telnet.Read().ToLower();
					}
					finally {
						telnet.WriteLine("logout");
					}
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

		private IDictionary PropertyBag { get; set; }

		/// <summary>
		/// Получение информации о коммутаторе
		/// </summary>
		/// <param name="session"></param>
		/// <param name="propertyBag"></param>
		/// <param name="logger"></param>
		/// <param name="endPointId"></param>
		public void GetPortInfo(ISession session, IDictionary propertyBag, int endPointId)
		{
			propertyBag["interfaceLines"] = null;
			propertyBag["interfaceCounters"] = null;
			propertyBag["countersLinesA"] = null;
			propertyBag["countersLinesB"] = null;


			//коммутатор выводит на 1 таб больше в инф. о пакетах
			PropertyBag = propertyBag;
			int attemptsToGetData = 5;
			var point = session.Load<ClientEndpoint>(endPointId);
			GetPortBaseInfo(session, propertyBag, point);

			//Авторизация, как у linksys
			var login = ConfigurationManager.AppSettings["linksysLogin"];
			var password = ConfigurationManager.AppSettings["linksysPassword"];
			while (attemptsToGetData > 0) {
				try {
					AskTheSwitch(point, login, password, propertyBag);
					break;
				}
				catch (Exception ex) {
					propertyBag["attempts"] = ((int)propertyBag["attempts"]) + 1;
					attemptsToGetData--;
					if (attemptsToGetData <= 0) {
						propertyBag["message"] = Message.Error("Ошибка подключения к коммутатору");
						//logger.Error(string.Format("Коммутатор {0} Порт {1}", point.Switch.IP, point.Port.ToString()), ex);
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

			GetSwitchCurrentModel(point.Switch.Name);
			while (attemptsToGetData > 0) {
				try {
#if DEBUG
					//для модели Des-3028
					var telnet = new TelnetConnection("172.16.4.130", 23);

					//для модели Des-3526
					if (CurrentSwitchModel == _switchModel[1]) {
						telnet = new TelnetConnection("172.16.5.115", 23);
					}
#else
			var telnet = new TelnetConnection(point.Switch.Ip.ToString(), 23);
#endif
					try {
#if DEBUG
						telnet.Login(login, password, 100);
						var port = 3.ToString();
#else
				telnet.Login(login, password, 100);
				var port = point.Port.ToString();
#endif
						//общие сведения
						Thread.Sleep(1000);
						var command = string.Format("show ports {0}", port);
						telnet.WriteLine(command);
						telnet.WriteLine("q");
						Thread.Sleep(500);
						var interfaces = telnet.Read();
						propertyBag["port"] = interfaces.ToLower().IndexOf("100m/full") != -1;
					}
					finally {
						telnet.WriteLine("logout");
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


		/// <summary>
		/// Опрос коммутатора
		/// </summary>
		/// <param name="login"></param>
		/// <param name="password"></param>
		/// <param name="propertyBag"></param>
		protected void AskTheSwitch(ClientEndpoint point, string login, string password, IDictionary propertyBag)
		{
			GetSwitchCurrentModel(point.Switch.Name);

#if DEBUG
			//для модели Des-3028
			var telnet = new TelnetConnection("172.16.4.130", 23);

			//для модели Des-3526
			if (CurrentSwitchModel == _switchModel[1]) {
				telnet = new TelnetConnection("172.16.5.115", 23);
			}
#else
			var telnet = new TelnetConnection(point.Switch.Ip.ToString(), 23);
#endif
			try {
#if DEBUG
				telnet.Login(login, password, 100);
				var port = 3.ToString();
#else
				telnet.Login(login, password, 100);
				var port = point.Port.ToString();
#endif
				//общие сведения
				Thread.Sleep(1000);
				var command = string.Format("show ports {0}", port);
				telnet.WriteLine(command);
				telnet.WriteLine("q");
				Thread.Sleep(500);
				var rowGeneralInformation = HardwareHelper.DelCommandAndHello(telnet.Read(), command);
				Thread.Sleep(500);

				//сведения о счетчиках
				command = string.Format("show packet ports {0}", port);
				telnet.WriteLine(command);
				telnet.WriteLine("q");
				Thread.Sleep(500);
				var rowPackageCounter = HardwareHelper.DelCommandAndHello(telnet.Read(), command);
				Thread.Sleep(500);

				//сведения об ошибках
				command = string.Format("show error ports {0}", port);
				telnet.WriteLine(command);
				telnet.WriteLine("q");
				Thread.Sleep(500);
				var rowErrorCounter = HardwareHelper.DelCommandAndHello(telnet.Read(), command);
				Thread.Sleep(500);

				command = string.Format("show fdb port {0}", port);
				telnet.WriteLine(command);
				Thread.Sleep(500);
				var macInfo = telnet.Read();
				macInfo = macInfo.Substring(macInfo.IndexOf("MAC Address"));

				GetSnoopingInfo(point, macInfo.Split('\n'), propertyBag);

				//обработка полученных сведений
				GetDataBySwitchModel(rowGeneralInformation, rowPackageCounter, rowErrorCounter, propertyBag);
			}
			finally {
				telnet.WriteLine("logout");
			}
		}

		////для модели Des-3028
		//	if (CurrentSwitchModel == _switchModel[0])
		//	//для модели Des-3526
		//	if (CurrentSwitchModel == _switchModel[1])
		private void GetDataBySwitchModel(string rowGeneralInformation,
			string rowPackageCounter, string rowErrorCounter, IDictionary propertyBag)
		{
			//общие сведения
			var generalInfo = GetDataByColumnPattern(rowGeneralInformation, ColumnPattern.GeneralInformation);
			propertyBag["interfaceLines"] = generalInfo;

			//сведения о счетчиках ***Состоит из двух таблиц
			GetDataByColumnPattern(rowPackageCounter, ColumnPattern.PackageCounter);

			//сведения об ошибках
			var interfaceCounters = GetDataByColumnPattern(rowErrorCounter, ColumnPattern.ErrorCounter);
			propertyBag["interfaceCounters"] = interfaceCounters;
		}


		/// <summary>
		/// Обработка общих сведений
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override List<Tuple<bool, string>> GetDataOfGeneralInformation(string data)
		{
			var portState = (CurrentSwitchModel == _switchModel[0]) ? " Port   State/" : "Port  State/";
			if (data.IndexOf(portState) != -1) {
				data = data.Substring(data.IndexOf(portState));
			}
			if (data.IndexOf(@"                                                              ") != -1) {
				data = data.Substring(0, data.IndexOf(@"                                                              "));
			}
			var dataList = new List<Tuple<bool, string>>();
			var stucturedData = GetStucturedData(data);
			var columnCount = LineData.GetColumnsCount();
			int startIndex = 0;
			var concatenator = new ColumnConcatenator();
			var rawChecked = true;
			for (int j = 0; j < stucturedData.Count; j++) {
				var textOfColumn = stucturedData[j].GetTextOfColumn(0);
				if (textOfColumn.IndexOf("----") != -1) {
					startIndex = ++j;
					break;
				}
				concatenator.AddColumns(stucturedData[j]);
			}
			for (int i = 0; i < columnCount; i++) {
				var dataListItem = new Tuple<bool, string>(true, concatenator.ConcatenateColumns(i));
				dataList.Add(dataListItem);
			}
			concatenator = new ColumnConcatenator();

			for (int j = startIndex; j < stucturedData.Count; j++) {
				concatenator.AddColumns(stucturedData[j]);
			}
			for (int i = 0; i < columnCount; i++) {
				var dataListItem = new Tuple<bool, string>(false, concatenator.ConcatenateColumns(i));
				dataList.Add(dataListItem);
			}

			return dataList;
		}

		/// <summary>
		/// Обработка сведений о счетчиках
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override List<Tuple<bool, string>> GetDataOfPackageCounter(string data)
		{
			var frameSize = (CurrentSwitchModel == _switchModel[0]) ? " Frame Size" : "Frame Size";
			var reservData = "";
			if (data.IndexOf(frameSize) != -1) {
				data = data.Substring(data.IndexOf(frameSize));
			}
			if (data.IndexOf("[") != -1) {
				data = data.Substring(0, data.IndexOf("["));
			}
			if (data.IndexOf(@"                                                              ") != -1) {
				data = data.Substring(0, data.IndexOf(@"                                                              "));
			}
			var dataList = new List<Tuple<bool, string>>();

			var stucturedData = CurrentSwitchModel == _switchModel[1]
				? GetStucturedData(data, "------------  ------------  ----------".Length)
				: GetStucturedData(data);

			var columnCount = LineData.GetColumnsCount();
			int startIndex = 0;
			var concatenator = new ColumnConcatenator();
			for (int j = 0; j < stucturedData.Count; j++) {
				var textOfColumn = stucturedData[j].GetTextOfColumn(0);
				if (textOfColumn.IndexOf("----") != -1) {
					startIndex = ++j;
					break;
				}
				concatenator.AddColumns(stucturedData[j]);
			}
			for (int i = 0; i < columnCount; i++) {
				var dataListItem = new Tuple<bool, string>(true, concatenator.ConcatenateColumns(i));
				dataList.Add(dataListItem);
			}
			for (int j = startIndex; j < stucturedData.Count; j++) {
				var textOfColumn = stucturedData[j].GetTextOfColumn(0);
				if (textOfColumn.IndexOf("Frame Type") != -1) {
					startIndex = j;
					break;
				}
				for (int i = 0; i < columnCount; i++) {
					var dataListItem = new Tuple<bool, string>(false, stucturedData[j].GetTextOfColumn(i));
					dataList.Add(dataListItem);
				}
			}
			//певая таблица
			PropertyBag["countersLinesA"] = dataList;


			var frameType = (CurrentSwitchModel == _switchModel[0]) ? " Frame Type" : "Frame Type";
			if (data.IndexOf(frameType) != -1) {
				data = data.Substring(data.IndexOf(frameType));
			}
			dataList = new List<Tuple<bool, string>>();
			concatenator = new ColumnConcatenator();
			stucturedData = CurrentSwitchModel == _switchModel[1]
				? GetStucturedData(data, "------------  ------------  ----------".Length, true)
				: GetStucturedData(data);
			columnCount = LineData.GetColumnsCount();
			startIndex = 0;

			for (int j = startIndex; j < stucturedData.Count; j++) {
				var textOfColumn = stucturedData[j].GetTextOfColumn(0);
				if (textOfColumn.IndexOf("----") != -1) {
					startIndex = ++j;
					break;
				}
				concatenator.AddColumns(stucturedData[j]);
			}
			for (int i = 0; i < columnCount; i++) {
				var dataListItem = new Tuple<bool, string>(true, concatenator.ConcatenateColumns(i));
				dataList.Add(dataListItem);
			}

			for (int j = startIndex; j < stucturedData.Count; j++) {
				for (int i = 0; i < columnCount; i++) {
					var dataListItem = new Tuple<bool, string>(false, stucturedData[j].GetTextOfColumn(i));
					dataList.Add(dataListItem);
				}
			}

			//вторая таблица
			PropertyBag["countersLinesB"] = dataList;

			return dataList;
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
				for (int i = 0; i < val.Length; i++) {
					if (val[i].Count(s => s == '-') == 5) {
						var ip = "не определен"; //не выводится
						var mac = val[i].Split(':').Implode("-").ToUpper();
						var lease = endpoint.LeaseList.FirstOrDefault(s => s.Mac == mac);
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
			}
			list.AddRange(endpoint.LeaseList.Where(s => !list.Any(d => d.Item1 == s.Mac && d.Item4 == s.Ip.ToString())).Select(
				s => new Tuple<string, string, string, string, string, string>("", "", s.Mac, s.Ip.ToString(), s.LeaseBegin.ToString(), s.LeaseEnd.ToString())));
			propertyBag["message"] = null;
			propertyBag["AddressList"] = list;
		}

		/// <summary>
		/// Обработка сведений об ошибках
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override List<Tuple<bool, string>> GetDataOfErrorCounter(string data)
		{
			if (data.IndexOf("RX Frames") != -1) {
				data = data.Substring(data.IndexOf("            RX Frames"));
				if (data.IndexOf("[") != -1) {
					data = data.Substring(0, data.IndexOf("["));
				}
			}
			var dataList = new List<Tuple<bool, string>>();
			var stucturedData = GetStucturedData(data);
			var columnCount = LineData.GetColumnsCount();
			int startIndex = 0;
			var concatenator = new ColumnConcatenator();
			for (int j = 0; j < stucturedData.Count; j++) {
				var textOfColumn = stucturedData[j].GetTextOfColumn(0);
				if (textOfColumn.IndexOf("----") != -1) {
					startIndex = ++j;
					break;
				}
				concatenator.AddColumns(stucturedData[j]);
			}
			for (int i = 0; i < columnCount; i++) {
				var dataListItem = new Tuple<bool, string>(true, concatenator.ConcatenateColumns(i));
				dataList.Add(dataListItem);
			}
			for (int j = startIndex; j < stucturedData.Count; j++) {
				var textOfColumn = stucturedData[j].GetTextOfColumn(0);
				for (int i = 0; i < columnCount; i++) {
					var dataListItem = new Tuple<bool, string>(false, stucturedData[j].GetTextOfColumn(i));
					dataList.Add(dataListItem);
				}
			}
			return dataList;
		}

		/// <summary>
		/// Получение структурированных данных из строки
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private List<LineData> GetStucturedData(string data, int cleanNumber = 0, bool revers = false)
		{
			var lineList = new List<LineData>();
			var lines = data.Split('\n');
			if (cleanNumber != 0) {
				if (revers)
					for (int i = 0; i < lines.Length; i++) {
						if (lines[i].Length > cleanNumber) {
							lines[i] = lines[i].Substring(cleanNumber, lines[i].Length - cleanNumber);
							while (lines[i].IndexOf(" ") == 0) {
								lines[i] = lines[i].Substring(1);
							}
							if (lines[i].IndexOf("----------  ----------  ---------") != -1) {
								lines[i] = "----------  ----------  ---------";
							}
						}
					}
				else
					for (int i = 0; i < lines.Length; i++) {
						if (lines[i].Length >= cleanNumber) {
							lines[i] = lines[i].Substring(0, cleanNumber);
						}
					}
			}
			if (!lines.Any(s => s.IndexOf("----") != -1)) {
				throw new Exception("Строка не содержит маркера нарезки '-' !");
			}
			var baseLine = lines.FirstOrDefault(s => s.IndexOf("----") != -1);

			LineData.SetColumnsLength(baseLine);
			for (int i = 0; i < lines.Length; i++) {
				if (lines[i].IndexOf("\r") == 0) {
					lines[i] = lines[i].Substring(1);
				}
				if (lines[i].Replace(" ", "").Replace("\r", "").Replace("\u001b", "").Length != 0) {
					var lineData = new LineData(lines[i]);
					lineList.Add(lineData);
				}
			}
			return lineList;
		}

		private void GetCabelStateInformation(string rowGeneralInformation, IDictionary propertyBag)
		{
			if (rowGeneralInformation.IndexOf("no cable") != -1) {
				propertyBag["state"] = "Не подключен";
			}
			if (rowGeneralInformation.IndexOf(" ok ") != -1) {
				propertyBag["state"] = "";
			}
			if (rowGeneralInformation.IndexOf("pair1 open") != -1) {
				var cut1 = rowGeneralInformation.Substring(rowGeneralInformation.IndexOf("pair1 open"));
				var at = cut1.IndexOf("at");
				var atL = "at".Length;
				var value1 = cut1.Substring(at + atL, cut1.IndexOf(" m ") - at - atL).Trim();

				cut1 = rowGeneralInformation.Substring(rowGeneralInformation.IndexOf("pair2 open"));
				at = cut1.IndexOf("at");
				var value2 = cut1.Substring(at + atL, cut1.IndexOf(" m") - at - atL).Trim();

				propertyBag["state"] = $"Разрыв на {value1}м. / {value2}м. ";
			}
			if (rowGeneralInformation.IndexOf("pair1 short") != -1) {
				var cut1 = rowGeneralInformation.Substring(rowGeneralInformation.IndexOf("pair1 short"));
				var at = cut1.IndexOf("at");
				var atL = "at".Length;
				var value1 = cut1.Substring(at + atL, cut1.IndexOf(" m ") - at - atL).Trim();

				cut1 = rowGeneralInformation.Substring(rowGeneralInformation.IndexOf("pair2 short"));
				at = cut1.IndexOf("at");
				var value2 = cut1.Substring(at + atL, cut1.IndexOf(" m") - at - atL).Trim();

				propertyBag["state"] = $"Замыкание на {value1}м. / {value2}м. ";
			}
		}


		/// <summary>
		/// Конкатенация строк сфорвированных "обработчиком строк"
		/// </summary>
		private class ColumnConcatenator
		{
			public ColumnConcatenator()
			{
				Lines = new List<LineData>();
			}

			private List<LineData> Lines { get; set; }

			public void AddColumns(LineData line)
			{
				Lines.Add(line);
			}

			public string ConcatenateColumns(int columnNumber)
			{
				string toReturn = "";
				foreach (var item in Lines) {
					toReturn += item.GetTextOfColumn(columnNumber);
				}
				return toReturn;
			}
		}

		/// <summary>
		/// Обработчик строк
		/// </summary>
		private class LineData
		{
			public LineData()
			{
				if (LineData.ColumnsLength == null || LineData.ColumnsLength.Length == 0) {
					throw new Exception("Постоянные длины столбцов не заданы!");
				}
				this.TextArrays = new string[ColumnsLength.Length];
			}

			public LineData(string columnText)
			{
				if (LineData.ColumnsLength == null || LineData.ColumnsLength.Length == 0) {
					throw new Exception("Постоянные длины столбцов не заданы!");
				}
				this.TextArrays = new string[ColumnsLength.Length];
				SetTextOfRaw(columnText);
			}

			private static int[] ColumnsLength { get; set; }
			private string[] TextArrays { get; set; }
			private int _iterator = 0;

			public void SetTextOfRaw(string columnText)
			{
				int currentIndex = 0;
				for (int i = 0; i < TextArrays.Length; i++) {
					if (columnText.Length >= currentIndex + ColumnsLength[i]) {
						try {
							TextArrays[i] = columnText.Substring(currentIndex, ColumnsLength[i]);
							currentIndex += ColumnsLength[i];
						}
						catch (Exception ex) {
							TextArrays[i] = "";
						}
					}
					else {
						if (columnText.LastIndexOf("\r") + 1 == columnText.Length) {
							TextArrays[i] = columnText.Substring(currentIndex, columnText.LastIndexOf("\r") - currentIndex);
							break;
						}
						TextArrays[i] = "";
					}
				}
			}

			public string GetTextOfColumn(int columnNumber)
			{
				if (TextArrays.Length < columnNumber) {
					throw new Exception(string.Format("Отсутствует столбезц под номером {0}!", columnNumber));
				}
				return TextArrays[columnNumber];
			}

			public static void SetColumnsLength(string baseLine)
			{
				var splitedBaseLine =
					baseLine.Replace("- ", "-\n ").Replace("\r", "").Split('\n').Where(s => s.IndexOf("---") != -1).ToArray();
				if (splitedBaseLine.Length == 0) {
					throw new Exception("Маркер нарезки '-' неверного формата!");
				}
				ColumnsLength = new int[splitedBaseLine.Length];
				for (int i = 0; i < splitedBaseLine.Length; i++) {
					ColumnsLength[i] = splitedBaseLine[i].Length;
				}
			}

			public static int GetColumnsCount()
			{
				return ColumnsLength.Length;
			}

			private void SetText(string text)
			{
				_iterator++;
				if (_iterator == TextArrays.Length)
					_iterator = 0;
				TextArrays[_iterator] = text;
			}
		}
	}
}