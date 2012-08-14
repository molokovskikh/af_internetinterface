using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using InternetInterface.Models;
using log4net;

namespace InternetInterface.Helpers
{
	public enum SmsRequestType
	{
		ValidOperation = 1
	}

	public class SmsHelper
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (SmsHelper));
		private const string Login = "inforoom";
		private const string Password = "analitFarmacia";
		private const string Source = "inforoom";
		public static List<string> Types = new List<string> {
			"delivered",
			"notDelivered",
			"waiting",
			"enqueued",
			"cancel",
			"onModer"
		};

		private static bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		private static string GenerateSMSID()
		{
			return Convert.ToInt64((DateTime.Now - DateTime.MinValue).TotalSeconds).ToString();
		}

		protected virtual XDocument MakeRequest(XDocument document, string url)
		{
			XDocument resultDoc = new XDocument();
			ServicePointManager.ServerCertificateValidationCallback = AcceptAllCertifications;

			var request = (HttpWebRequest)WebRequest.Create(url);
			document.Declaration = new XDeclaration("1.0", "utf-8", "true");
			var data = Encoding.UTF8.GetBytes(document.ToString());

			request.ContentType = "text/xml";
			request.ContentLength = data.Length;
			request.Method = "POST";
#if !DEBUG
			var outStream = request.GetRequestStream();

			outStream.Write(data, 0, data.Length);
			outStream.Close();

			var responseStream = request.GetResponse().GetResponseStream();

			var reader = new StreamReader(responseStream);
			resultDoc = XDocument.Parse(reader.ReadToEnd());
			responseStream.Close();
#endif
			return resultDoc;
		}

		public List<XDocument> SendServiceMessageNow(List<string> numbers)
		{
			var messages = numbers.Select(number => new SmsMessage {
				CreateDate = DateTime.Now, PhoneNumber = number
			}).ToList();
			return SendMessages(messages);
		}

		public XDocument SendMessage(string number, string text, DateTime? shouldBeSend = null)
		{
			return SendMessage(new SmsMessage {
				CreateDate = DateTime.Now,
				PhoneNumber = number,
				ShouldBeSend = shouldBeSend,
				Text = text
			});
		}

		public XDocument SendMessage(SmsMessage message)
		{
			return SendMessages(new List<SmsMessage> {message}).FirstOrDefault();
		}

		public static void DeleteNoSendingMessages(Client client)
		{
			SmsMessage.Queryable.Where(m => m.Client == client && !m.IsSended).ToList().ForEach(m => m.Delete());
		}

		public string GetStatus(SmsMessage message)
		{
			var statuses = GetStatus(message.SMSID);
			if (statuses.Keys.Contains("delivered") && statuses["delivered"].Contains(message.PhoneNumber.Replace("+", string.Empty))) {
				return "Доставлено";
			}
			if (statuses.Keys.Contains("notDelivered") && statuses["notDelivered"].Contains(message.PhoneNumber.Replace("+", string.Empty))) {
				return "Не доставлено";
			}
			if (statuses.Keys.Contains("waiting") && statuses["waiting"].Contains(message.PhoneNumber.Replace("+", string.Empty))) {
				return "В ожидании";
			}
			if (statuses.Keys.Contains("enqueued") && statuses["enqueued"].Contains(message.PhoneNumber.Replace("+", string.Empty))) {
				return "Отчет о доставке еще не сформирован";
			}
			if (statuses.Keys.Contains("cancel") && statuses["cancel"].Contains(message.PhoneNumber.Replace("+", string.Empty))) {
				return "Отмена";
			}
			if (statuses.Keys.Contains("onModer") && statuses["onModer"].Contains(message.PhoneNumber.Replace("+", string.Empty))) {
				return "Сообщение находятся на модерации";
			}
			return "Неизвестен";
		}

		public Dictionary<string, List<string>> GetStatus(string smsId)
		{
			var result = new Dictionary<string, List<string>>();

			var document = new XDocument(
				new XElement("data",
					new XElement("login", Login),
					new XElement("password", Password),
					new XElement("smsid", smsId)
					)
				);

			var url = @"https://lcab.sms-uslugi.ru/API/XML/report.php";

			var data = MakeRequest(document, url);

			var resultDocData = data.Element("data");
			if (resultDocData != null) {
				var serverRequest = Int32.Parse(resultDocData.Element("code").Value);
				if (serverRequest == (int)SmsRequestType.ValidOperation) {
					var detail = resultDocData.Element("detail");
					foreach (var type in Types) {
						var delivered = detail.Element(type);
						if (delivered != null && delivered.HasElements) {
							result.Add(type, new List<string>());
							var delResult = result[type];
							foreach (var number in delivered.Elements("number")) {
								delResult.Add(number.Value);
							}
						}
					}
				}
			}
			return result;
		}

		public List<XDocument> SendMessages(IList<SmsMessage> smses)
		{
			var result = new List<XDocument>();

			var groupedSmses = smses.Where(s => !s.IsSended).GroupBy(s => s.ShouldBeSend);

			foreach (var groupedSms in groupedSmses) {
				var document = new XDocument(
					new XElement("data",
					             new XElement("login", Login),
					             new XElement("password", Password),
					             new XElement("action", "send"),
					             new XElement("source", Source),
								 new XElement("text", "Уважаемый абонент, компания Инфорум предлагает передовые услуги в сфере телекомуникаций (подроблее http://ivrn.net)")
						)
					);

				var dataElement = document.Element("data");
				if (dataElement != null) {
					if (groupedSms.Key != null)
						dataElement.Add(new XElement("datetime", groupedSms.Key.Value.ToString("yyyy-MM-dd HH:mm:ss")));
					var smsId = GenerateSMSID();
					dataElement.Add(new XElement("SMSID", smsId));

					foreach (var smsMessage in groupedSms) {
						if (!string.IsNullOrEmpty(smsMessage.PhoneNumber)) {
							dataElement.Add(new XElement("to", new XAttribute("number", smsMessage.PhoneNumber), smsMessage.Text));
							smsMessage.SendToOperatorDate = DateTime.Now;
							smsMessage.SMSID = smsId;
							smsMessage.Save();
						}
						else {
							_log.Error(
								string.Format(
									"Не было отправлено сообщение для клиента {0} Из-за того, что небыл найден номер для отправки. Текст: {1}",
									smsMessage.Client != null ? smsMessage.Client.Id.ToString(CultureInfo.InvariantCulture) : "<Не удалось определить клиента>", smsMessage.Text));
						}
					}
				}

				var resultDoc = MakeRequest(document, @"https://transport.sms-pager.com:7214/send.xml");

				var resultDocData = resultDoc.Element("data");
				if (resultDocData != null) {
					var serverRequest = Int32.Parse(resultDocData.Element("code").Value);
					var smsId = resultDocData.Element("smsid").Value;
					_log.Debug(resultDocData);
					foreach (var smsMessage in groupedSms.Where(s => !string.IsNullOrEmpty(s.PhoneNumber))) {
						smsMessage.IsSended = serverRequest == (int) SmsRequestType.ValidOperation;
						smsMessage.ServerRequest = serverRequest;
						smsMessage.SMSID = smsId;
						smsMessage.Save();
					}
				}
				result.Add(resultDoc);
			}

			return result;
		}
	}
}