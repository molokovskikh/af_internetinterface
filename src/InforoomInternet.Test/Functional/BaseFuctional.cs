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

			_webServer = new Server(port, "/", Path.GetFullPath(webDir));
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
		public void PrivateOffice()
		{
			var phisClient = new PhisicalClients
			{
				Name = "Петр",
				Patronymic = "Иванович",
				Password = CryptoPass.GetHashString("123")
			};
			phisClient.SaveAndFlush();
			new Clients
			{
				PhisicalClient = phisClient
			}.SaveAndFlush();
		}
	}
}
