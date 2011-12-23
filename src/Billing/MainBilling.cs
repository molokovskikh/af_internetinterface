﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Castle.ActiveRecord.Framework.Internal.EventListener;
using Common.Tools;
using InternetInterface.Helpers;
using InternetInterface.Models;
using log4net;
using log4net.Config;
using NHibernate.Cfg;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using Microsoft.Win32;
using Environment = NHibernate.Cfg.Environment;

namespace Billing
{

	public class MainBilling
	{
		public const int FreeDaysVoluntaryBlockin = 28;

		private readonly ILog _log = LogManager.GetLogger(typeof (MainBilling));
		private Mutex _mutex = new Mutex();

		public MainBilling()
		{
			try {
				XmlConfigurator.Configure();
				InitActiveRecord();
			}
			catch (Exception ex) {
				_log.Error("Ошибка к конструкторе" ,ex);
			}
		}

		public static void InitActiveRecord()
		{
			if (!ActiveRecordStarter.IsInitialized) {
				var configuration = new InPlaceConfigurationSource();
				configuration.PluralizeTableNames = true;
				configuration.Add(typeof (ActiveRecordBase),
				                  new Dictionary<string, string> {
				                  	{Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
				                  	{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
				                  	{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
				                  	{Environment.ConnectionStringName, "DB"},
				                  	{
				                  	Environment.ProxyFactoryFactoryClass,
				                  	"NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle"
				                  	},
				                  	{Environment.Hbm2ddlKeyWords, "none"}
				                  });
				ActiveRecordStarter.EventListenerComponentRegistrationHook += RemoverListner.Make;
				ActiveRecordStarter.Initialize(new[] {typeof (Client).Assembly}, configuration);
			}
		}

		private int DayInCurrentYear()
		{
			return DateTime.IsLeapYear(SystemTime.Now().Year) ? 366 : 365;
		}

		public void UseSession(Action func)
		{
			using (var session = new TransactionScope(OnDispose.Rollback)) {
				func();
				session.VoteCommit();
			}
		}

		public void On()
		{
			_mutex.WaitOne();
			try {
				UseSession(OnMethod);
			}
			catch (Exception ex) {
				_log.Error("Ошибка в методе On" ,ex);
			}
			_mutex.ReleaseMutex();
		}


		public void Run()
		{
			_mutex.WaitOne();
			try {
				var thisDateMax = InternetSettings.FindFirst().NextBillingDate;
				var now = SystemTime.Now();
				if ((thisDateMax - now).TotalMinutes <= 0) {
					UseSession(Compute);
					if (now.Hour < 22) {
						var billingTime = InternetSettings.FindFirst();
						billingTime.NextBillingDate = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0);
						billingTime.Save();
					}
				}
			}
			catch (Exception ex) {
				_log.Error("Ошибка запуска процедуры Run Billing", ex);
			}
			_mutex.ReleaseMutex();
		}

		public void OnMethod()
		{
			foreach (var cserv in ClientService.Queryable.Where(c => !c.Activated && !c.Diactivated).ToList()) {
				cserv.Activate();
			}
			var newClients = Client.FindAll(DetachedCriteria.For(typeof (Client))
			                                	.CreateAlias("PhysicalClient", "PC", JoinType.InnerJoin)
			                                	.Add(Restrictions.Eq("PC.ConnectionPaid", false))
			                                	.Add(Restrictions.IsNotNull("BeginWork"))
			                                	.Add(Restrictions.IsNotNull("PhysicalClient")));
			foreach (var newClient in newClients) {
				var phisCl = newClient.PhysicalClient;
				phisCl.Balance -= phisCl.ConnectSum;
				phisCl.ConnectionPaid = true;
				phisCl.UpdateAndFlush();
				newClient.UpdateAndFlush();
				if (phisCl.ConnectSum > 0)
				new WriteOff {
					Client = newClient,
					WriteOffDate = SystemTime.Now(),
					WriteOffSum = phisCl.ConnectSum
				}.SaveAndFlush();
			}
			var newPayments = Payment.FindAll(DetachedCriteria.For(typeof (Payment))
			                                  	.Add(Restrictions.Eq("BillingAccount", false)));
			foreach (var newPayment in newPayments) {
				var updateClient = newPayment.Client;
				var physicalClient = updateClient.PhysicalClient;
				var lawyerClient = updateClient.LawyerPerson;
				var havePayment = updateClient.HavePayment;
				if (physicalClient != null) {
					physicalClient.Balance += Convert.ToDecimal(newPayment.Sum);
					physicalClient.UpdateAndFlush();
					newPayment.BillingAccount = true;
					newPayment.Update();
					if (!havePayment) {
						updateClient.AutoUnblocked = true;
					}
					else {
						updateClient.PercentBalance = 0.8m;
					}
					if (updateClient.RatedPeriodDate != null)
						if (physicalClient.Balance >= updateClient.GetPriceForTariff()) {
							updateClient.ShowBalanceWarningPage = false;
						}
					if (updateClient.ClientServices != null)
						for (int i = updateClient.ClientServices.Count - 1; i > -1; i--) {
							var cserv = updateClient.ClientServices[i];
							cserv.PaymentClient();
						}
					updateClient.Update();
				}
				if (lawyerClient != null) {
					lawyerClient.Balance += Convert.ToDecimal(newPayment.Sum);
					lawyerClient.UpdateAndFlush();
					newPayment.BillingAccount = true;
					newPayment.UpdateAndFlush();
				}
			}

			var userWriteOffs = UserWriteOff.Queryable.Where(u => !u.BillingAccount && u.Client != null).ToList();
			foreach (var userWriteOff in userWriteOffs) {
				var client = userWriteOff.Client;
				if (client.PhysicalClient != null) {
					var physicalClient = client.PhysicalClient;
					physicalClient.Balance -= userWriteOff.Sum;
					physicalClient.Update();
				}
				if (client.LawyerPerson != null) {
					var lawyerPerson = client.LawyerPerson;
					lawyerPerson.Balance -= userWriteOff.Sum;
					lawyerPerson.Update();
				}
				userWriteOff.BillingAccount = true;
				userWriteOff.Update();
			}

			var clients =
				Client.Queryable.Where(
					c => c.PhysicalClient != null && c.Disabled && c.AutoUnblocked).ToList().Where(
						c => c.PhysicalClient.Balance >= c.GetPriceForTariff()*c.PercentBalance).ToList();
			foreach (var client in clients) {
				client.Status = Status.Find((uint) StatusType.Worked);
				client.RatedPeriodDate = null;
				client.DebtDays = 0;
				client.ShowBalanceWarningPage = false;
				client.Disabled = false;
				client.UpdateAndFlush();
			}
			var lawyerPersons = Client.Queryable.Where(c => c.LawyerPerson != null);
			foreach (var client in lawyerPersons) {
				var person = client.LawyerPerson;
				if (person.Balance < -(person.Tariff*1.9m)) {
					client.ShowBalanceWarningPage = true;
				}
				else {
					client.ShowBalanceWarningPage = false;
				}
				client.UpdateAndFlush();
			}
			foreach (var cserv in ClientService.FindAll()) {
				cserv.Diactivate();
			}
		}

		public void Compute()
		{
			var clients = Client.Queryable.Where(c => c.PhysicalClient != null).ToList();
			foreach (var client in clients) {
				var phisicalClient = client.PhysicalClient;
				var balance = Convert.ToDecimal(phisicalClient.Balance);
				if ((balance >= 0) &&
				    (!client.Disabled) &&
				    (client.RatedPeriodDate != DateTime.MinValue) && (client.RatedPeriodDate != null)) {
					var DtNow = SystemTime.Now();

					if ((((DateTime) client.RatedPeriodDate).AddMonths(1).Date - DtNow.Date).Days == -client.DebtDays) {
						var dtFrom = (DateTime) client.RatedPeriodDate;
						var dtTo = DtNow;
						client.DebtDays += dtFrom.Day - dtTo.Day;
						var thisMonth = DtNow.Month;
						client.RatedPeriodDate = DtNow.AddDays(client.DebtDays);
						while (((DateTime) client.RatedPeriodDate).Month != thisMonth) {
							client.RatedPeriodDate = ((DateTime) client.RatedPeriodDate).AddDays(-1);
						}
						client.UpdateAndFlush();
					}
				}

				if (client.GetPrice() > 0 && !client.PaidDay) {
					if (client.RatedPeriodDate != DateTime.MinValue && client.RatedPeriodDate != null) {
						var toDt = client.GetInterval();
						var price = client.GetPrice();
						var dec = price/toDt;
						phisicalClient.Balance -= dec;
						phisicalClient.UpdateAndFlush();
						var bufBal = phisicalClient.Balance;
						client.ShowBalanceWarningPage = bufBal - dec < 0;
						client.UpdateAndFlush();
						if (dec > 0)
						new WriteOff {
							Client = client,
							WriteOffDate = SystemTime.Now(),
							WriteOffSum = dec
						}.SaveAndFlush();
					}
				}
				if (client.CanBlock()) {
					client.Disabled = true;
					client.Status = Status.Find((uint) StatusType.NoWorked);
				}
				if (client.PaidDay) {
					client.PaidDay = false;
				}
				if (client.YearCycleDate == null || (SystemTime.Now().Date - client.YearCycleDate.Value.Date).TotalDays >= DayInCurrentYear()) {
					client.FreeBlockDays = FreeDaysVoluntaryBlockin;
					client.YearCycleDate = SystemTime.Now();
				}
				client.Update();
			}
			var lawyerclients = Client.Queryable.Where(c => c.LawyerPerson != null && c.LawyerPerson.Tariff != null).ToList();
			foreach (var client in lawyerclients) {
				var person = client.LawyerPerson;
				if (!client.Disabled) {
					var thisDate = SystemTime.Now();
					decimal spis = person.Tariff.Value / DateTime.DaysInMonth(thisDate.Year, thisDate.Month);
					person.Balance -= spis;
					person.UpdateAndFlush();
					if (spis > 0)
					new WriteOff {
						Client = client,
						WriteOffDate = SystemTime.Now(),
						WriteOffSum = spis
					}.SaveAndFlush();
				}
			}
			var thisDateMax = InternetSettings.FindFirst();
			thisDateMax.NextBillingDate = SystemTime.Now().AddDays(1).Date.AddHours(22);
			thisDateMax.UpdateAndFlush();
			//Console.WriteLine(ClientService.Queryable.Count());
			ClientService.Queryable.ToList().Each(cs => cs.WriteOff());
		}
	}
}
