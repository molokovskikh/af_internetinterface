using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Common.Tools;
using InternetInterface.Helpers;
using InternetInterface.Models;
using Test.Support.Helpers;
using Environment = NHibernate.Cfg.Environment;

namespace BananceChanger
{
	class Program
	{
		static void Main(string[] args)
		{
			Init();

			//CreateWriteOffs();
			//ZeroTarif();
			//IntervalTariffs();
			//diactivateClient();
			TheeDaysTrial();
		}

		//Начисляет бонусные 3 дня клиентам, которые вчера работали
		public static void TheeDaysTrial()
		{
			using (var session = new TransactionScope(OnDispose.Rollback)) {
				var whoRealWork = Client.Queryable.Where(c =>
				                                         !c.Disabled &&
				                                         c.PhysicalClient != null &&
				                                         c.BeginWork != null &&
				                                         c.RatedPeriodDate != null).ToList();
				//!!!!!!!!!!!!!!!! ИЗМЕНИ ПРИ SQL2!!!!!!!!!!!1
				var lastDay = new DateTime(2011, 11, 29, 00, 00, 00);
				var clientWhoBlocked = ClientInternetLogs.Queryable.Where(c =>
				                                                          c.Disabled &&
				                                                          c.OperatorName == "InternetBilling" &&
				                                                          c.LogTime >= lastDay).ToList().Select(c => c.Client).ToList();
				whoRealWork.AddRange(clientWhoBlocked);
				foreach (var client in whoRealWork) {
					decimal add_sum = 0m;
					for (int i = 0; i < 3; i++) {
						SystemTime.Now = () => DateTime.Now.AddDays(i);
						if (client.RatedPeriodDate == null)
							client.RatedPeriodDate = SystemTime.Now();
						if ((client.RatedPeriodDate.Value.AddMonths(1).Date - SystemTime.Now().Date).Days <= 0)
							client.RatedPeriodDate = SystemTime.Now();
						var toDt = client.GetInterval();
						var price = client.GetPrice();
						add_sum += price/toDt;
					}
					if (add_sum > 0)
					new Payment {
						Client = client,
						Sum = add_sum,
						PaidOn = DateTime.Now,
						RecievedOn = DateTime.Now
					}.Save();
					Console.WriteLine(string.Format("Клиент {0} активирован, ему начислено {1} руб", client.Id.ToString("00000"), add_sum.ToString("0.00")));
				}
				foreach (var client in whoRealWork) {
					session.Evict(client);
				}
				session.VoteCommit();
			}
			using (var session = new TransactionScope(OnDispose.Rollback)) {
				//!!!!!!!!!!!!!!!! ИЗМЕНИ ПРИ SQL2!!!!!!!!!!!1
				var lastDay = new DateTime(2011, 11, 29, 00, 00, 00);
				var clientWhoBlocked = ClientInternetLogs.Queryable.Where(c =>
				                                                          c.Disabled &&
				                                                          c.OperatorName == "InternetBilling" &&
				                                                          c.LogTime >= lastDay).ToList().Select(c => c.Client).ToList();
				foreach (var client in clientWhoBlocked) {
					client.Disabled = false;
					client.AutoUnblocked = true;
					client.RatedPeriodDate = null;
					client.ShowBalanceWarningPage = false;
					client.Status = Status.Find((uint) StatusType.Worked);
					Console.WriteLine(string.Format("Клиент {0} активирован", client.Id.ToString("00000")));
					var service = client.ClientServices.FirstOrDefault();
					if (service != null && service.GetType() == typeof(DebtWork)) {
						service.Delete();
						Console.WriteLine(string.Format("Для клиента {0} удалена услуга обещанный платеж", client.Id.ToString("00000")));
					}
				}
				session.VoteCommit();
			}
			Console.WriteLine("ГОТОВО!");
		}

