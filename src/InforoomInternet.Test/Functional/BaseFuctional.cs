using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CassiniDev;
using InternetInterface;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

namespace InforoomInternet.Test.Unit
{
	[TestFixture]
	class BaseFunctional: WatinFixture 
	{
		private Server _webServer;

		[SetUp]
		public void SetupFixture()
		{

			//WatinFixture.ConfigTest();

			var port = int.Parse(ConfigurationManager.AppSettings["webPort"]);
			var webDir = ConfigurationManager.AppSettings["webDirectory"];

			_webServer = new Server(port, "/", Path.GetFullPath(webDir), false, true);
			_webServer.Start();
			Settings.Instance.AutoMoveMousePointerToTopLeft = false;
			Settings.Instance.MakeNewIeInstanceVisible = false;
		}

		[TearDown]
		public void TeardownFixture()
		{
			_webServer.ShutDown();
		}



		[Test]
		public void WarningTest()
		{
			using (var browser = Open("Warning?host=http://google.com&url="))
			{
				Thread.Sleep(1000);
				Assert.That(browser.Text, Is.StringContaining("Продолжить"));
				browser.Button(Find.ById("ConButton")).Click();
				Thread.Sleep(2000);
				Assert.That(browser.Text, Is.StringContaining("google"));
			}
			using (var browser = Open("Warning"))
			{
				Thread.Sleep(1000);
				Assert.That(browser.Text, Is.StringContaining("Продолжить"));
				browser.Button(Find.ById("ConButton")).Click();
				Thread.Sleep(2000);
				Assert.That(browser.Text, Is.StringContaining("Тарифы"));
			}
			Console.WriteLine("WarningTest Complite");
		}

		[Test]
		public void MainTest()
		{
			using (var browser = Open(""))
			{
				Assert.That(browser.Text, Is.StringContaining("Тарифы"));
				Assert.That(browser.Text, Is.StringContaining("Контакты"));
				Assert.That(browser.Text, Is.StringContaining("Личный кабинет"));
				Assert.That(browser.Text, Is.StringContaining("Реквизиты"));
				Assert.That(browser.Text, Is.StringContaining(Tariff.FindFirst().Name));
			}
			Console.WriteLine("MainTest Complite");
		}

		[Test]
		public void RequsiteTest()
		{
			using (var browser = Open("Main/requisite"))
			{
				Assert.That(browser.Text, Is.StringContaining("ОГРН"));
				Assert.That(browser.Text, Is.StringContaining("КПП"));
				Assert.That(browser.Text, Is.StringContaining("Телефон"));
				Assert.That(browser.Text, Is.StringContaining("Расчетный"));
				Assert.That(browser.Text, Is.StringContaining("Директор"));
			}
			Console.WriteLine("RequsiteTest Complite");
		}

		[Test]
		public void ZayavkaTest()
		{
			using (var browser = Open("Main/zayavka"))
			{
				Assert.That(browser.Text, Is.StringContaining("Номер телефона:"));
				Assert.That(browser.Text, Is.StringContaining("Электронная почта:"));
				browser.TextField("fio").AppendText("pupkin vasilii aristarkhovich");
				browser.TextField("phone_").AppendText("9991234567");
				browser.TextField("email").AppendText("vasia@mail.ru");
				browser.TextField("City").AppendText("bebsk");
				browser.TextField("residence").AppendText("matrosova");
				browser.TextField("House").AppendText("45");
				browser.TextField("CaseHouse").AppendText("5");
				browser.TextField("Apartment").AppendText("1");
				browser.TextField("Entrance").AppendText("1");
				browser.TextField("Floor").AppendText("1");
				browser.Button("appbut").Click();
				Thread.Sleep(2000);
				Assert.That(browser.Text, Is.StringContaining("Спасибо, Ваша заявка принята."));
				Requests.FindAll().Last().DeleteAndFlush();
			}
			using (var browser = Open("Main/zayavka"))
			{
				browser.Button("appbut").Click();
				Thread.Sleep(500);
				Assert.That(browser.Text, Is.StringContaining("Пожалуйста, укажите свои данные"));
				Assert.That(browser.Text, Is.StringContaining("Введите улицу"));
				Assert.That(browser.Text, Is.StringContaining("Введите номер этажа"));
			}
			Console.WriteLine("ZayavkaTest Complite");
		}


