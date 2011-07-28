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

            //CreateWriteOffs();
            ZeroTarif();
        }

        public static void ZeroTarif()
        {
            using (new SessionScope())
            {
                var client = Client.Find((uint) 1082);
                Console.WriteLine("Interver" + client.GetInterval());
                Console.WriteLine("Price" + client.GetPrice());
            }
            Console.ReadLine();
        }

        public static void CreateWriteOffs()
        {
            using (new SessionScope())
            {
                foreach (var source in Client.Queryable.Where(c=>c.Id < 87).ToList())
                {
                    var balance = source.PhysicalClient.Balance;
                    var writeOffs = source.WriteOffs.Sum(w => w.WriteOffSum);
                    var payments = Payment.Queryable.Where(p => p.Client == source).ToList().Sum(p => p.Sum);
                    new WriteOff {
                                     Client = source,
                                     WriteOffDate = new DateTime(2011, 03, 15),
                                     WriteOffSum = payments - balance - writeOffs
                                 }.Save();
                }
                Console.WriteLine("Complete");
            }
        }

        public void ChangeBalance()
        {
            using (new SessionScope())
            {
                var trueCount = 0;
                var falseCount = 0;
                /*new WriteOff {
                                 Client = Clients.Queryable.First(c => c.Id == 87),
                                 WriteOffDate = DateTime.Now,
                                 WriteOffSum = 700
                             }.Save();*/
                //foreach (var clientse in Clients.Queryable.Where(c => c.Id == 101 || c.Id == 1065).ToList())
                foreach (var clientse in Client.Queryable.Where(c => c.PhysicalClient != null && c.Id == 63 || c.Id == 105 || c.Id == 4).ToList())
                {
                    if (clientse.PhysicalClient.Tariff.Id != 10)
                    {
                        var firstWriteOff = clientse.WriteOffs.FirstOrDefault();
                        if (firstWriteOff != null)
                        {
                            clientse.RatedPeriodDate = firstWriteOff.WriteOffDate;
                        }
                        clientse.DebtDays = 0;
                        clientse.Update();

                        {
                            var phisCl = clientse.PhysicalClient;
                            phisCl.Tariff = Tariff.Find((uint)1);
                            phisCl.Update();
                        }
                        var writeOffs = clientse.WriteOffs.ToList();
                        foreach (var writeOff in clientse.WriteOffs)
                        {
                            if ((writeOff.WriteOffDate.Date >= new DateTime(2011, 6, 15) && clientse.Id == 4)
                                || (writeOff.WriteOffDate.Date >= new DateTime(2011, 5, 05) && clientse.Id == 63)
                                || (writeOff.WriteOffDate.Date >= new DateTime(2011, 5, 13) && clientse.Id == 105))
                            {
                                var phisCl = clientse.PhysicalClient;
                                phisCl.Tariff = Tariff.Find((uint)6);
                                phisCl.Update();
                            }
                            if (writeOff.WriteOffSum > 0)
                            {
                                var thisIndex = writeOffs.IndexOf(writeOff);
                                if ((writeOffs.Count < thisIndex + 1) &&
                                    (writeOff.WriteOffDate.Date != writeOffs[thisIndex + 1].WriteOffDate.AddDays(-1).Date))
                                {
                                    clientse.RatedPeriodDate = writeOff.WriteOffDate;
                                    clientse.Update();
                                }
                                if ((((DateTime)clientse.RatedPeriodDate).AddMonths(1).Date - SystemTime.Now().Date).Days <= 0)
                                {
                                    clientse.RatedPeriodDate = writeOff.WriteOffDate;
                                    clientse.Update();
                                }

                                SystemTime.Now = () => writeOff.WriteOffDate;
                                var writeOffSum = clientse.GetPrice() /
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
                new[] { typeof(Client).Assembly },
                configuration);
        }
    }
}
