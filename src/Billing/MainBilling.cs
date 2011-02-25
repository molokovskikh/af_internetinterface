using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
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
				_log.Error(ex.Message + "\n \r" + ex.StackTrace + "\n \r" + ex.InnerException + "\n \r" + ex.Source);
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
				ActiveRecordStarter.Initialize(typeof (Clients).Assembly,
				                               configuration);
			}
		}

		public void UseSession(Func<object> func)
		{
			using (new SessionScope())
			{
				func();
			}
		}


		public void On()
		{
			try
			{
				UseSession(() =>
				           	{
				           		var clients = Clients.FindAll(DetachedCriteria.For(typeof (Clients))
				           		                              	.Add(Restrictions.IsNotNull("PhisicalClient"))
																.Add(Restrictions.IsNotNull("Balance")));
				           		foreach (var client in clients)
				           		{
				           			var phisicalClient = client.PhisicalClient;
				           			var balance = Convert.ToDecimal(phisicalClient.Balance);
				           			if ((balance >= 0) &&
				           			    (phisicalClient.Status.Blocked) &&
				           			    (phisicalClient.AutoUnblocked))
				           			{
				           				phisicalClient.Status = Status.Find((uint) StatusType.Worked);
				           				client.FirstLease = true;
				           				client.UpdateAndFlush();
				           				phisicalClient.UpdateAndFlush();
				           			}
				           		}
				           		return new object();
				           	});
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message + "\n \r" + ex.StackTrace + "\n \r" + ex.InnerException + "\n \r" + ex.Source);
			}
		}

		public void Run()
		{
			try
			{
				UseSession(() =>
				           	{
				           		Compute();
				           		return new object();
				           	});
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message + "\n \r" + ex.StackTrace + "\n \r" + ex.InnerException + "\n \r" + ex.Source);
			}
		}

		public void Compute()
		{
			//InitActiveRecord();
			/*var clients = Clients.FindAll(DetachedCriteria.For(typeof (Clients))
											.CreateAlias("PhisicalClient", "PC", JoinType.InnerJoin)
											.CreateAlias("PC.Status", "S", JoinType.InnerJoin)
											.Add(Restrictions.Eq("S.Blocked", false)));*/
			var clients = Clients.FindAll(DetachedCriteria.For(typeof (Clients))
			                              	.Add(Restrictions.IsNotNull("PhisicalClient")));
			foreach (var client in clients)
			{
				var phisicalClient = client.PhisicalClient;
				var balance = Convert.ToDecimal(phisicalClient.Balance);
				if (phisicalClient.Status.Blocked == false)
				{
					//decimal toDt = (client.RatedPeriodDate.AddMonths(1) - client.RatedPeriodDate).Days + client.DebtDays;
					decimal toDt = client.GetInterval();
#if DEBUG
					Console.WriteLine(string.Format("{0} - {2} расчетный интервал {1} дней ID - {3}",
					                                client.RatedPeriodDate.ToShortDateString(), toDt,
					                                client.RatedPeriodDate.AddMonths(1).ToShortDateString(),
					                                client.Id));
#endif
					var dec = phisicalClient.Tariff.Price/toDt;
					phisicalClient.Balance = (balance - dec).ToString();
					phisicalClient.UpdateAndFlush();

					if (client.RatedPeriodDate != DateTime.MinValue)
					{
#if !DEBUG
							DtNow = DateTime.Now;
#endif
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
				}
				if ((balance < 0) &&
				    (phisicalClient.Status.Blocked == false))
				{
					phisicalClient.Status = Status.Find((uint) StatusType.NoWorked);
					phisicalClient.UpdateAndFlush();
				}
				if ((balance >= 0) &&
				    (phisicalClient.Status.Blocked) &&
				    (phisicalClient.AutoUnblocked))
				{
					phisicalClient.Status = Status.Find((uint) StatusType.Worked);
					client.FirstLease = true;
					client.UpdateAndFlush();
					phisicalClient.UpdateAndFlush();
				}
			}
		}
	}
}
