using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.Tools;
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

	public static class Error
	{
		public static string GerError(Exception ex)
		{
			return ex.Message + "\n \r" + ex.StackTrace + "\n \r" + ex.InnerException + "\n \r" + ex.Source + "\n \r" + ex.Data;
		}
	}

	public class MainBilling
	{
		public DateTime DtNow;
		private readonly ILog _log = LogManager.GetLogger(typeof(MainBilling));

		public MainBilling()
		{
			try
			{
				XmlConfigurator.Configure();
				InitActiveRecord();
			}
			catch (Exception ex)
			{
				_log.Error(Error.GerError(ex));
			}
		}

		public static void InitActiveRecord()
		{
			if (!ActiveRecordStarter.IsInitialized)
			{
				var configuration = new InPlaceConfigurationSource();
				configuration.PluralizeTableNames = true;
				configuration.Add(typeof (ActiveRecordBase),
				                  new Dictionary<string, string>
				                  	{
				                  		{Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
				                  		{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
				                  		{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
				                  		{Environment.ConnectionStringName, "DB"},
										{Environment.ProxyFactoryFactoryClass,
				                  			"NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle"
				                  			},
				                  		{Environment.Hbm2ddlKeyWords, "none"},
				                  	});
				ActiveRecordStarter.Initialize( new [] {typeof (Clients).Assembly, typeof(InternetSettings).Assembly},
				                               configuration);
			}
		}

		public void UseSession(Action func)
		{
			using (var session = new TransactionScope(OnDispose.Rollback))
			{
				func();
				session.VoteCommit();
			}
		}


		public void On()
		{
			try
			{
				UseSession(() =>
				           	{
								var newClients = Clients.FindAll(DetachedCriteria.For(typeof(Clients))
									.Add(Restrictions.Eq("SayBillingIsNewClient", true))
									.Add(Restrictions.IsNotNull("PhisicalClient")));
				           		foreach (var newClient in newClients)
				           		{
				           			var phisCl = newClient.PhisicalClient;
				           			var connectSum = PaymentForConnect.FindAllByProperty("ClientId", phisCl).First().Summ;
				           			phisCl.Balance -= Convert.ToDecimal(connectSum);
									phisCl.UpdateAndFlush();
				           			newClient.SayBillingIsNewClient = false;
									newClient.UpdateAndFlush();
									new WriteOff
									{
										Client = newClient,
										WriteOffDate = SystemTime.Now(),
										WriteOffSum = Convert.ToDecimal(connectSum)
									}.SaveAndFlush();

				           		}
				           		var newPayments = Payment.FindAll(DetachedCriteria.For(typeof (Payment))
				           		                                  	.Add(Restrictions.Eq("BillingAccount", false)));
								foreach (var newPayment in newPayments)
								{
									var updateClient = PhisicalClients.Find(newPayment.Client.Id);
									updateClient.Balance += Convert.ToDecimal(newPayment.Sum);
									updateClient.UpdateAndFlush();
									newPayment.BillingAccount = true;
									newPayment.UpdateAndFlush();
								}
				           		var clients = Clients.FindAll(DetachedCriteria.For(typeof (Clients))
									.CreateAlias("PhisicalClient", "PC", JoinType.InnerJoin)
									.CreateAlias("PC.Status","S", JoinType.InnerJoin)
				           		                              	.Add(Restrictions.IsNotNull("PhisicalClient"))
																.Add(Restrictions.Eq("S.Blocked", true))
																.Add(Restrictions.Eq("PC.AutoUnblocked", true))
																.Add(Restrictions.Ge("PC.Balance" , 0m)));
								foreach (var client in clients)
								{
									var phisicalClient = client.PhisicalClient;
									phisicalClient.Status = Status.Find((uint) StatusType.Worked);
									//client.FirstLease = true;
									client.ShowBalanceWarningPage = false;
									client.UpdateAndFlush();
									phisicalClient.UpdateAndFlush();
								}
				           	});
			}
			catch (Exception ex)
			{
				_log.Error(Error.GerError(ex));
			}
		}

		public void Run()
		{
			try
			{
				var thisDateMax = InternetSettings.FindFirst().NextBillingDate;
				if ((thisDateMax - DateTime.Now).TotalMinutes <= 0)
				UseSession(Compute);
			}
			catch (Exception ex)
			{
				_log.Error(Error.GerError(ex));
			}
		}

		public void Compute()
		{
			var clients = Clients.FindAll(DetachedCriteria.For(typeof (Clients))
			                              	.Add(Restrictions.IsNotNull("PhisicalClient")));
			foreach (var client in clients)
			{
				var phisicalClient = client.PhisicalClient;
				var balance = Convert.ToDecimal(phisicalClient.Balance);
				if ((balance >= 0) &&
					(!client.Disabled) &&
					(client.RatedPeriodDate != DateTime.MinValue))
				{
					DtNow = SystemTime.Now();

					if ((client.RatedPeriodDate.AddMonths(1) - DtNow).Days == -client.DebtDays)
					{
						var dtFrom = client.RatedPeriodDate;
						var dtTo = DtNow;
						client.DebtDays += dtFrom.Day - dtTo.Day;
						var thisMonth = DtNow.Month;
						client.RatedPeriodDate = DtNow.AddDays(client.DebtDays);
						while (client.RatedPeriodDate.Month != thisMonth)
						{
							client.RatedPeriodDate = client.RatedPeriodDate.AddDays(-1);
						}
						client.UpdateAndFlush();
					}
				}

				//if (phisicalClient.Status.Blocked == false)
				if (!client.Disabled)
				{
					if (client.RatedPeriodDate != DateTime.MinValue)
					{
						decimal toDt = client.GetInterval();
						var dec = phisicalClient.Tariff.Price / toDt;
						phisicalClient.Balance = (balance - dec);
						phisicalClient.UpdateAndFlush();
						var bufBal = phisicalClient.Balance;
						client.ShowBalanceWarningPage = bufBal - dec < 0;
						client.UpdateAndFlush();
						new WriteOff
							{
								Client = client,
								WriteOffDate = SystemTime.Now(),
								WriteOffSum = dec
							}.SaveAndFlush();
					}
				}

				// Тут со временем должно устанавливаться дисейбл
				if ((phisicalClient.Balance < 0) &&
				    (phisicalClient.Status.Blocked == false))
				{
					phisicalClient.Status = Status.Find((uint) StatusType.NoWorked);
					phisicalClient.UpdateAndFlush();
				}
			}
			var thisDateMax = InternetSettings.FindFirst();
			thisDateMax.NextBillingDate = DateTime.Now.AddDays(1);
			thisDateMax.UpdateAndFlush();
		}
	}
}
