#define BILLING_TEST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Controllers.Filter;
using NUnit.Framework;
using InternetInterface.Models;


namespace Billing.Test.Unit
{
	[TestFixture]
	public class IntegrationFixture
	{
		protected ISessionScope scope;

		[SetUp]
		public void Setup()
		{
			scope = new SessionScope();
		}

		[TearDown]
		public void TearDown()
		{
			if (scope != null)
				scope.Dispose();
		}
	}

	public class MainBillingFixture : IntegrationFixture
	{
		private MainBilling billing;

		[SetUp]
		public void CreateBilling()
		{
			billing = new MainBilling();
		}

		public static void PrepareTests()
		{
			new Partner
			{
				Login = "Test",
			}.Save();



			//SessionScope.Current.Flush();

			InitializeContent.GetAdministrator = () => Partner.FindFirst();

			new Status
			{
				Blocked = false,
				Id = (uint)StatusType.Worked,
				Name = "unblocked"
			}.Save();

			new Status
			{
				Blocked = true,
				Id = (uint)StatusType.NoWorked,
				Name = "testBlockedStatus"
			}.Save();

			new Status {
						   ShortName = "VoluntaryBlocking",
						   Id = (uint)StatusType.VoluntaryBlocking,
						   Blocked = true,
						   Connected = true,
					   }.Save();

			new DebtWork
			{
				BlockingAll = false,
				Price = 0
			}.Save();

			new VoluntaryBlockin
			{
				BlockingAll = true,
				Price = 0
			}.Save();

			new InternetSettings{NextBillingDate = DateTime.Now}.Save();
		}