		public static void LawyerPersonMonth()
		{
			var totalSum = 0m;
			var totalCount = 0;
			var unClient = new List<uint>();
			using (var transaction = new TransactionScope(OnDispose.Rollback))
			{
				var lawPerson = Client.Queryable.Where(c => c.LawyerPerson != null && c.PhysicalClient == null).ToList();
				Console.WriteLine("Всего персон " + lawPerson.Count);
				foreach (var client in lawPerson)
				{
					var writeOffs =
						WriteOff.Queryable.Where(w => w.Client == client).ToList().Where(
							w => w.WriteOffDate.Date == new DateTime(2011, 10, 1)).ToList();
					var writeOff30 = writeOffs.FirstOrDefault();
					//var writeOff30Last = writeOffs.LastOrDefault();
					if (writeOff30 != null)
					{
						var writeOff29 =
							WriteOff.Queryable.Where(w => w.Client == client).ToList().Where(
								w => w.WriteOffDate.Date == new DateTime(2011, 09, 29)).FirstOrDefault();
						if (writeOff29 != null)
						{
							var balanceChange = writeOff29.WriteOffSum - writeOff30.WriteOffSum;
							writeOff30.WriteOffSum = writeOff29.WriteOffSum;
							writeOff30.Update();
							var lawyer = client.LawyerPerson;
							lawyer.Balance -= balanceChange;
							lawyer.Update();
							Console.WriteLine(string.Format("Коррекция для клиента {0} на {1} рублей", client.Id.ToString("00000"),
							                                balanceChange));
							totalCount++;
							totalSum += balanceChange;
						}
						else
						{
							unClient.Add(client.Id);
						}
					}
					else
					{
						unClient.Add(client.Id);
					}
				}
				transaction.VoteCommit();
			}
			Console.WriteLine(string.Format("Всего клиентов обработано: {0} общая сумма корроектировки : {1}", totalCount, totalSum));
			Console.WriteLine("Клиенты не обработаны: " + unClient.Implode(" "));
			Console.ReadLine();
		}

		public static void diactivateClient()
		{
			using (var session = new TransactionScope(OnDispose.Rollback))
			{
				var inlogs =
					PhysicalClientInternetLogs.Queryable.Where(
						p =>
						p.LogTime >= DateTime.Parse("2011-09-19 16:14:04") && p.OperatorName == "Zolotarev" &&
						p.LogTime <= DateTime.Parse("2011-09-19 16:33:04") && p.Balance != null).Select(
							i => Client.Queryable.Where(c => c.PhysicalClient == i.ClientId).FirstOrDefault()).ToList().Where(c => c != null)
						.ToList();
				foreach (var client in inlogs)
				{
					if (client.PhysicalClient.Balance >= 0)
						client.Disabled = false;
					else
					{
						client.Disabled = true;
						Console.WriteLine("Деактивирован клиент " + client.Id.ToString("00000"));
					}
					client.Update();
				}
				session.VoteCommit();
			}
			Console.ReadLine();
		}

		public static void DeleteWriteOffs()
		{
			using (var session = new TransactionScope(OnDispose.Rollback))
			{
				var inlogs =
					PhysicalClientInternetLogs.Queryable.Where(
						p =>
						p.LogTime >= DateTime.Parse("2011-09-19 16:14:04") && p.OperatorName == "Zolotarev" &&
						p.LogTime <= DateTime.Parse("2011-09-19 16:33:04") && p.Balance != null).Select(
							i => Client.Queryable.Where(c => c.PhysicalClient == i.ClientId).FirstOrDefault()).ToList().Where(c => c != null).Select(c=>c.Id).ToList()
						.ToList();
				var errWrite =
					WriteOff.Queryable.Where(
						w => w.WriteOffDate >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 18, 00, 00, 00)).ToList().Where(
							c => inlogs.Contains(c.Client.Id)).ToList();
				foreach (var writeOff in errWrite)
				{
					writeOff.Delete();
					Console.WriteLine(string.Format("Списание {0} удалено сумма {1} клиент {2}", writeOff.Id,
					                                writeOff.WriteOffSum.ToString("0.00"), writeOff.Client.Id.ToString("00000")));
				}
				session.VoteCommit();
				Console.WriteLine("Всего " + errWrite.Count);
			}
			Console.WriteLine("Conmmited");
			Console.ReadLine();
		}

