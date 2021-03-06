﻿using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Controllers;
using InternetInterface.Models;

namespace Billing.Test.Integration
{
	public class Interval
	{
		public DateTime dtFrom;
		public DateTime dtTo;

		public Interval(string _dtFrom, string _dtTo)
		{
			dtTo = Convert.ToDateTime(_dtTo);
			dtFrom = Convert.ToDateTime(_dtFrom);
		}

		public decimal GetInterval()
		{
			return (dtTo - dtFrom).Days;
		}

		public override string ToString()
		{
			return string.Format("{0} и {1}", dtFrom.ToShortDateString(), dtTo.ToShortDateString());
		}
	}

	public class BaseBillingFixture
	{
		public static Client CreateAndSaveClient(string name, bool statusBlocked, decimal balance)
		{
			Settings settings;
			using (new SessionScope()) {
				settings = Settings();
			}
			Status newClientStatus = Status.Queryable.AsQueryable().First(s=>s.Id == (int) StatusType.BlockedAndNoConnected);
			var phisicalClient = CreatePhisicalClient(statusBlocked, balance);
			phisicalClient.Save();

			var client = new Client(phisicalClient, settings) {
				Disabled = false,
				Status = newClientStatus,
				Sale = 0,
				DebtDays = 0,
				FreeBlockDays = 0,
				PercentBalance = 0,
				Name = name,
				BeginWork = DateTime.Now,
				RatedPeriodDate = DateTime.Now,
				YearCycleDate = DateTime.Now
			};
			client.PhysicalClient.ConnectSum = 0;
			client.Internet.ActivatedByUser = true;
			//тк некоторые тесты не вызывают метод активации
			foreach (var service in client.ClientServices)
				service.TryActivate();

			return client;
		}

		public static Settings Settings()
		{
			return ArHelper.WithSession(s => new Settings(s));
		}

		public static PhysicalClient CreatePhisicalClient(bool statusBlocked, decimal balance)
		{
			var tariff = CreateTariff((int)balance);
			ActiveRecordMediator.Save(tariff);
			return new PhysicalClient {
				Name = "TestPhisicalClient",
				Surname = "Test",
				Patronymic = "Testovich",
				Balance = balance,
				Tariff = tariff,
				PassportNumber = "123456"
			};
		}

		public static Tariff CreateTariff(int balanceForTariff)
		{
			return new Tariff {
				Name = "testTariff",
				Price = balanceForTariff,
				Description = "testTariff"
			};
		}

		public static void CreatePayment(decimal sum)
		{
			new Payment {
				BillingAccount = false,
				Client = Client.Queryable.AsQueryable().Count(c => c.Name == "testblockedClient") != 0 ? 
					Client.Queryable.AsQueryable().First(c => c.Name == "testblockedClient") : 
					Client.FindFirst(),
				PaidOn = DateTime.Now,
				RecievedOn = DateTime.Now,
				Sum = sum
			}.SaveAndFlush();
		}

		public static void CreateAndSaveInternetSettings()
		{
			new InternetSettings {
				NextBillingDate = DateTime.Now.AddHours(-1)
			}.SaveAndFlush();
		}
	}
}