		public Client CreateClient()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, 590);
			client.Save();
			return client;
		}

		private static void PrepareTest()
		{
			UserWriteOff.DeleteAll();
			ClientService.DeleteAll();
			WriteOff.DeleteAll();
			Payment.DeleteAll();
			Client.DeleteAll();
			SystemTime.Reset();
		}

		private void SetClientDate(Client client, Interval rd)
		{
			client = Client.FindFirst();
			client.RatedPeriodDate = rd.dtFrom;
			client.Update();
			SystemTime.Now = () => rd.dtTo;
			//billing.DtNow = rd.dtTo;
			billing.Compute();
		}

		[Test]
		public void TariffTest()
		{
			var client = CreateClient();
			var intervalTariff = new Tariff {
												FinalPrice = 200,
												FinalPriceInterval = 2,
												Price = 100,
											};
			intervalTariff.Save();
			client.PhysicalClient.Tariff = intervalTariff;
			client.Update();
			Assert.That(client.GetPrice(), Is.EqualTo(100));
			SystemTime.Now = () => DateTime.Now.AddMonths(2).AddHours(1);
			Assert.That(client.GetPrice(), Is.EqualTo(200));
			var simpleTariff = new Tariff {
											  Price = 300
										  };
			client.PhysicalClient.Tariff = simpleTariff;
			client.Update();
			Assert.That(client.GetPrice(), Is.EqualTo(300));
		}

		[Test]
		public void UserWriteOffTest()
		{
			PrepareTest();
			SystemTime.Reset();
			var client = CreateClient();
			client.Save();
			var lawyerPerson = new LawyerPerson {
			                                    	Balance = 1000
			                                    };
			lawyerPerson.Save();
			client.PhysicalClient = null;
			client.LawyerPerson = lawyerPerson;
			client.UpdateAndFlush();
			new UserWriteOff {
			                 	Client = client,
			                 	Sum = 500,
			                 	Date = SystemTime.Now()
			                 }.Save();
			billing.On();
			client.Refresh();
			Assert.That(client.LawyerPerson.Balance, Is.EqualTo(500m));
		}

		[Test]
		public void TimeTest()
		{
			var client = CreateClient();
			client.RatedPeriodDate = DateTime.Now;
			client.Update();
			var time = InternetSettings.FindFirst();
			time.NextBillingDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day - 1, 22, 00, 00);
			time.Update();
			SystemTime.Reset();
			billing.Run();
			Assert.That(WriteOff.Queryable.Count(w => w.Client == client), Is.EqualTo(1));
			billing.Run();
			billing.Run();
			Assert.That(WriteOff.Queryable.Count(w => w.Client == client), Is.EqualTo(1));
			SystemTime.Now = () => new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 22, 10, 00);
			billing.Run();
			Assert.That(WriteOff.Queryable.Count(w => w.Client == client), Is.EqualTo(2));


		}

		[Test]
		public void DebtWorkTest()
		{
			PrepareTest();
			SystemTime.Reset();
			var client = CreateClient();
			const int countDays = 5;
			var physClient = client.PhysicalClient;
			physClient.Balance = -10m;
			physClient.Update();
			client.Disabled = true;
			client.AutoUnblocked = true;
			client.RatedPeriodDate = SystemTime.Now();
			client.Update();

			var CServive = new ClientService {
												 Client = client,
												 BeginWorkDate = DateTime.Now,
												 EndWorkDate = SystemTime.Now().AddDays(countDays),
												 Service = Service.GetByType(typeof (DebtWork)),
											 };
			/*CServive.Save();
			client.Refresh();*/

			client.ClientServices.Add(CServive);
			CServive.Activate();
			for (int i = 0; i < countDays; i++)
			{
				billing.OnMethod();
				billing.Compute();
				SystemTime.Now = () => DateTime.Now.AddDays(i + 1);
			}
			/*physClient.Refresh();
			client.Refresh();*/
			Console.WriteLine(client.RatedPeriodDate);
			Assert.That(physClient.Balance, Is.EqualTo(-10));
			new Payment {
							Client = client,
							Sum = client.PhysicalClient.Tariff.Price,
							PaidOn = DateTime.Now.AddDays(-1)
						}.Save();
			billing.OnMethod();
			physClient.Balance = -10;
			client.RatedPeriodDate = DateTime.Now;
			client.Update();
			physClient.Update();
			client.Refresh();
			SystemTime.Reset();
			//CServive.Refresh();
			//CServive.Client.Update();
			CServive.Activate();
			for (int i = 0; i < countDays; i++)
			{
				billing.OnMethod();
				billing.Compute();
				int i1 = i;
				SystemTime.Now = () => DateTime.Now.AddDays(i1 + 1);
			}
			SystemTime.Now = () => DateTime.Now.AddDays(countDays + 1);
			billing.OnMethod();
			//client.Refresh();
			Assert.That(WriteOff.FindAll().Count(), Is.EqualTo(countDays +1));
			Assert.That(physClient.Balance, Is.LessThan(0m));
			Assert.IsTrue(client.Disabled);
			client.Disabled = false;
			client.Update();
			//SystemTime.Reset();
			Assert.That(Math.Round(-client.GetPrice()/client.GetInterval()*countDays, 5) - 10,
						Is.EqualTo(Math.Round(physClient.Balance, 5)));
			Assert.That(client.RatedPeriodDate.Value.Date, Is.EqualTo(DateTime.Now.Date));
			CServive = new ClientService {
											 Client = client,
											 BeginWorkDate = DateTime.Now,
											 EndWorkDate = SystemTime.Now().AddDays(countDays),
											 Service = Service.GetByType(typeof (DebtWork)),
										 };
			client.ClientServices.Add(CServive);
			//CServive.Save();
			Assert.That(CServive.LogComment, !Is.EqualTo(string.Empty));
			client.Disabled = true;
			client.Update();
			new Payment {
							Client = client,
							Sum = client.PhysicalClient.Tariff.Price,
							PaidOn = DateTime.Now.AddDays(1)
						}.Save();
			SystemTime.Now = () => DateTime.Now.AddDays(countDays + 1);
			billing.OnMethod();
			physClient.Balance = -10;
			physClient.Update();
			//client.Refresh();
			CServive = new ClientService {
											 Client = client,
											 BeginWorkDate = DateTime.Now,
											 EndWorkDate = SystemTime.Now().AddDays(countDays),
											 Service = Service.GetByType(typeof (DebtWork)),
										 };
			client.ClientServices.Add(CServive);
			//CServive.Save();
			CServive.Activate();
			Assert.That(CServive.Activated, Is.EqualTo(true));
		}

		[Test]
		public void DebtWorkTestPartner()
		{
			PrepareTest();
			SystemTime.Reset();
			var client = CreateClient();
			const int countDays = 10;
			var physClient = client.PhysicalClient;
			physClient.Balance = -10m;
			physClient.Update();
			client.Disabled = true;
			client.AutoUnblocked = true;
			client.RatedPeriodDate = SystemTime.Now();
			client.Update();
			//var service = Service.GetByType(typeof (DebtWork));
			//Console.WriteLine(service);
			var CServive = new ClientService
			{
				Client = client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = SystemTime.Now().AddDays(countDays),
				Service = Service.GetByType(typeof(DebtWork)),
				Activator = InitializeContent.partner
			};
			client.ClientServices.Add(CServive);
			//CServive.Save();
			CServive.Refresh();
			CServive.Activate();
			Assert.That(CServive.Activated, Is.EqualTo(true));
			Assert.IsFalse(CServive.Client.Disabled);
			CServive.CompulsoryDiactivate();
			Assert.IsTrue(CServive.Client.Disabled);
			//scope.Flush();
			Assert.That(client.ClientServices, Is.Empty);
		}

		[Test]
		public void VoluntaryBlockinTest()
		{
			PrepareTest();
			SystemTime.Reset();
			var client = CreateClient();
			int countDays = 10;
			var physClient = client.PhysicalClient;
			//client.Disabled = true;
			client.AutoUnblocked = true;
			client.Update();
			var service = new ClientService {
												Client = client,
												Activator = InitializeContent.partner,
												Service = Service.GetByType(typeof(VoluntaryBlockin)),
												BeginWorkDate = DateTime.Now.AddDays(2),
												EndWorkDate = DateTime.Now.AddDays(countDays+2)
											};
			client.ClientServices.Add(service);
			service.Activate();
			billing.OnMethod();
			billing.Compute();
			client.Refresh();
			Assert.IsFalse(client.Disabled);
			WriteOff.DeleteAll();
			SystemTime.Now = () => DateTime.Now.AddDays(2);
			billing.OnMethod();
			client.Refresh();
			Assert.IsTrue(client.Disabled);
			for (int i = 0; i < countDays; i++)
			{
				billing.OnMethod();
				billing.Compute();
				int i1 = i;
				SystemTime.Now = () => DateTime.Now.AddDays(i1 + 3);
			}
			Assert.That(WriteOff.Queryable.Where(c => c.Client == client).ToList().Sum(w => w.WriteOffSum),
						Is.EqualTo(0));
			SystemTime.Now = () => DateTime.Now.AddDays(countDays + 3);
			billing.OnMethod();
			//client.Refresh();
			Assert.IsFalse(client.Disabled);
			Assert.That(client.ClientServices, !Is.Empty);
			SystemTime.Now = () => service.EndWorkDate.Value.AddDays(46);
			billing.On();
			Assert.That(client.ClientServices, Is.Empty);
			Assert.IsFalse(client.Disabled);
			SystemTime.Reset();
			service = new ClientService
			{
				Client = client,
				Activator = InitializeContent.partner,
				Service = Service.GetByType(typeof(VoluntaryBlockin)),
				BeginWorkDate = DateTime.Now,
			};
			client.ClientServices.Add(service);
			service.Activate();
			countDays = 29;
			//WriteOff.DeleteAll();
			physClient.Refresh();
			while (physClient.Balance > 0)
			{
				billing.OnMethod();
				billing.Compute();
				SystemTime.Now = () => DateTime.Now.AddDays(countDays);
				countDays++;
				physClient.Refresh();
			}
			var firstdate = WriteOff.FindFirst().WriteOffDate;
			Console.WriteLine(physClient.Balance);
			Assert.That(
				Math.Round(
					Convert.ToDecimal(
						((WriteOff.FindAll().Last().WriteOffDate - WriteOff.FindFirst().WriteOffDate).TotalDays + 1)*2),
					2),
				Is.EqualTo(Math.Round(
					WriteOff.Queryable.Where(w => w.Client == client).ToList().Sum(w => w.WriteOffSum), 2)));
			Assert.That(firstdate.Date, Is.EqualTo(DateTime.Now.AddMonths(1).Date));
			WriteOff.FindAll().Select(w => w.WriteOffSum).Each(c => Assert.That(c, Is.EqualTo(2m)));
		}

		[Test]
		public void ActiveDeactive()
		{
			PrepareTest();
			SystemTime.Reset();
			var client = CreateClient();
			const int countDays = 5;
			var physClient = client.PhysicalClient;
			physClient.Balance = -10m;
			physClient.Update();
			client.Disabled = true;
			client.AutoUnblocked = true;
			client.RatedPeriodDate = SystemTime.Now();
			client.Update();

			var CServive = new ClientService
			{
				Client = client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = SystemTime.Now().AddDays(countDays),
				Service = Service.GetByType(typeof(DebtWork)),
				Activator = InitializeContent.partner
			};
			client.ClientServices.Add(CServive);
			CServive.Activate();
			billing.OnMethod();
			billing.Compute();
			Assert.IsFalse(client.Disabled);
			CServive.CompulsoryDiactivate();
			Assert.IsTrue(client.Disabled);
			billing.OnMethod();
			Assert.IsTrue(client.Disabled);
			CServive = new ClientService
			{
				Client = client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = SystemTime.Now().AddDays(countDays),
				Service = Service.GetByType(typeof(VoluntaryBlockin)),
				Activator = InitializeContent.partner
			};
			physClient.Balance = 200m;
			physClient.Update();
			billing.OnMethod();
			Assert.IsFalse(client.Disabled);// = true;
			Assert.IsTrue(client.AutoUnblocked);
			Assert.IsNull(client.RatedPeriodDate);
			//client.AutoUnblocked = true;
			//client.RatedPeriodDate = null;
			//client.Update();
			client.ClientServices.Add(CServive);
			CServive.Activate();
			Assert.IsTrue(client.Disabled);
			SystemTime.Now = () => DateTime.Now.AddMonths(1).AddDays(1);
			billing.Compute();
			Assert.That(WriteOff.FindAll().Last().WriteOffSum, Is.EqualTo(2m));
			CServive.CompulsoryDiactivate();
			billing.Compute();
			Assert.That(WriteOff.FindAll().Last().WriteOffSum, Is.GreaterThan(5m));
			Assert.IsFalse(client.Disabled);
			billing.OnMethod();
			Assert.IsFalse(client.Disabled);
		}


		[Test]
		public void MaxDebtTest()
		{
			var client = CreateClient();
			client.Disabled = false;
			SystemTime.Reset();
			var dayInMonth = DateTime.DaysInMonth(SystemTime.Now().AddDays(-15).Year, SystemTime.Now().AddDays(-15).Month);
			client.RatedPeriodDate = SystemTime.Now().AddDays(-15);
			client.Update();
			billing.Compute();
			var spisD0 = WriteOff.Queryable.FirstOrDefault(w => w.Client == client);
			client.Refresh();
			Console.WriteLine("Interval DO: " + client.GetInterval());
			Assert.That(dayInMonth, Is.EqualTo(client.GetInterval()));
			client.DebtDays = 29;
			client.Update();
			billing.Compute();
			var slisD29 = WriteOff.Queryable.Where(w => w.Client == client).ToList().LastOrDefault();
			client.Refresh();
			Console.WriteLine("Interval D29: " + client.GetInterval());
			Assert.That(dayInMonth + 29, Is.EqualTo(client.GetInterval()));
			Console.WriteLine(string.Format("spisDO: {0}  spisD29: {1}", spisD0.WriteOffSum.ToString("0.00"), slisD29.WriteOffSum.ToString("0.00")));
		}

		[Test]
		public void CanBlockTest()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, -1000);
			client.Disabled = false;
			client.Save();
			Assert.IsTrue(client.CanBlock());
			var service = new ClientService {
								  Client = client,
								  Service = ActiveRecordMediator<DebtWork>.FindFirst() //service.GetByType(typeof(DebtWork))
							  };
			service.Save();
			//client.DebtWork = true;
			//client.Update();
			client.Refresh();
			Assert.IsFalse(client.CanBlock());
			client.ClientServices.Remove(service);
			//scope.Flush();
			//service.Delete();
			service = new ClientService {
											Client = client,
											Service = Service.GetByType(typeof(DebtWork)),
											BeginWorkDate = DateTime.Now,
											EndWorkDate = DateTime.Now.AddDays(1)
										};
			client.ClientServices.Add(service);
			//service.Save();
			//client.DebtWork = false;
			//client.PostponedPayment = DateTime.Now;
			SystemTime.Now = () => DateTime.Now;
			//client.Update();
			Assert.IsFalse(client.CanBlock());
			SystemTime.Now = () => DateTime.Now.AddDays(2);
			Assert.IsTrue(client.CanBlock());
			client.ClientServices.Add(service);
			new ClientService {
								  Client = client,
								  Service = Service.GetByType(typeof(DebtWork))
							  }.Save();
			//client.DebtWork = true;
			client.Refresh();
			Assert.IsFalse(client.CanBlock());
			//client.DebtWork = false;
			//client.PostponedPayment = null;
			//ClientService.DeleteAll();
			client.ClientServices.Clear();
			//client.Refresh();
			Assert.IsTrue(client.CanBlock());
			client.Disabled = true;
			client.Update();
			Assert.IsFalse(client.CanBlock());
		}

		[Test]
		public void Test1151()
		{
			var client = CreateClient();
			client.Disabled = false;
			client.RatedPeriodDate = new DateTime(2011, 5, 31, 15, 05, 23);
			SystemTime.Now = () => new DateTime(2011, 6, 30, 22, 02, 03);
			billing.Compute();
			Console.WriteLine("WriteOffSum "+WriteOff.Queryable.Where(w => w.Client == client).ToList().Last().WriteOffSum.ToString("0.00"));
			client.Refresh();
			Console.WriteLine("RatedDate " + client.RatedPeriodDate.Value.ToShortDateString());
			Console.WriteLine("Interval " + client.GetInterval());
			Console.WriteLine("DebtDays " + client.DebtDays);
			Assert.That(client.DebtDays, Is.EqualTo(1));
			SystemTime.Now = () => new DateTime(2011, 7, 31 , 19, 03, 6);
			billing.Compute();
			Console.WriteLine("WriteOffSum " + WriteOff.Queryable.Where(w => w.Client == client).ToList().Last().WriteOffSum.ToString("0.00"));
			client.Refresh();
			Console.WriteLine("RatedDate " + client.RatedPeriodDate.Value.ToShortDateString());
			Console.WriteLine("Interval " + client.GetInterval());
			Console.WriteLine("DebtDays " + client.DebtDays);
			Assert.That(client.DebtDays, Is.EqualTo(0));
		}

		[Test]
		public void TetsDebtDays()
		{
			var client = CreateClient();
			client.Disabled = false;
			client.RatedPeriodDate = new DateTime(2011, 5, 15, 15, 05, 23);
			SystemTime.Now = () => new DateTime(2011, 6, 15, 22, 02, 03);
			billing.Compute();
			client.Refresh();
			Assert.That(client.DebtDays, Is.EqualTo(0));
			Assert.That(((DateTime)client.RatedPeriodDate).Date, Is.EqualTo(new DateTime(2011, 6, 15)));
		}

		[Test]
		public void FindDebt()
		{
		   var count = 0;
		   var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, 500000);
			client.Disabled = false;
			client.RatedPeriodDate = new DateTime(2011, 6, 9, 15, 00, 9);
			client.Save();
			var tarif = client.PhysicalClient.Tariff;
			tarif.Price = 500;
			tarif.Update();
			while (client.DebtDays < 1 && count < 365)
			{
				Console.WriteLine("Count: " + count + " Date: " + SystemTime.Now().Date);
				SystemTime.Now = () => new DateTime(2011, 6, 7, 22, 15, 9).AddDays(count);
				Console.WriteLine("IterDate " + SystemTime.Now().ToShortDateString());
				billing.Compute();
				client.Refresh();
				count++;
				Console.WriteLine("Rated Date: " + client.RatedPeriodDate);
				Console.WriteLine("DateNow : " + SystemTime.Now());
				Console.WriteLine("-------------------------------------------------");
			}
			Console.WriteLine("***********************************");
			Console.WriteLine("All iterations " + count);
			Console.WriteLine("DebtDays " + client.DebtDays);
			Console.WriteLine("Rated Date: " + client.RatedPeriodDate);
			Console.WriteLine("DateNow : " + SystemTime.Now());
			Assert.That(365 , Is.EqualTo(count));
		}

		[Test]
		public void ShowBalWarning()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("ShowClient", false, 300);
			client.BeginWork = DateTime.Now;
			client.RatedPeriodDate = DateTime.Now;
			client.Save();
			var partBalance = client.GetPrice() / client.GetInterval();
			client.PhysicalClient.Balance = partBalance * 2 - 1;
			client.Update();
			billing.Compute();
			client.Refresh();
			Assert.IsTrue(client.ShowBalanceWarningPage);
			Assert.Greater(client.PhysicalClient.Balance, 0);
			new Payment {
							Client = client, 
							Sum = partBalance,
							BillingAccount = false,
						}.Save();
			billing.OnMethod();
			client.Refresh();
			Assert.IsFalse(client.ShowBalanceWarningPage);
		}

		[Test]
		public void OnTest()
		{
			var unblockedClient = CreateClient();
			unblockedClient.AutoUnblocked = true;
			unblockedClient.Update();
			var phisClient = unblockedClient.PhysicalClient;
			phisClient.Balance = -100;
			phisClient.Update();
			billing.Compute();
			Assert.IsTrue(unblockedClient.Status.Blocked);
			new Payment {
							Client = unblockedClient,
							Sum = 200
						}.Save();
			billing.OnMethod();
			unblockedClient.Refresh();
			Assert.IsFalse(unblockedClient.Status.Blocked);
			Assert.That(unblockedClient.PhysicalClient.Balance, Is.GreaterThan(0));
		}

		[Test]
		public void PostponedPayment()
		{
			var client_Post = BaseBillingFixture.CreateAndSaveClient("testRated", false, 100);
			client_Post.RatedPeriodDate = DateTime.Now;
			client_Post.Save();
			var client_simple = BaseBillingFixture.CreateAndSaveClient("testRated", false, 100);
			client_simple.Save();
			var pclient_post = client_Post.PhysicalClient;
			pclient_post.Balance -= 200;
			pclient_post.Update();

			billing.Compute();

			client_simple.Refresh();
			client_Post.Refresh();
			Assert.IsTrue(client_Post.Disabled);
			Assert.IsFalse(client_simple.Disabled);

			new Payment
			{
				Client = client_Post,
				Sum = 1000,
				BillingAccount = false
			}.Save();

			billing.OnMethod();

			pclient_post.Balance = -200m;
			pclient_post.Update();

			var service = new ClientService {
								  Client = client_Post,
								  Service = Service.GetByType(typeof(DebtWork)),
								  BeginWorkDate = DateTime.Now,
								  EndWorkDate = DateTime.Now.AddDays(1)
							  };
			//service.Save();
			client_Post.ClientServices.Add(service);
			//service.Service.Activate(service);
			service.Activate();
			//client_Post.PostponedPayment = DateTime.Now;
			/*client_Post.Disabled = false;
			client_Post.Update();*/

			billing.OnMethod();
			billing.Compute();


			client_simple.Refresh();
			client_Post.Refresh();
			Assert.IsFalse(client_Post.Disabled);
			Assert.IsFalse(client_simple.Disabled);

			SystemTime.Now = () => DateTime.Now.AddHours(25);

			billing.OnMethod();
			billing.Compute();

			Assert.That(WriteOff.Queryable.Where(w => w.Client == client_Post).Count(), Is.EqualTo(3));

			client_simple.Refresh();
			client_Post.Refresh();
			Assert.IsTrue(client_Post.Disabled);
			Assert.IsFalse(client_simple.Disabled);

			client_Post.Disabled = false;
			client_Post.Update();

			billing.Compute();

			client_simple.Refresh();
			client_Post.Refresh();
			Assert.IsTrue(client_Post.Disabled);
			Assert.IsFalse(client_simple.Disabled);


			client_Post.Refresh();
			//Assert.IsNull(client_Post.PostponedPayment);
		}


		[Test]
		public void LawyerPersonTest()
		{
			PrepareTest();
			//SystemTime.Reset();
			var lPerson = new LawyerPerson
			{
				Balance = 0,
				Tariff = 10000m,
			};
			lPerson.Save();
			var client = new Client {
				Disabled = false,
				Name = "TestLawyer",
				ShowBalanceWarningPage = false,
				LawyerPerson = lPerson
			};
			client.Save();

			for (int i = 0; i < DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month) * 2; i++)
			{
				billing.Compute();
			}
			Console.WriteLine(lPerson.Balance);
			Assert.That( -19999m, Is.GreaterThan(lPerson.Balance));
			Assert.That(-20000m, Is.LessThan(lPerson.Balance));
			billing.OnMethod();
			//client.Refresh();
			Assert.IsTrue(client.ShowBalanceWarningPage);
			Console.WriteLine(client.ShowBalanceWarningPage);
			lPerson.Balance += 1000;
			billing.OnMethod();
			Assert.IsTrue(!client.ShowBalanceWarningPage);
			Console.WriteLine(client.ShowBalanceWarningPage);
		}

		[Test]
		public void Write_off()
		{
			BaseBillingFixture.CreateAndSaveInternetSettings();
			PrepareTest();
			var client = CreateClient();
			//var client = CreateClient();
			client.PhysicalClient.Balance = Tariff.FindFirst().Price;
			client.Update();
			var interval = new Interval("15.01.2011", "15.02.2011");
			var dayCount = interval.GetInterval();
			interval.dtTo = DateTime.Parse("15.01.2011");
			for (var i = 0; i < dayCount; i++)
			{
				interval.dtTo = interval.dtTo.AddDays(1);
				SetClientDate(client, interval);
			}
			Assert.That(Math.Round(Convert.ToDecimal(client.PhysicalClient.Balance), 2), Is.LessThan(0.00));
			Console.WriteLine("End balance = " + Math.Round(Convert.ToDecimal(client.PhysicalClient.Balance), 2));
			var writeOffs = WriteOff.FindAll();
			//Assert.That(writeOffs.Length, Is.EqualTo(31));
			foreach (var writeOff in writeOffs)
			{
				Console.WriteLine(string.Format("id = {0} date = {1} sum = {2}", writeOff.Id, writeOff.WriteOffDate.ToShortDateString(), Math.Round(writeOff.WriteOffSum,2)));
			}
		}

		[Test]
		public void RealTest()
		{
			BaseBillingFixture.CreateAndSaveInternetSettings();
			var b = new MainBilling();
			b.Run();
			var thisSettings = InternetSettings.FindFirst().NextBillingDate;
			Assert.That(thisSettings.ToShortDateString(), Is.EqualTo(DateTime.Now.AddDays(1).ToShortDateString()));
		}

		[Test]
		public void Complex_tariff()
		{
			var tariff = new Tariff {
				Price = 200,
				FinalPriceInterval = 1,
				FinalPrice = 400,
			};
			var client = new Client {
				Name = "TestLawyer",
				BeginWork = DateTime.Now.AddMonths(-1),
				RatedPeriodDate = DateTime.Now,
				PhysicalClient = new PhysicalClients {
					Tariff = tariff,
					Balance = 1000
				}
			};
			client.Save();
			client.PhysicalClient.Save();
			tariff.Save();
			billing.Compute();
			var writeOffs = WriteOff.Queryable.Where(w => w.Client == client).ToList();
			Assert.That(writeOffs.Count, Is.EqualTo(1));
			Assert.That(writeOffs[0].WriteOffSum, Is.GreaterThan(10));
			WriteOff.DeleteAll();
			Client.DeleteAll();
		}


		/// <summary>
		/// Следить за состоянием client.DebtDays;
		/// </summary>
		[Test]
		public void IntervalTest()
		{
			BaseBillingFixture.CreateAndSaveInternetSettings();
			foreach (var writeOff in WriteOff.FindAll())
			{
				writeOff.Delete();
			}
			foreach (var clientse in Client.FindAll())
			{
				clientse.Delete();   
			}
			var client = CreateClient();

			var dates = new List<List<Interval>> {
				new List<Interval> {
					new Interval("30.09.2010", "30.10.2010"),
					new Interval("30.10.2010", "30.11.2010"),
					new Interval("30.11.2010", "30.12.2010"),
					new Interval("30.12.2010", "30.01.2011"),
					new Interval("30.01.2011", "28.02.2011"),
					new Interval("28.02.2011", "30.03.2011"),
					new Interval("30.03.2011", "30.04.2011"),
					new Interval("30.04.2011", "30.05.2011"),
					new Interval("30.05.2011", "30.06.2011"),
					new Interval("30.06.2011", "30.07.2011"),
				},
				new List<Interval> {
					new Interval("31.10.2010", "30.11.2010"),
					new Interval("30.11.2010", "31.12.2010"),
					new Interval("31.12.2010", "31.01.2011"),
					new Interval("31.01.2011", "28.02.2011"),
					new Interval("28.02.2011", "31.03.2011"),
					new Interval("31.03.2011", "30.04.2011"),
					new Interval("30.04.2011", "31.05.2011"),
					new Interval("31.05.2011", "30.06.2011"),
					new Interval("30.06.2011", "31.07.2011"),
				},
				new List<Interval> {
					new Interval("15.10.2010", "15.11.2010"),
					new Interval("15.11.2010", "15.12.2010"),
					new Interval("15.12.2010", "15.01.2011"),
					new Interval("15.01.2011", "15.02.2011"),
					new Interval("15.02.2011", "15.03.2011"),
					new Interval("15.03.2011", "15.04.2011"),
					new Interval("15.04.2011", "15.05.2011"),
					new Interval("15.05.2011", "15.06.2011"),
					new Interval("15.06.2011", "15.07.2011"),
				}
			};

			foreach (var date in dates)
			{
				client.DebtDays = 0;
				client.Update();
				for (int i = 0; i < date.Count-1; i++)
				{
					SetClientDate(client, date[i]);
					Assert.That(date[i+1].GetInterval(), Is.EqualTo(client.GetInterval()));
					Console.WriteLine(string.Format("Между датами {0} прошло {1} дней", date[i], date[i].GetInterval()));
				}
			}
			
			//client.DeleteAndFlush();
		}
	}
}
