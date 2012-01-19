using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	class ByteFixture
	{
		[Test]
		public void ByteTest()
		{
			byte[] raw = BitConverter.GetBytes(Convert.ToInt64("2886730166"));
			var fg = new byte[8];
			fg[0] = 182;
			fg[1] = 1;
			fg[2] = 16;
			fg[3] = 172;
			string dfg = BitConverter.ToInt64(fg, 0).ToString();
			Assert.That(dfg, Is.StringContaining("2886730166"));
		}

		[Test]
		public void DateTest()
		{
			Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			var day = DateTime.Now;
			var indexDay = (int) day.DayOfWeek;
			Console.WriteLine(day.AddDays(-indexDay+1));
			Console.WriteLine(day.AddDays(7 - indexDay));
		}

		public bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		[Test]
		public void SmsTest()
		{
			ServicePointManager.ServerCertificateValidationCallback =
				new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);

			var request = WebRequest.Create(@"https://transport.sms-pager.com:7214/send.xml");

			/*var data =
				Encoding.UTF8.GetBytes(
					@"<?xml version='1.0' encoding='UTF-8'?>
<send>
	<login>infoorom</login>
	<password>4c6ly</password>
	<action>check</action>
	<text>TEST_TEXT</text>
	<to number='+79507738447'></to>
</send>");*/

			var document = new XDocument(
					new XElement("data",
							new XElement("login", "inforoom"),
							new XElement("password", "analitFarmacia"),
							new XElement("action", "send"),
							//new XElement("text", "Проверка рассылки SMS компании Inforoom (от Золотарева)"),
							new XElement("source", "inforoom")/*,
							new XElement("to", 
									new XAttribute("number", "+79507738447")
								),
							new XElement("to", 
									new XAttribute("number", "+79155415633")
								),
							new XElement("to", 
									new XAttribute("number", "+79103495077")
								)*/
						)
				);

			var dataElement = document.Element("data");
			if (dataElement != null)
				dataElement.Add(new XElement("to", new XAttribute("number", "+79507738447"), "Проверка рассылки SMS компании Inforoom (от Золотарева)"));

			document.Declaration = new XDeclaration("1.0", "utf-8", "true");

			Console.WriteLine(document);
			//document.Save("c:/test.xml");
			//document.

			var data = Encoding.UTF8.GetBytes(document.ToString());
			//string fileName = @"C:\1.xml";

			request.Method = "POST";
			request.ContentType = "text/xml";
			request.ContentLength = data.Length;
			//request.Proxy = null;

			var outStream = request.GetRequestStream();

			outStream.Write(data, 0, data.Length);
			outStream.Close();

			WebResponse response = request.GetResponse();

			var responseStream = response.GetResponseStream();

			var reader = new StreamReader(responseStream);
			string htmlContent = reader.ReadToEnd();

			Console.WriteLine(htmlContent);
		}
	}
}

//infoorom
//4c6ly

//inforoom
//analitFarmacia