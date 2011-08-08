﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
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

    public static class Error
    {
        public static string GerError(Exception ex)
        {
            return ex.Message + "\n \r" + ex.StackTrace + "\n \r" + ex.InnerException + "\n \r" + ex.Source + "\n \r" +
                   ex.Data;
        }
    }

    public class MainBilling
    {
        public DateTime DtNow;
        private readonly ILog _log = LogManager.GetLogger(typeof (MainBilling));

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
                                  new Dictionary<string, string> {
                                                                     {
                                                                         Environment.Dialect,
                                                                         "NHibernate.Dialect.MySQLDialect"
                                                                         },
                                                                     {
                                                                         Environment.ConnectionDriver,
                                                                         "NHibernate.Driver.MySqlDataDriver"
                                                                         },
                                                                     {
                                                                         Environment.ConnectionProvider,
                                                                         "NHibernate.Connection.DriverConnectionProvider"
                                                                         },
                                                                     {Environment.ConnectionStringName, "DB"},
                                                                     {
                                                                         Environment.ProxyFactoryFactoryClass,
                                                                         "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle"
                                                                         },
                                                                     {Environment.Hbm2ddlKeyWords, "none"},
                                                                 });
                ActiveRecordStarter.EventListenerComponentRegistrationHook += RemoverListner.Make;
                ActiveRecordStarter.Initialize(new[] {typeof (Client).Assembly},
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
                UseSession(OnMethod);
            }
            catch (Exception ex)
            {
                _log.Error(Error.GerError(ex));
            }
        }

        public void OnMethod()
        {
            foreach (var cserv in ClientService.Queryable.Where(c => !c.Activated).ToList())
            {
                cserv.Activate();
            }
            var newClients = Client.FindAll(DetachedCriteria.For(typeof (Client))
                                                .CreateAlias("PhysicalClient", "PC", JoinType.InnerJoin)
                                                .Add(Restrictions.Eq("PC.ConnectionPaid", false))
                                                .Add(Restrictions.IsNotNull("BeginWork"))
                                                .Add(Restrictions.IsNotNull("PhysicalClient")));
            foreach (var newClient in newClients)
            {
                var phisCl = newClient.PhysicalClient;
                phisCl.Balance -= phisCl.ConnectSum;
                phisCl.ConnectionPaid = true;
                phisCl.UpdateAndFlush();
                newClient.UpdateAndFlush();
                new WriteOff {
                                 Client = newClient,
                                 WriteOffDate = SystemTime.Now(),
                                 WriteOffSum = phisCl.ConnectSum
                             }.SaveAndFlush();

            }
            var newPayments = Payment.FindAll(DetachedCriteria.For(typeof (Payment))
                                                  .Add(Restrictions.Eq("BillingAccount", false)));
            foreach (var newPayment in newPayments)
            {
                var updateClient = newPayment.Client;
                var physicalClient = updateClient.PhysicalClient;
                var lawyerClient = updateClient.LawyerPerson;
                if (physicalClient != null)
                {
                    physicalClient.Balance += Convert.ToDecimal(newPayment.Sum);
                    physicalClient.UpdateAndFlush();
                    newPayment.BillingAccount = true;
                    newPayment.UpdateAndFlush();
                    var bufBal = physicalClient.Balance;
                    if (updateClient.RatedPeriodDate != null)
                        if (bufBal - updateClient.GetPrice()/updateClient.GetInterval() > 0)
                        {
                            updateClient.ShowBalanceWarningPage = false;
                            updateClient.Update();
                        }
                    if (updateClient.ClientServices != null)
                        for (int i = updateClient.ClientServices.Count - 1; i > -1; i--)
                        {
                            var cserv = updateClient.ClientServices[i];
                            cserv.PaymentClient();
                        }
                }
                if (lawyerClient != null)
                {
                    lawyerClient.Balance += Convert.ToDecimal(newPayment.Sum);
                    lawyerClient.UpdateAndFlush();
                    newPayment.BillingAccount = true;
                    newPayment.UpdateAndFlush();
                }
            }
            var clients = Client.FindAll(DetachedCriteria.For(typeof (Client))
                                             .CreateAlias("PhysicalClient", "PC", JoinType.InnerJoin)
                                             .Add(Restrictions.IsNotNull("PhysicalClient"))
                                             .Add(Restrictions.Eq("Disabled", true))
                                             .Add(Restrictions.Eq("AutoUnblocked", true))
                                             .Add(Restrictions.Ge("PC.Balance", 0m)));
            foreach (var client in clients)
            {
                client.Status = Status.Find((uint) StatusType.Worked);
                client.RatedPeriodDate = null;
                client.DebtDays = 0;
                client.ShowBalanceWarningPage = false;
                client.Disabled = false;
                client.UpdateAndFlush();
            }
            var lawyerPersons = Client.Queryable.Where(c => c.LawyerPerson != null);
            foreach (var client in lawyerPersons)
            {
                var person = client.LawyerPerson;
                if (person.Balance < -(person.Tariff*1.9m))
                {
                    client.ShowBalanceWarningPage = true;
                }
                else
                {
                    client.ShowBalanceWarningPage = false;
                }
                client.UpdateAndFlush();
            }
            foreach (var cserv in ClientService.FindAll())
            {
                cserv.Diactivate();
            }
        }


        public void Run()
        {
            try
            {
                var thisDateMax = InternetSettings.FindFirst().NextBillingDate;
                if ((thisDateMax - DateTime.Now).TotalMinutes <= 0)
                {
                    UseSession(Compute);
                    if (DateTime.Now.Hour < 22)
                    {
                        var billingTime = InternetSettings.FindFirst();
                        billingTime.NextBillingDate = new DateTime(thisDateMax.Year, thisDateMax.Month, thisDateMax.Day,
                                                                   22, 0, 0);
                        billingTime.SaveAndFlush();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(Error.GerError(ex));
            }
        }

        public void Compute()
        {
            var clients = Client.Queryable.Where(c => c.PhysicalClient != null).ToList();
            foreach (var client in clients)
            {
                var phisicalClient = client.PhysicalClient;
                var balance = Convert.ToDecimal(phisicalClient.Balance);
                if ((balance >= 0) &&
                    (!client.Disabled) &&
                    (client.RatedPeriodDate != DateTime.MinValue) && (client.RatedPeriodDate != null))
                {
                    DtNow = SystemTime.Now();

                    if ((((DateTime) client.RatedPeriodDate).AddMonths(1).Date - DtNow.Date).Days == -client.DebtDays)
                    {
                        var dtFrom = (DateTime) client.RatedPeriodDate;
                        var dtTo = DtNow;
                        client.DebtDays += dtFrom.Day - dtTo.Day;
                        var thisMonth = DtNow.Month;
                        client.RatedPeriodDate = DtNow.AddDays(client.DebtDays);
                        while (((DateTime) client.RatedPeriodDate).Month != thisMonth)
                        {
                            client.RatedPeriodDate = ((DateTime) client.RatedPeriodDate).AddDays(-1);
                        }
                        client.UpdateAndFlush();
                    }
                }

                if (client.GetPrice() > 0)
                {
                    if (client.RatedPeriodDate != DateTime.MinValue && client.RatedPeriodDate != null)
                    {
                        var toDt = client.GetInterval();
                        var price = client.GetPrice();
                        var dec = price/toDt;
                        phisicalClient.Balance -= dec;
                        phisicalClient.UpdateAndFlush();
                        var bufBal = phisicalClient.Balance;
                        client.ShowBalanceWarningPage = bufBal - dec < 0;
                        client.UpdateAndFlush();
                        new WriteOff {
                                         Client = client,
                                         WriteOffDate = SystemTime.Now(),
                                         WriteOffSum = dec
                                     }.SaveAndFlush();
                    }
                }
                if (client.CanBlock())
                {
                    client.Disabled = true;
                    client.Status = Status.Find((uint) StatusType.NoWorked);
                    client.UpdateAndFlush();
                }
            }
            var lawyerclients = Client.Queryable.Where(c => c.LawyerPerson != null).ToList();
            foreach (var client in lawyerclients)
            {
                var person = client.LawyerPerson;
                if (!client.Disabled)
                {
                    var thisDate = SystemTime.Now();
                    decimal spis = person.Tariff/DateTime.DaysInMonth(thisDate.Year, thisDate.Month);
                    person.Balance -= spis;
                    person.UpdateAndFlush();
                    new WriteOff {
                                     Client = client,
                                     WriteOffDate = SystemTime.Now(),
                                     WriteOffSum = spis
                                 }.SaveAndFlush();
                }
            }
            var thisDateMax = InternetSettings.FindFirst();
            thisDateMax.NextBillingDate = DateTime.Now.AddDays(1);
            thisDateMax.UpdateAndFlush();
        }
    }
}
