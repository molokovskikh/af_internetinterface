using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using CassiniDev;
using Castle.ActiveRecord;
using InternetInterface;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

namespace InforoomInternet.Test.Unit
{
	[TestFixture]
	internal class BaseFunctional : WatinFixture
	{
		private Server _webServer;

		[SetUp]
		public void SetupFixture()
		{

			//WatinFixture.ConfigTest();

			var port = int.Parse(ConfigurationManager.AppSettings["webPort"]);

			var webDir = string.Empty;
			if (Environment.MachineName.ToLower() == "devsrv")
				webDir = ConfigurationManager.AppSettings["webDirectoryDev"];
			else
				webDir = ConfigurationManager.AppSettings["webDirectory"];

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
			var mashineIp = BigEndianConverter.ToInt32(
				IPAddress.Parse("127.0.0.1").
					GetAddressBytes());
			using (new SessionScope())
			{
				foreach (var lease in Lease.Queryable.Where(l => l.Ip == mashineIp).ToList())
					lease.Delete();
				new Lease {
							  Ip = mashineIp,
							  Endpoint = new ClientEndpoints {
																 Client = new Client {
																						  Disabled = false,
																						  LawyerPerson =
																							  new LawyerPerson {
																												   Balance
																													   =
																													   -100,
																												   Tariff
																													   =
																													   1000,
																											   }
																					  }
															 }
						  }.Save();
			}
			using (var browser = Open("Warning?host=http://google.com&url="))
			{
				Thread.Sleep(1000);
				Assert.That(browser.Text, Is.StringContaining("Продолжить"));
				Assert.That(browser.Text,
							Is.StringContaining(
								"Ваша задолженность за оказанные услуги по доступу в интернет составляет 100,00 руб."));
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
			using (new SessionScope())
			{
				foreach (var lease in Lease.Queryable.Where(l => l.Ip == mashineIp).ToList())
					lease.Delete();
				new Lease {
							  Ip = mashineIp,
							  Endpoint = new ClientEndpoints {
																 Client = new Client {
																						  Disabled = false,
																						  PhysicalClient =
																							  new PhysicalClients() {
																														Balance
																															=
																															-200,
																														Tariff
																															=
																															new Tariff {
																																		   Name
																																			   =
																																			   "Тестовый",
																																		   Price
																																			   =
																																			   100,
																																		   Description
																																			   =
																																			   "Тестовый тариф"
																																	   }
																													}
																					  }
															 }
						  }.Save();
			}
			using (var browser = Open("Warning?host=http://google.com&url="))
			{
				Thread.Sleep(1000);
				Assert.That(browser.Text, Is.StringContaining("Продолжить"));
				Assert.That(browser.Text,
							Is.StringContaining(
								"Ваша задолженность за оказанные услуги по доступу в интернет составляет 200,00 руб."));
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
				Assert.That(browser.Text, Is.StringContaining("Уважаемые абоненты!"));
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
		public void WarningPackageIdTest()
		{
			using (var browser = Open("Main/WarningPackageId"))
			{
				Assert.That(browser.Text, Is.StringContaining("К сожалению, услуга доступа интернет Вам временно заблокирована."));
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
				browser.TextField("phone_").AppendText("8-900-900-90-90");
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
				var imageIds = new List<string> {
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
			using (new SessionScope())
			{
				ClientService.DeleteAll();
			}
			Client client;
			PhysicalClients phisClient;
			string clientId;
			using (var browser = Open("PrivateOffice/IndexOffice"))
			{
				using (new SessionScope())
				{
					clientId = browser.Element("clientId").GetAttributeValue("value");
					client = Client.Find(Convert.ToUInt32(clientId));
					phisClient = client.PhysicalClient;
					Assert.That(browser.Text, Is.StringContaining("Ваш личный кабинет"));
					Assert.That(browser.Text,
								Is.StringContaining("Номер лицевого счета для оплаты через терминалы " +
													client.Id.ToString("00000")));
					Assert.That(browser.Text,
								Is.StringContaining(
									WriteOff.Queryable.Where(w => w.Client == client).First().WriteOffSum.ToString()));

					phisClient.Balance = -100;
					phisClient.UpdateAndFlush();
					//client.PostponedPayment = null;
					client.Disabled = true;
					client.AutoUnblocked = true;
					client.UpdateAndFlush();
					foreach (var payment in Payment.Queryable.Where(p => p.Client == client).ToList())
					{
						payment.Delete();
					}

				}
			}
			using (new SessionScope())
			{
				client.Refresh();
				Assert.IsFalse(client.PaymentForTariff());
				new Payment {
								Client = client,
								Sum = client.GetPriceForTariff()*2,
							}.Save();
				client.ClientServices.Clear();
				client.Update();
			}
			using (var browser = Open("PrivateOffice/IndexOffice"))
			using (new SessionScope())
			{
				
				Console.WriteLine(client.PhysicalClient != null);
				Console.WriteLine(!client.ClientServices.Select(c => c.Service).Contains(Service.GetByType(typeof(DebtWork))));
				Console.WriteLine(client.Disabled);
				Console.WriteLine(client.PhysicalClient.Balance < 0);
				Console.WriteLine(client.AutoUnblocked);
				Console.WriteLine(client.PaymentForTariff());
				browser.Element("podrob").Click();
				//Console.WriteLine(browser.Html);
				browser.Button("PostponedBut").Click();
				Console.WriteLine(browser.Text);
				Assert.That(browser.Text, Is.StringContaining("Ваш личный кабинет"));
			}
			using (new SessionScope())
			{
				client = Client.Find(Convert.ToUInt32(clientId));
				Assert.IsFalse(client.Disabled);
			}

			Console.WriteLine("PrivateOfficeTest Complite");
		}

		[Test]
		public void LinkTest()
		{
			List<string> links;
			using (var browser = Open(""))
			{
				links = browser.Div(Find.ByClass("middle")).Links.Select(l => l.Url.Split(new[] { '/' })[3] + "/" + l.Url.Split(new[] { '/' })[4]).ToList();
			}
			foreach (var link in links)
			{
				Console.WriteLine(link);
				using (var browser = Open(link))
				{
					Assert.That(browser.Text, Is.StringContaining("Контакты"));
					Thread.Sleep(2000);
				}
			}
			Console.WriteLine("LinkTest Complite");
		}
	}
}
