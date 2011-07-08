using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.Tools;
using InternetInterface.Models;
using Environment = NHibernate.Cfg.Environment;

namespace BananceChanger
{
    class Program
    {
        static void Main(string[] args)
        {
            Init();

            using (new SessionScope())
            {
                var trueCount = 0;
                var falseCount = 0;
                new WriteOff {
                                 Client = Clients.Queryable.First(c => c.Id == 87),
                                 WriteOffDate = DateTime.Now,
                                 WriteOffSum = 700
                             }.Save();
                foreach (var clientse in Clients.Queryable.Where(c => c.PhysicalClient != null && c.Id != 1163 && c.Id != 105 && c.Id != 619).ToList())
                {
                    if (clientse.PhysicalClient.Tariff.Id != 10 && clientse.Id >= 87)
                    {
                        var firstWriteOff = clientse.WriteOffs.FirstOrDefault();
                        if (firstWriteOff != null)
                        {
                            clientse.RatedPeriodDate = firstWriteOff.WriteOffDate;
                        }
                        clientse.DebtDays = 0;
                        clientse.Update();
                        var writeOffs = clientse.WriteOffs.ToList();
                        foreach (var writeOff in clientse.WriteOffs)
                        {
                            if (writeOff.WriteOffSum > 0)
                            {
                                var thisIndex = writeOffs.IndexOf(writeOff);
                                if ((writeOffs.Count < thisIndex + 1) &&
                                    (writeOff.WriteOffDate != writeOffs[thisIndex + 1].WriteOffDate.AddDays(-1)))
                                {
                                    clientse.RatedPeriodDate = writeOff.WriteOffDate;
                                    clientse.Update();
                                }
                                if ((((DateTime)clientse.RatedPeriodDate).AddMonths(1).Date - SystemTime.Now().Date).Days == 0)
                                {
                                    clientse.RatedPeriodDate = writeOff.WriteOffDate;
                                    clientse.Update();
                                }

                                SystemTime.Now = () => writeOff.WriteOffDate;
                                var writeOffSum = clientse.PhysicalClient.Tariff.GetPrice(clientse)/
                                                  clientse.GetInterval();
                                if (writeOff.WriteOffSum.ToString("0.00") == writeOffSum.ToString("0.00"))
                                {
                                    Console.WriteLine(
                                        string.Format(
                                            "Правильно по клиенту {0} дата списания {1} должно быть {2} есть {3}",
                                            clientse.Id, SystemTime.Now().ToShortDateString(),
                                            writeOffSum.ToString("0.00"), writeOff.WriteOffSum.ToString("0.00")));
                                    trueCount++;
                                }
                                else
                                {
                                    if (writeOff.WriteOffSum >= 700)
                                        writeOff.WriteOffSum = 700 + writeOffSum;
                                    else
                                        writeOff.WriteOffSum = writeOffSum;
                                    writeOff.Update();
                                    falseCount++;
                                }
                            }
                            //Console.WriteLine(writeOff.WriteOffSum);
                        }
                    }
                }
                Console.WriteLine("Правильных списаний " + trueCount);
                Console.WriteLine("Ошибочных списаний " + falseCount);
            }
            Console.ReadLine();
        }

        private static void Init()
        {
            var configuration = new InPlaceConfigurationSource();
            //configuration.PluralizeTableNames = true;
            configuration.Add(typeof(ActiveRecordBase),
                new Dictionary<string, string>
				{
					{Environment.Dialect, "NHibernate.Dialect.MySQLDialect"},
					{Environment.ConnectionDriver, "NHibernate.Driver.MySqlDataDriver"},
					{Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
					{Environment.ConnectionStringName, "DB"},
					{Environment.ProxyFactoryFactoryClass, "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle" },
					{Environment.Hbm2ddlKeyWords, "none"},
				});
            ActiveRecordStarter.Initialize(
                new[] { typeof(Clients).Assembly },
                configuration);
        }
    }
}
