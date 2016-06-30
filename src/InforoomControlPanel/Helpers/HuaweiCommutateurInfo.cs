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
using NHibernate.Proxy;

namespace InforoomControlPanel.Helpers
{
	public class HuaweiCommutateurInfo : CommutateurInfo, IPortInfo
	{
		public void GetStateOfCable(ISession session, IDictionary propertyBag, int endPointId)
		{
			propertyBag["state"] = "Не поддерживается";
		}

		/// <summary>
		/// Получение информации о коммутаторе
		/// </summary>
		/// <param name="session"></param>
		/// <param name="propertyBag"></param>
		/// <param name="logger"></param>
		/// <param name="endPointId"></param>
		public void GetPortInfo(ISession session, IDictionary propertyBag, int endPointId)
		{
			//кол-во попыток при неудачном опросе
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
#if DEBUG
			var telnet = new TelnetConnection("172.16.4.74", 23);
#else
			var telnet = new TelnetConnection(point.Switch.Ip.ToString(), 23);
#endif
			try {
#if DEBUG
				telnet.Login(login, password, 100);
				var port = 9.ToString();
#else
				telnet.Login(login, password, 100);
				var port = point.Port.ToString();
#endif
				//получение сведения от коммутатора
				Thread.Sleep(1000);
				var command = string.Format("display interface Ethernet 0/0/{0}", port);
				telnet.WriteLine(command);
				Thread.Sleep(1000);
				//обработка полученных сведений 
				var interfaces = HardwareHelper.DelCommandAndHello(telnet.Read(), command);
				telnet.WriteLine("q");
				GetInfo(interfaces, propertyBag);

				command = string.Format("display dhcp snooping user-bind interface Ethernet 0/0/{0}", port);
				telnet.WriteLine(command);
				Thread.Sleep(1000);
				string macInfoString = telnet.Read();

				var macInfo = macInfoString.Substring(macInfoString.IndexOf("IP Address"), macInfoString.IndexOf("print count:") - macInfoString.IndexOf("IP Address"));

				GetSnoopingInfo(point, macInfo.Split('\n'), propertyBag);
			}
			finally {
				telnet.WriteLine("quite");
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
					var telnet = new TelnetConnection("172.16.4.74", 23);
#else
			var telnet = new TelnetConnection(point.Switch.Ip.ToString(), 23);
#endif
					try {
#if DEBUG
						telnet.Login(login, password, 100);
						var port = 9.ToString();
#else
				telnet.Login(login, password, 100);
				var port = point.Port.ToString();
#endif
						//получение сведения от коммутатора
						Thread.Sleep(1000);
						var command = string.Format("display interface Ethernet 0/0/{0}", port);
						telnet.WriteLine(command);
						Thread.Sleep(1000);
						//обработка полученных сведений 
						var interfaces = telnet.Read();
						propertyBag["port"] = interfaces.ToLower().IndexOf("current state : up") != -1;
					}
					finally {
						telnet.WriteLine("quite");
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

		IEnumerable<string> CutMac(string str, int chunkSize)
		{
			for (int i = 0; i < str.Length; i += chunkSize) {
				yield return str.Substring(i, chunkSize);
			}
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
				if ((val.FirstOrDefault() ?? "").Count(s => s == '.') == 3) {
					var ip = val[0];

					var mac = CutMac(val[1].Replace("-", ""), 2).Implode("-").ToUpper();
					var lease = endpoint.LeaseList.FirstOrDefault(s => s.Mac == mac && s.Ip.ToString() == ip);
					if (lease != null) {
						list.Add(new Tuple<string, string, string, string, string, string>(
							mac,
							ip, lease.Mac, lease.Ip.ToString(), lease.LeaseBegin.ToString(), lease.LeaseEnd.ToString()));
					}
					else {
						list.Add(new Tuple<string, string, string, string, string, string>(
							mac,
							ip, "", "", "", ""));
					}
				}
			}
			list.AddRange(endpoint.LeaseList.Where(s => !list.Any(d => d.Item1 == s.Mac && d.Item2 == s.Ip.ToString())).Select(
				s => new Tuple<string, string, string, string, string, string>("", "", s.Mac, s.Ip.ToString(), s.LeaseBegin.ToString(), s.LeaseEnd.ToString())));
			propertyBag["message"] = null;
			propertyBag["AddressList"] = list;
		}

		protected void GetInfo(string interfaces, IDictionary propertyBag)
		{
			//общие сведения
			var generalInfo = GetDataByColumnPattern(interfaces, ColumnPattern.GeneralInformation);
			propertyBag["interfaceLines"] = generalInfo;
			//сведения о счетчиках
			var countersLines = GetDataByColumnPattern(interfaces, ColumnPattern.PackageCounter);
			propertyBag["countersLines"] = countersLines;
			//сведения об ошибках
			var interfaceCounters = GetDataByColumnPattern(interfaces, ColumnPattern.ErrorCounter);
			propertyBag["interfaceCounters"] = interfaceCounters;
		}

		/// <summary>
		/// Обработка общих сведений
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override List<Tuple<bool, string>> GetDataOfGeneralInformation(string data)
		{
			var dataList = new List<Tuple<bool, string>>();
			var splitedStrings =
				data.Replace("  ", " ")
					.Replace("rate", "rate : ")
					.Replace("\r", " ")
					.Replace(" is ", ":")
					.Replace("0/0/", " ")
					.Split(',')
					.SelectMany(s => s.Split('\n'))
					.ToArray();
			for (int i = 0; i < splitedStrings.Length; i++) {
				//получаем строки до задиси с подстракой
				if (splitedStrings[i].IndexOf("Mdi") != -1)
					break;
				//режем строку на заголовок и значение 
				var splitedItem = splitedStrings[i].Split(':');
				if (splitedItem.Length > 1) {
					//сохраняем заголовок
					dataList.Add(new Tuple<bool, string>(true, splitedItem[0]));
					dataList.Add(new Tuple<bool, string>(false, splitedItem[1]));
				}
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
			var dataList = new List<Tuple<bool, string>>();
			var splitedStrings =
				data.Replace("  ", " ")
					.Replace("rate", "rate : ")
					.Replace("\r", " ")
					.Replace(" is ", ":")
					.Replace("0/0/", " ")
					.Split(',').Where(s =>
						s.IndexOf("Jumbo") == -1
						&& s.IndexOf("CRC") == -1
						&& s.IndexOf("Giants") == -1
						&& s.IndexOf("Jabbers") == -1
						&& s.IndexOf("Fragments") == -1
						&& s.IndexOf("Runts") == -1
						&& s.IndexOf("DropEvents") == -1
						&& s.IndexOf("Alignments") == -1
						&& s.IndexOf("Symbols") == -1
						&& s.IndexOf("Ignoreds") == -1
						&& s.IndexOf("Frames") == -1
						&& s.IndexOf("Discard") == -1
						&& s.IndexOf("Total Error") == -1
					)
					.SelectMany(s => s.Split('\n'))
					.ToArray();
			bool startAdding = false;
			bool gotRecordTime = true;
			bool gotUnicast = true;
			bool gotMulticast = true;
			for (int i = 0; i < splitedStrings.Length; i++) {
				if (startAdding) {
					if (splitedStrings[i].ToLower().IndexOf("input") != -1) {
						//режем строку на заголовок и значение 
						var splitedItem = splitedStrings[i].Split(':');
						if (splitedItem.Length > 1) {
							//сохраняем заголовок
							dataList.Add(new Tuple<bool, string>(true, splitedItem[0]));
							dataList.Add(new Tuple<bool, string>(true, splitedItem[1]));
						}
					}
					else {
						if (splitedStrings[i].IndexOf("Record time") != -1 && gotRecordTime
						    || splitedStrings[i].IndexOf("Unicast") != -1 && gotUnicast
						    || splitedStrings[i].IndexOf("Multicast") != -1 && gotMulticast) {
							//режем строку на заголовок и значение 
							var splitedItem = splitedStrings[i].Split(':');
							if (splitedItem.Length > 1) {
								//сохраняем заголовок
								dataList.Add(new Tuple<bool, string>(true, splitedItem[0]));
								dataList.Add(new Tuple<bool, string>(true, splitedItem[1]));

								if (splitedStrings[i].IndexOf("Record time") != -1)
									gotRecordTime = false;
								if (splitedStrings[i].IndexOf("Unicast") != -1)
									gotUnicast = false;
								if (splitedStrings[i].IndexOf("Multicast") != -1)
									gotMulticast = false;
							}
						}
					}
				}
				//получаем строки до задиси с подстракой
				if (splitedStrings[i].IndexOf("Mdi") != -1)
					startAdding = true;
			}
			for (int i = 0; i < splitedStrings.Length; i++) {
				if (startAdding) {
					if (splitedStrings[i].ToLower().IndexOf("output") != -1) {
						//режем строку на заголовок и значение 
						var splitedItem = splitedStrings[i].Split(':');
						if (splitedItem.Length > 1) {
							//сохраняем заголовок
							dataList.Add(new Tuple<bool, string>(false, splitedItem[0]));
							dataList.Add(new Tuple<bool, string>(false, splitedItem[1]));
						}
					}
					else {
						if (splitedStrings[i].IndexOf("Record time") != -1 && gotRecordTime
						    || splitedStrings[i].IndexOf("Unicast") != -1 && gotUnicast
						    || splitedStrings[i].IndexOf("Multicast") != -1 && gotMulticast
						    || splitedStrings[i].IndexOf("Broadcast") != -1) {
							//режем строку на заголовок и значение 
							var splitedItem = splitedStrings[i].Split(':');
							if (splitedItem.Length > 1) {
								//сохраняем заголовок
								dataList.Add(new Tuple<bool, string>(false, splitedItem[0]));
								dataList.Add(new Tuple<bool, string>(false, splitedItem[1]));
							}
						}
						if (splitedStrings[i].IndexOf("Record time") != -1)
							gotRecordTime = true;
						if (splitedStrings[i].IndexOf("Unicast") != -1)
							gotUnicast = true;
						if (splitedStrings[i].IndexOf("Multicast") != -1)
							gotMulticast = true;
					}
				}
				//получаем строки до задиси с подстракой
				if (splitedStrings[i].IndexOf("Mdi") != -1)
					startAdding = true;
			}
			return dataList;
		}

		/// <summary>
		/// Обработка сведений об ошибках
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override List<Tuple<bool, string>> GetDataOfErrorCounter(string data)
		{
			var dataList = new List<Tuple<bool, string>>();
			var splitedStrings =
				data.Replace("  ", " ")
					.Replace("rate", "rate : ")
					.Replace("\r", " ")
					.Replace(" is ", ":")
					.Replace("0/0/", " ")
					.Split(',')
					.SelectMany(s => s.Split('\n'))
					.ToArray();
			for (int i = 0; i < splitedStrings.Length; i++) {
				//получаем строки до задиси с подстракой
				if (splitedStrings[i].IndexOf("Jumbo") != -1
				    || splitedStrings[i].IndexOf("CRC") != -1
				    || splitedStrings[i].IndexOf("Giants") != -1
				    || splitedStrings[i].IndexOf("Jabbers") != -1
				    || splitedStrings[i].IndexOf("Fragments") != -1
				    || splitedStrings[i].IndexOf("Runts") != -1
				    || splitedStrings[i].IndexOf("DropEvents") != -1
				    || splitedStrings[i].IndexOf("Alignments") != -1
				    || splitedStrings[i].IndexOf("Symbols") != -1
				    || splitedStrings[i].IndexOf("Ignoreds") != -1
				    || splitedStrings[i].Replace(" ", "") == "Frames"
				    || splitedStrings[i].IndexOf("Discard") != -1
				    || splitedStrings[i].IndexOf("Total Error") != -1) {
					//режем строку на заголовок и значение 
					var splitedItem = splitedStrings[i].Split(':');
					if (splitedItem.Length > 1) {
						//сохраняем заголовок
						dataList.Add(new Tuple<bool, string>(true, splitedItem[0]));
						dataList.Add(new Tuple<bool, string>(false, splitedItem[1]));
					}
				}
			}
			return dataList;
		}
	}
}