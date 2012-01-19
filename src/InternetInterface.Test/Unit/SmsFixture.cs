using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class SmsFixture
	{
		[Test]
		public void GetElementTest()
		{
			var doc = XDocument.Load("c:/0_test.xml");
			doc.Element("code");
		}

		[Test]
		public void BaseSmsTest()
		{
			var messeges = new List<SmsMessage>() {
				new SmsMessage() {
					CreateDate = DateTime.Now,
					PhoneNumber = "+79507738447",
					ShouldBeSend = DateTime.Now.AddMinutes(5),
					Text = "Ваш баланс 100 рублей (смс с задержкой 5 минут)"
				},
				new SmsMessage() {
					CreateDate = DateTime.Now,
					PhoneNumber = "+79507738447",
					ShouldBeSend = DateTime.Now.AddMinutes(5),
					Text = "Ваш баланс 200 рублей (смс с задержкой 5 минут)"
				},
				new SmsMessage() {
					CreateDate = DateTime.Now,
					PhoneNumber = "+79507738447",
					Text = "Смс отправлена немедленно"
				},
				new SmsMessage() {
					CreateDate = DateTime.Now,
					PhoneNumber = "+79507738447"
				}
			};
			messeges.ForEach(m => m.Save());

			var result = SmsHelper.SendMessages(messeges);

			for (int i = 0; i < result.Count; i++) {
				result[i].Save(string.Format("c:/{0}_test.xml", i));
			}

			foreach (var xDocument in result) {
				Assert.That(xDocument.Element("data").Element("code").Value, Is.EqualTo((int)SmsRequestType.ValidOperation));
			}
		}
	}
}
