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
			using (new SessionScope()) {
				foreach (var lease in Lease.Queryable.Where(l => l.Ip == mashineIp).ToList())
					lease.Delete();
				new Lease {
					Ip = mashineIp,
					Endpoint = new ClientEndpoints {
						Client = new Client {
							Disabled = false,
							LawyerPerson =
								new LawyerPerson {
									Balance = -100,
									Tariff = 1000,
								}
						}
					}
				}.Save();
			}
			using (var ie = Open("Warning?host=google.com&url=")) {
				Thread.Sleep(1000);
				Assert.That(ie.Text, Is.StringContaining("Продолжить"));
				Assert.That(ie.Text, Is.StringContaining("Ваша задолженность за оказанные услуги по доступу в интернет составляет 100,00 руб."));
				ie.Button(Find.ById("ConButton")).Click();
				Thread.Sleep(1000);
				Console.WriteLine(ie.Uri);
				Assert.That(ie.Text, Is.StringContaining("google"));
			}
			using (var browser = Open("Warning")) {
				Thread.Sleep(1000);
				Assert.That(browser.Text, Is.StringContaining("Продолжить"));
				browser.Button(Find.ById("ConButton")).Click();
				Thread.Sleep(2000);
				Assert.That(browser.Text, Is.StringContaining("Тарифы"));
			}
			using (new SessionScope()) {
				foreach (var lease in Lease.Queryable.Where(l => l.Ip == mashineIp).ToList())
					lease.Delete();
				new Lease {
					Ip = mashineIp,
					Endpoint = new ClientEndpoints {
						Client = new Client {
							Disabled = false,
							RatedPeriodDate = DateTime.Now,
							PhysicalClient =
								new PhysicalClients {
									Balance = -200,
									Tariff = new Tariff {
											Name = "Тестовый",
											Price = 100,
											Description = "Тестовый тариф"
										}
								}
						}
					}
				}.Save();
			}
			using (var ie = Open("Warning?host=google.com&url=")) {
				Thread.Sleep(1000);
				Assert.That(ie.Text, Is.StringContaining("Продолжить"));
				Assert.That(ie.Text,
				            Is.StringContaining(
				            	"Ваша задолженность за оказанные услуги составляет 200,00 руб"));
				ie.Button(Find.ById("ConButton")).Click();
				Thread.Sleep(2000);
				Assert.That(ie.Text, Is.StringContaining("google"));
			}
			using (var ie = Open("Warning")) {
				Thread.Sleep(1000);
				Assert.That(ie.Text, Is.StringContaining("Продолжить"));
				ie.Button(Find.ById("ConButton")).Click();
				Thread.Sleep(2000);
				Assert.That(ie.Text, Is.StringContaining("Тарифы"));
			}
			Console.WriteLine("WarningTest Complite");
		}

		[Test]
		public void MainTest()
		{
			using (var ie = Open(""))
			{
				Thread.Sleep(500);
				Assert.That(ie.Text, Is.StringContaining("Тарифы"));
				Assert.That(ie.Text, Is.StringContaining("Контакты"));
				Assert.That(ie.Text, Is.StringContaining("Личный кабинет"));
				Assert.That(ie.Text, Is.StringContaining("Реквизиты"));
				Assert.That(ie.Text, Is.StringContaining("Уважаемые абоненты!"));
			}
			Console.WriteLine("MainTest Complite");
		}

		[Test]
		public void RequsiteTest()
		{
			using (var ie = Open("Main/requisite"))
			{
				Thread.Sleep(500);
				Assert.That(ie.Text, Is.StringContaining("ОГРН"));
				Assert.That(ie.Text, Is.StringContaining("КПП"));
				Assert.That(ie.Text, Is.StringContaining("Телефон"));
				Assert.That(ie.Text, Is.StringContaining("Расчетный"));
				Assert.That(ie.Text, Is.StringContaining("Директор"));
			}
			Console.WriteLine("RequsiteTest Complite");
		}

		[Test]
		public void WarningPackageIdTest()
		{
			using (var ie = Open("Main/WarningPackageId"))
			{
				Thread.Sleep(500);
				Assert.That(ie.Text, Is.StringContaining("К сожалению, услуга доступа интернет Вам временно заблокирована."));
			}
			Console.WriteLine("RequsiteTest Complite");
		}

		[Test]
		public void ZayavkaTest()
		{
			using (var ie = Open("Main/zayavka"))
			{
				Thread.Sleep(500);
				Assert.That(ie.Text, Is.StringContaining("Номер телефона:"));
				Assert.That(ie.Text, Is.StringContaining("Электронная почта:"));
				ie.TextField("fio").AppendText("pupkin vasilii aristarkhovich");
				ie.TextField("phone_").AppendText("8-900-900-90-90");
				ie.TextField("email").AppendText("vasia@mail.ru");
				ie.TextField("City").AppendText("bebsk");
				ie.TextField("residence").AppendText("matrosova");
				ie.TextField("House").AppendText("45");
				ie.TextField("CaseHouse").AppendText("5");
				ie.TextField("Apartment").AppendText("1");
				ie.TextField("Entrance").AppendText("1");
				ie.TextField("Floor").AppendText("1");
				ie.Button("appbut").Click();
				Thread.Sleep(2000);
				Assert.That(ie.Text, Is.StringContaining("Спасибо, Ваша заявка принята."));
				Requests.FindAll().Last().DeleteAndFlush();
			}
			using (var ie = Open("Main/zayavka"))
			{
				ie.Button("appbut").Click();
				Thread.Sleep(500);
				Assert.That(ie.Text, Is.StringContaining("Введите ФИО"));
				Assert.That(ie.Text, Is.StringContaining("Введите улицу"));
				Assert.That(ie.Text, Is.StringContaining("Введите номер дома"));
			}
			Console.WriteLine("ZayavkaTest Complite");
		}


		[Test]
		public void OfferTest()
		{
			using (var ie = Open("Main/OfferContract"))
			{
				Thread.Sleep(500);
				Assert.That(ie.Text, Is.StringContaining("Договор оферта стр1."));
				Assert.That(ie.Text, Is.StringContaining("Договор оферта стр5."));
				Assert.That(ie.Text, Is.StringContaining("Тарифы "));
				Assert.That(ie.Text, Is.StringContaining("Порядок расчетов стр1."));
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
					imagesUris.Add(ie.Image(imageId).Uri.AbsoluteUri);
				}
				foreach (var imagesUri in imagesUris)
				{
					ie.GoTo(imagesUri);
					Assert.That(ie.Text, Is.Not.StringContaining("Description: HTTP 404"));
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
			using (var ie = Open(""))
			{
				links = ie.Div(Find.ByClass("middle")).Links.Select(l => l.Url.Split(new[] { '/' })[3] + "/" + l.Url.Split(new[] { '/' })[4]).ToList();
			}
			foreach (var link in links)
			{
				Console.WriteLine(link);
				using (var ie = Open(link))
				{
					Thread.Sleep(100);
					Assert.That(ie.Text, Is.StringContaining("Новости"));
				}
			}
			Console.WriteLine("LinkTest Complite");
		}
	}
}