		[Test]
		public void OfferTest()
		{
			using (var browser = Open("Main/OfferContract"))
			{
				Assert.That(browser.Text, Is.StringContaining("Договор оферта стр1."));
				Assert.That(browser.Text, Is.StringContaining("Договор оферта стр5."));
				Assert.That(browser.Text, Is.StringContaining("Тарифы "));
				Assert.That(browser.Text, Is.StringContaining("Порядок расчетов стр1."));
				var imageIds = new List<string>
				               	{
				               		"imd1",
									"imd2",
									"imd3",
									"imd4",
									"imd5",
									"imt",
									"imo1",
									"imo2",
									"imo3",
									"imp1",
									"imp2",
									"impo1",
									"impo2",
									"impo3"
				               	};
				var imagesUris = new List<string>();
				foreach (var imageId in imageIds)
				{
					imagesUris.Add(browser.Image(imageId).Uri.AbsoluteUri);
				}
				foreach (var imagesUri in imagesUris)
				{
					browser.GoTo(imagesUri);
					Assert.That(browser.Text, Is.Not.StringContaining("Description: HTTP 404"));
				}
				Console.WriteLine("OfferTest Complite");
			}
		}

		[Test]
		public void PrivateOfficeTest()
		{
			var phisClient = new PhysicalClients
			{
				Name = "Петр",
				Patronymic = "Иванович",
				Balance = 500,
				Password = CryptoPass.GetHashString("123")
			};
			phisClient.SaveAndFlush();
			var client = new Clients
			{
				PhisicalClient = phisClient
			};
			client.SaveAndFlush();
			new Payment
				{
					Client = phisClient,
					Agent = Agent.FindFirst(),
					Sum = 500.ToString()
				}.SaveAndFlush();
			new WriteOff
				{
					Client = client,
					WriteOffDate = DateTime.Now,
					WriteOffSum = 400
				}.SaveAndFlush();
			using (var browser = Open("PrivateOffice/IndexOffice"))
			{
				browser.TextField("Login").AppendText(phisClient.Id.ToString());
				browser.TextField("Password").AppendText("123");
				browser.Button("LogBut").Click();
				Thread.Sleep(500);
				Assert.That(browser.Text, Is.StringContaining("Ваш личный кабинет, Петр Иванович"));
				Assert.That(browser.Text, Is.StringContaining("Номер лицевого счета для оплаты через терминалы " + phisClient.Id.ToString("00000")));
				Assert.That(browser.Text, Is.StringContaining("500"));
				Assert.That(browser.Text, Is.StringContaining("400"));
			}
			Console.WriteLine("PrivateOfficeTest Complite");
		}

		[Test]
		public void LinkTest()
		{
			using (var browser = Open(""))
			{
				Console.WriteLine(browser.Text);
				browser.Link("maina").Click();
				Assert.That(browser.Text, Is.StringContaining("Тарифы"));
				browser.Link("requisite").Click();
				Assert.That(browser.Text, Is.StringContaining("ИНН"));
				browser.Link("zayavka").Click();
				Assert.That(browser.Text, Is.StringContaining("Электронная почта:"));
				browser.Link("OfferContract").Click();
				Assert.That(browser.Text, Is.StringContaining("Договор оферта стр2. "));
				browser.Link("PrivateOffice").Click();
				Assert.That(browser.Text, Is.StringContaining("Логин"));
				browser.Link("HowPay2").Click();
				Assert.That(browser.Text, Is.StringContaining("QIWI"));
				browser.Link("HowPay1").Click();
				Assert.That(browser.Text, Is.StringContaining("QIWI"));

				browser.Link("Main1").Click();
				Assert.That(browser.Text, Is.StringContaining("Тарифы"));
				browser.Link("requisite1").Click();
				Assert.That(browser.Text, Is.StringContaining("ИНН"));
				browser.Link("Zayavka1").Click();
				Assert.That(browser.Text, Is.StringContaining("Электронная почта:"));
				browser.Link("OfferContract1").Click();
				Assert.That(browser.Text, Is.StringContaining("Договор оферта стр2. "));
				browser.Link("PrivateOffice1").Click();
			}
			Console.WriteLine("LinkTest Complite");
		}
	}
}
