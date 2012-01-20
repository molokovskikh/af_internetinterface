using System;
using System.Collections.Generic;
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

		private static bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		private static string GenerateSMSID()
		{
			return Convert.ToInt64((DateTime.Now - DateTime.MinValue).TotalSeconds).ToString();
		}

		public static List<XDocument> SendServiceMessageNow(List<string> numbers)
		{
			var messages = numbers.Select(number => new SmsMessage {
				CreateDate = DateTime.Now, PhoneNumber = number
			}).ToList();
			return SendMessages(messages);
		}

		public static XDocument SendMessage(string number, string text, DateTime? shouldBeSend = null)
		{
			return SendMessage(new SmsMessage {
				CreateDate = DateTime.Now,
				PhoneNumber = number,
				ShouldBeSend = shouldBeSend,
				Text = text
			});
		}

		public static XDocument SendMessage(SmsMessage message)
		{
			return SendMessages(new List<SmsMessage> {message}).FirstOrDefault();
		}

		public static List<XDocument> SendMessages(IList<SmsMessage> smses)
		{
			var result = new List<XDocument>();

			ServicePointManager.ServerCertificateValidationCallback = AcceptAllCertifications;

			var groupedSmses = smses.Where(s => !s.IsSended).GroupBy(s => s.ShouldBeSend);

			foreach (var groupedSms in groupedSmses) {
				var request = (HttpWebRequest)WebRequest.Create(@"https://transport.sms-pager.com:7214/send.xml");
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
							smsMessage.IsSended = true;
							smsMessage.SendToOperatorDate = DateTime.Now;
							smsMessage.SMSID = smsId;
							smsMessage.Save();
						}
						else {
							_log.Error(
								string.Format(
									"Не было отправлено сообщение для клиента {0} Из-за того, что небыл найден номер для отправки. Текст: {1}",
									smsMessage.Client.Id, smsMessage.Text));
						}
					}
				}

				document.Declaration = new XDeclaration("1.0", "utf-8", "true");
				var data = Encoding.UTF8.GetBytes(document.ToString());

				request.ContentType = "text/xml";
				request.ContentLength = data.Length;
				request.Method = "POST";

				var outStream = request.GetRequestStream();

				outStream.Write(data, 0, data.Length);
				outStream.Close();

				var responseStream = request.GetResponse().GetResponseStream();

				var reader = new StreamReader(responseStream);
				var resultDoc = XDocument.Parse(reader.ReadToEnd());
				responseStream.Close();
				var resultDocData = resultDoc.Element("data");
				if (resultDocData != null) {
					foreach (var smsMessage in groupedSms.Where(s => !string.IsNullOrEmpty(s.PhoneNumber))) {
						smsMessage.ServerRequest = Int32.Parse(resultDocData.Element("code").Value);
						smsMessage.Save();
					}
				}
				result.Add(resultDoc);
			}

			return result;
		}
	}
}