		public static void DeleteWriteOffsWhoDis()
		{
			using (var session = new TransactionScope(OnDispose.Rollback))
			{
				var errWrite =
					WriteOff.Queryable.Where(
						w => w.WriteOffDate >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 18, 00, 00, 00) && w.Client.Disabled).ToList();
				var disClientWork =
					Internetsessionslog.Queryable.Where(
						i =>
						i.LeaseBegin > new DateTime(DateTime.Now.Year, DateTime.Now.Month, 18, 00, 00, 00) &&
						i.LeaseBegin < new DateTime(DateTime.Now.Year, DateTime.Now.Month, 18, 18, 10, 00) && i.EndpointId.Client.Disabled)
						.ToList();
				var clientDisWorked = disClientWork.Where(i => WhiteIp(i.IP)).Select(i => i.EndpointId.Client.Id).ToList();
				var deletingWO = errWrite.Where(w => !clientDisWorked.Contains(w.Client.Id)).ToList();
				foreach (var writeOff in deletingWO)
				{
					if (writeOff.Client.PhysicalClient != null)
					{
						var pClient = writeOff.Client.PhysicalClient;
						pClient.Balance -= writeOff.WriteOffSum;
						pClient.Update();
					}
					if (writeOff.Client.LawyerPerson != null)
					{
						var lCLient = writeOff.Client.LawyerPerson;
						lCLient.Balance -= writeOff.WriteOffSum;
						lCLient.Update();
					}
					Console.WriteLine(string.Format("Клиент {0} дата списания {1} зачислено {2} рублей",
					                                writeOff.Client.Id.ToString("00000"),
					                                writeOff.WriteOffDate,
					                                writeOff.WriteOffSum.ToString("0.00")));
					writeOff.Delete();
				}
				session.VoteCommit();
				Console.WriteLine(string.Format("Следующие клиенты работали, хотя они заблокированы: {0}", clientDisWorked.Implode(" ")));
			}
			Console.ReadLine();
		}

		public static bool WhiteIp(string IP)
		{
			var ip = Int64.Parse(IP);
			return (Int64.Parse("1541080065") <= ip) && (Int64.Parse("1541080319") >= ip) ||
			       (Int64.Parse("1541080833") <= ip) && (Int64.Parse("1541081086") >= ip) ||
			       (Int64.Parse("1541080321") <= ip) && (Int64.Parse("1541080575") >= ip);
		}

		public static void ChangeWriteOffsToday()
		{
			using (var session = new TransactionScope(OnDispose.Rollback))
			{
				var errWrite =
					WriteOff.Queryable.Where(
						w => w.WriteOffDate >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 18, 22,00,00)).ToList();
				foreach (var writeOff in errWrite)
				{
					if (writeOff.Client.PhysicalClient != null)
					{
						var pClient = writeOff.Client.PhysicalClient;
						pClient.Balance += writeOff.WriteOffSum;
						pClient.Update();
					}
					if (writeOff.Client.LawyerPerson != null)
					{
						var lCLient = writeOff.Client.LawyerPerson;
						lCLient.Balance += writeOff.WriteOffSum;
						lCLient.Update();
					}
					Console.WriteLine(string.Format("Клиент {0} дата списания {1} зачислено {2} рублей",
					                                writeOff.Client.Id.ToString("00000"),
					                                writeOff.WriteOffDate,
					                                writeOff.WriteOffSum.ToString("0.00")));
					writeOff.Delete();
				}
				session.VoteCommit();
			}
			Console.WriteLine("Commited");
			Console.ReadLine();
		}


		public static void IntervalTariffs()
		{
			using (new SessionScope())
			{
				var writeOffs = WriteOff.Queryable.Where(w => w.WriteOffDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
				foreach (var writeOff in writeOffs)
				{
					var ph = writeOff.Client.PhysicalClient;
					var lp = writeOff.Client.LawyerPerson;
					if (ph != null)
					{
						ph.Balance += writeOff.WriteOffSum;
						ph.Update();
						Console.WriteLine(string.Format("Клиент {0} зачислено {1}", writeOff.Client.Id, writeOff.WriteOffSum.ToString("0.00")));
						writeOff.Delete();
						continue;
					}
					if (lp != null)
					{
						lp.Balance += writeOff.WriteOffSum;
						lp.Update();
						Console.WriteLine(string.Format("Клиент {0} зачислено {1}", writeOff.Client.Id, writeOff.WriteOffSum.ToString("0.00")));
						writeOff.Delete();
						continue;
					}
				}
			}
			Console.ReadLine();
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
			ActiveRecordStarter.EventListenerComponentRegistrationHook += RemoverListner.Make;
			ActiveRecordStarter.Initialize(
				new[] { typeof(Client).Assembly, typeof(PhysicalClientInternetLogs).Assembly },
				configuration);

		}
	}
}
