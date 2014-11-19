using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml.Linq;

namespace Inforoom2.Helpers
{
	public class IpGeoBase
	{
		private const string QueryFormat = "<ipquery><fields><all/></fields><ip-list>{0}</ip-list></ipquery>";
		private const string IpItemFormat = "<ip>{0}</ip>";

		private string serviceAddr = "http://ipgeobase.ru:8090/geo/geo.html";

		public string ServiceAddr
		{
			set { serviceAddr = value; }
		}

		private static string GetVisitorIpAddress(bool getLan = false)
		{
			string visitorIpAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

			if (String.IsNullOrEmpty(visitorIpAddress))
				visitorIpAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];

			if (string.IsNullOrEmpty(visitorIpAddress))
				visitorIpAddress = HttpContext.Current.Request.UserHostAddress;

			if (string.IsNullOrEmpty(visitorIpAddress) || visitorIpAddress.Trim() == "::1") {
				getLan = true;
				visitorIpAddress = string.Empty;
			}

			if (getLan && string.IsNullOrEmpty(visitorIpAddress)) {
				//This is for Local(LAN) Connected ID Address
				string stringHostName = Dns.GetHostName();
				//Get Ip Host Entry
				IPHostEntry ipHostEntries = Dns.GetHostEntry(stringHostName);
				//Get Ip Address From The Ip Host Entry Address List
				IPAddress[] arrIpAddress = ipHostEntries.AddressList;

				try {
					visitorIpAddress = arrIpAddress[arrIpAddress.Length - 2].ToString();
				}
				catch {
					try {
						visitorIpAddress = arrIpAddress[0].ToString();
					}
					catch {
						try {
							arrIpAddress = Dns.GetHostAddresses(stringHostName);
							visitorIpAddress = arrIpAddress[0].ToString();
						}
						catch {
							visitorIpAddress = "127.0.0.1";
						}
					}
				}
			}


			return visitorIpAddress;
		}

		public IpAnswer GetInfo()
		{
			var ip = GetVisitorIpAddress();
			List<IpAnswer> answer = GetServiceAnswer(new List<string>() { ip });
			if (answer.Count > 0) {
				if (string.IsNullOrEmpty(answer[0].City)) {
					throw new WebException("");
				}
				else {
					return answer[0];
				}
			}
			return null;
		}

		public List<IpAnswer> GetInfo(List<string> ips)
		{
			return GetServiceAnswer(ips);
		}

		private List<IpAnswer> GetServiceAnswer(List<string> ips)
		{
			List<IpAnswer> answer = new List<IpAnswer>();
			string answerXml = GetQueryResponse(ips);
			if (answerXml != "")
				ParseQueryResponse(answerXml, answer);
			return answer;
		}

		private string InitQueryData(List<string> ips)
		{
			StringBuilder ipList = new StringBuilder();
			foreach (string ip in ips)
				ipList.AppendFormat(IpItemFormat, ip);
			return string.Format(QueryFormat, ipList.ToString());
		}

		private string GetQueryResponse(List<string> ips)
		{
			string response = "";
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(serviceAddr);
			httpWebRequest.Timeout = 1000;
			httpWebRequest.Method = "POST";
			string queryData = InitQueryData(ips);
			byte[] queryDataBytes = Encoding.UTF8.GetBytes(queryData);
			httpWebRequest.ContentLength = queryDataBytes.Length;
			httpWebRequest.GetRequestStream().Write(queryDataBytes, 0, queryDataBytes.Length);
			try {
				HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				if (httpWebResponse != null) {
					if (httpWebResponse.StatusCode == HttpStatusCode.OK) {
						using (
							StreamReader sr = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding("windows-1251"))) {
							response = sr.ReadToEnd();
						}
					}
				}
			}
			catch (Exception) {
			}
			return response;
		}

		private void ParseQueryResponse(string xml, List<IpAnswer> answer)
		{
			TextReader reader = new StringReader(xml);
			XElement xmlelement = XElement.Load(reader);
			string separator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
			foreach (XElement element in xmlelement.Elements("ip")) {
				IpAnswer ipItem = new IpAnswer();
				ipItem.Ip = element.Attribute("value").Value;
				foreach (XElement item in element.Descendants()) {
					switch (item.Name.LocalName) {
						case "inetnum":
							ipItem.InetNum = item.Value;
							break;
						case "inet-descr":
							ipItem.InetDesc = item.Value;
							break;
						case "inet-status":
							ipItem.InetStatus = item.Value;
							break;
						case "city":
							ipItem.City = item.Value;
							break;
						case "region":
							ipItem.Region = item.Value;
							break;
						case "district":
							ipItem.District = item.Value;
							break;
						case "lat":
							ipItem.Lat = double.Parse(item.Value.Replace(".", separator));
							break;
						case "lng":
							ipItem.Lng = double.Parse(item.Value.Replace(".", separator));
							break;
					}
				}
				answer.Add(ipItem);
			}
		}
	}

	public class IpAnswer
	{
		public string Ip { get; set; }

		public string InetNum { get; set; }

		public string InetDesc { get; set; }

		public string InetStatus { get; set; }

		public string City { get; set; }

		public string Region { get; set; }

		public string District { get; set; }

		public double Lat { get; set; }

		public double Lng { get; set; }
	}
}