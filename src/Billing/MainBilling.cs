using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.MySql;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;
using log4net;
using log4net.Config;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace Billing
{
	public class MainBilling
	{
		public const int FreeDaysVoluntaryBlockin = 28;

		private readonly ILog _log = LogManager.GetLogger(typeof(MainBilling));

		private Mutex _mutex = new Mutex();
		private SaleSettings _saleSettings;

		public List<SmsMessage> Messages;

		public MainBilling()
		{
			try {
				XmlConfigurator.Configure();
				InitActiveRecord();
			}
			catch (Exception ex) {
				_log.Error("Ошибка к конструкторе", ex);
			}
		}

		public static void InitActiveRecord()
		{
			if (!ActiveRecordStarter.IsInitialized) {
				ActiveRecordStarter.EventListenerComponentRegistrationHook += RemoverListner.Make;
				ActiveRecordInitialize.Init(ConnectionHelper.GetConnectionName(), new[] { typeof(Client).Assembly });
			}
		}

		public void On()
		{
			try {
				_mutex.WaitOne();

				OnMethod();
			}
			catch (Exception ex) {
				_log.Error("Ошибка в методе OnMethod", ex);
			}
			finally {
				_mutex.ReleaseMutex();
			}
		}

		public void Run()
		{
			try {
				_mutex.WaitOne();
				var now = SystemTime.Now();
				bool errorFlag;
				bool normalFlag;
				using (new SessionScope()) {
					var settings = ActiveRecordMediator<InternetSettings>.FindFirst();
					errorFlag = settings.LastStartFail && (settings.NextBillingDate - now).TotalMinutes > 0;
					normalFlag = (settings.NextBillingDate - now).TotalMinutes <= 0;
					if (normalFlag) {
						Reset();

						var billingTime = InternetSettings.FindFirst();
						if (now.Hour < 22) {
							billingTime.NextBillingDate = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0);
						}
						else {
							billingTime.NextBillingDate = SystemTime.Now().AddDays(1).Date.AddHours(22);
						}
						billingTime.Save();
					}
				}

				if (normalFlag || errorFlag)
					Compute();
			}
			catch (Exception ex) {
				_log.Error("Ошибка в методе Compute", ex);
			}
			finally {
				_mutex.ReleaseMutex();
			}
		}

		public static void Reset()
		{
			ArHelper.WithSession(
				s => s.CreateSQLQuery(@"
update internet.Clients c
set c.PaidDay = false;

update internet.InternetSettings s
set s.LastStartFail = true;")
					.ExecuteUpdate());
		}

		private void WithTransaction(Action<ISession> action)
		{
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				ArHelper.WithSession(action);
				transaction.VoteCommit();
			}
		}

		private void ActivateServices(ISession session)
		{
			var services = session.Query<ClientService>().Where(s => !s.Activated);
			foreach (var service in services) {
				service.Activate();
				session.SaveOrUpdate(service);
			}
		}

		public void OnMethod()
		{
			WithTransaction(ActivateServices);

			WithTransaction(session => {
				var newEndPointForConnect = session.Query<ClientEndpoint>().Where(c => c.Client.PhysicalClient != null && !c.PayForCon.Paid && c.Client.BeginWork != null).ToList();
				foreach (var clientEndpoint in newEndPointForConnect) {
					var writeOff = new UserWriteOff(clientEndpoint.Client, clientEndpoint.PayForCon.Sum, "Плата за подключение", false);
					session.SaveOrUpdate(writeOff);
					clientEndpoint.PayForCon.Paid = true;
					session.SaveOrUpdate(clientEndpoint.PayForCon);
				}
			});

			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				var newPayments = Payment.FindAll(DetachedCriteria.For(typeof(Payment)).Add(Restrictions.Eq("BillingAccount", false)));
				foreach (var newPayment in newPayments) {
					var updateClient = newPayment.Client;
					var physicalClient = updateClient.PhysicalClient;
					var lawyerClient = updateClient.LawyerPerson;
					if (physicalClient != null) {
						if (newPayment.Virtual)
							physicalClient.VirtualBalance += newPayment.Sum;
						else {
							physicalClient.MoneyBalance += newPayment.Sum;
						}

						physicalClient.Balance += Convert.ToDecimal(newPayment.Sum);
						physicalClient.Update();
						newPayment.BillingAccount = true;
						newPayment.Update();
						SmsHelper.DeleteNoSendingMessages(updateClient);
						if (updateClient.HavePaymentToStart()) {
							updateClient.AutoUnblocked = true;
						}
						if (updateClient.RatedPeriodDate != null)
							if (physicalClient.Balance >= updateClient.GetPriceIgnoreDisabled() * updateClient.PercentBalance) {
								updateClient.ShowBalanceWarningPage = false;
								if (updateClient.IsChanged(c => c.ShowBalanceWarningPage))
									Appeals.CreareAppeal("Отключена страница Warning, клиент внес платеж", updateClient, AppealType.Statistic, false);
							}
						if (updateClient.ClientServices != null)
							foreach (var clientService in updateClient.ClientServices.ToList()) {
								clientService.PaymentClient();
							}
						updateClient.Update();
					}
					if (lawyerClient != null) {
						lawyerClient.Balance += Convert.ToDecimal(newPayment.Sum);
						lawyerClient.UpdateAndFlush();
						newPayment.BillingAccount = true;
						newPayment.UpdateAndFlush();
						updateClient.Disabled = false;
						updateClient.Update();
					}
				}
				transaction.VoteCommit();
			}
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				var userWriteOffs = UserWriteOff.Queryable.Where(u => !u.BillingAccount && u.Client != null).ToList();
				foreach (var userWriteOff in userWriteOffs) {
					var client = userWriteOff.Client;
					if (client.PhysicalClient != null) {
						var physicalClient = client.PhysicalClient;
						physicalClient.WriteOff(userWriteOff.Sum);
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
				transaction.VoteCommit();
			}
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				var clients = Client.Queryable.Where(c => c.PhysicalClient != null && c.Disabled && c.AutoUnblocked).ToList();
				clients = clients.Where(c => c.PhysicalClient.Balance > c.GetPriceIgnoreDisabled() * c.PercentBalance).ToList();
				var workStatus = Status.Find((uint)StatusType.Worked);
				foreach (var client in clients) {
					client.Status = workStatus;
					client.RatedPeriodDate = null;
					client.DebtDays = 0;
					client.ShowBalanceWarningPage = false;
					client.Disabled = false;
					if (client.IsChanged(c => c.ShowBalanceWarningPage))
						Appeals.CreareAppeal("Отключена страница Warning, клиент разблокирован", client, AppealType.Statistic, false);
					if (client.IsChanged(c => c.Disabled))
						Appeals.CreareAppeal("Клиент разблокирован", client, AppealType.Statistic, false);
					client.UpdateAndFlush();
					SmsHelper.DeleteNoSendingMessages(client);
				}
				var lawyerPersons = Client.Queryable.Where(c => c.LawyerPerson != null);
				foreach (var client in lawyerPersons) {
					if (client.NeedShowWarningForLawyer()) {
						if (client.WhenShowWarning == null ||
							(SystemTime.Now() - client.WhenShowWarning.Value).TotalHours >= 3) {
							client.ShowBalanceWarningPage = true;
							client.WhenShowWarning = SystemTime.Now();
							if (!client.SendEmailNotification)
								client.SendEmailNotification = EmailNotificationSender.SendLawyerPersonNotification(client);
							if (client.IsChanged(c => c.ShowBalanceWarningPage))
								Appeals.CreareAppeal("Включена страница Warning, клиент имеет низкий баланс", client, AppealType.Statistic, false);
						}
					}
					else {
						client.ShowBalanceWarningPage = false;
						client.SendEmailNotification = false;
						client.WhenShowWarning = null;
						if (client.IsChanged(c => c.ShowBalanceWarningPage))
							Appeals.CreareAppeal("Отключена страница Warning", client, AppealType.Statistic, false);
					}
					client.Update();
				}
				foreach (var cserv in ActiveRecordMediator.FindAll(typeof(ClientService)).Cast<ClientService>()) {
					cserv.Deactivate();
				}
				transaction.VoteCommit();
			}
		}

		public virtual void Compute()
		{
			int errorCount = 0;
			using (new SessionScope()) {
				var settings = ActiveRecordMediator<InternetSettings>.FindFirst();
				settings.LastStartFail = true;
				settings.Save();

				Messages = new List<SmsMessage>();
				_saleSettings = SaleSettings.FindFirst();
			}

			ProcessAll(WriteOffFromPhysicalClient,
				() => Client.Queryable.Where(c => c.PhysicalClient != null && !c.PaidDay),
				ref errorCount);

			ProcessAll(WriteOffFromLawyerPerson,
				() => Client.Queryable.Where(c => c.LawyerPerson != null && c.LawyerPerson.Tariff != null && !c.PaidDay),
				ref errorCount);

			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				var agentSettings = AgentTariff.GetPriceForAction(AgentActions.AgentPayIndex);
				var needToAgentSum = AgentTariff.GetPriceForAction(AgentActions.WorkedClient);
				var bonusesClients = Client.Queryable.Where(c =>
					c.Request != null &&
						!c.Request.PaidBonus &&
						c.Request.Registrator != null &&
						c.BeginWork != null)
					.ToList();
				foreach (var client in bonusesClients) {
					if (client.Payments.Sum(p => p.Sum) >= needToAgentSum * agentSettings) {
						var request = client.Request;
						request.PaidBonus = true;
						request.Update();
						new PaymentsForAgent {
							Action = AgentTariff.GetAction(AgentActions.WorkedClient),
							Agent = request.Registrator,
							RegistrationDate = SystemTime.Now(),
							Sum = needToAgentSum,
							Comment = string.Format("Клиент {0} начал работать (Заявка №{1})", client.Id, client.Request.Id)
						}.Save();
					}
				}

				var friendBonusRequests = Request.Queryable.Where(r =>
					r.Client != null &&
						r.FriendThisClient != null &&
						!r.PaidFriendBonus &&
						r.Client.BeginWork != null)
					.ToList();
				foreach (var friendBonusRequest in friendBonusRequests) {
					if (friendBonusRequest.Client.HavePaymentToStart()) {
						new Payment {
							Client = friendBonusRequest.FriendThisClient,
							Sum = 250m,
							PaidOn = SystemTime.Now(),
							RecievedOn = SystemTime.Now(),
							Virtual = true,
							Comment = string.Format("Подключи друга {0}", friendBonusRequest.FriendThisClient.Id)
						}.Save();
						friendBonusRequest.PaidFriendBonus = true;
						friendBonusRequest.Update();
					}
				}

				transaction.VoteCommit();
			}
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				var settings = ActiveRecordMediator<InternetSettings>.FindFirst();
				settings.LastStartFail = errorCount > 0;
				settings.Save();

				transaction.VoteCommit();
			}
		}

		private void ProcessAll(Action<Client> action, Func<IQueryable<Client>> query, ref int errorCount)
		{
			var ids = new List<uint>();
			using (new SessionScope()) {
				ids = query().Select(c => c.Id).ToList();
			}

			foreach (var id in ids) {
				try {
					using (var transaction = new TransactionScope(OnDispose.Rollback)) {
						var client = ActiveRecordMediator<Client>.FindByPrimaryKey(id);
						action(client);
						foreach (var clientService in client.ClientServices.ToList()) {
							clientService.WriteOff();
						}
						client.PaidDay = true;
						client.Update();
						transaction.VoteCommit();
					}
				}
				catch (Exception ex) {
					errorCount++;
					_log.Error(string.Format("Ошибка при обработке клиента {0}", id), ex);
				}
			}
		}

		private void WriteOffFromPhysicalClient(Client client)
		{
			var phisicalClient = client.PhysicalClient;
			var balance = phisicalClient.Balance;
			if (balance >= 0
				&& !client.Disabled
				&& client.RatedPeriodDate != DateTime.MinValue
				&& client.RatedPeriodDate != null) {
				var dtNow = SystemTime.Now();

				if ((client.RatedPeriodDate.Value.AddMonths(1).Date - dtNow.Date).Days == -client.DebtDays) {
					var dtFrom = client.RatedPeriodDate.Value;
					var dtTo = dtNow;
					client.DebtDays += dtFrom.Day - dtTo.Day;
					var thisMonth = dtNow.Month;
					client.RatedPeriodDate = dtNow.AddDays(client.DebtDays);
					while (client.RatedPeriodDate.Value.Month != thisMonth) {
						client.RatedPeriodDate = client.RatedPeriodDate.Value.AddDays(-1);
					}
				}
				else if ((client.RatedPeriodDate.Value.AddMonths(1).Date - dtNow.Date).TotalDays < -client.DebtDays) {
					client.RatedPeriodDate = dtNow.AddDays(client.DebtDays);
					client.DebtDays = 0;
				}
			}
			if (client.StartNoBlock != null) {
				decimal sale = 0;
				var monthOnStart = SystemTime.Now().TotalMonth(client.StartNoBlock.Value);
				if (monthOnStart >= _saleSettings.PeriodCount)
					sale = _saleSettings.MinSale + (monthOnStart - _saleSettings.PeriodCount) * _saleSettings.SaleStep;
				if (sale > _saleSettings.MaxSale)
					sale = _saleSettings.MaxSale;
				if (sale >= _saleSettings.MinSale)
					client.Sale = sale;
			}
			if (client.GetPrice() > 0 && !client.PaidDay
				&& client.RatedPeriodDate != DateTime.MinValue
				&& client.RatedPeriodDate != null) {
				if (client.StartNoBlock == null)
					client.StartNoBlock = SystemTime.Now();

				var daysInInterval = client.GetInterval();
				var price = client.GetPrice();
				var sum = price / daysInInterval;

				var writeOff = phisicalClient.WriteOff(sum);
				if (writeOff != null) {
					writeOff.Save();
					//для отладки
					//Console.WriteLine("Клиент {0} cписано {1}", client.Id, writeOff);
				}


				phisicalClient.Update();

				var bufBal = phisicalClient.Balance;
				//Отсылаем смс если клиенту осталось работать 2 дня или меньше
				if (client.SendSmsNotifocation && (bufBal - sum * 2 < 0)) {
					if (phisicalClient.Balance > 0) {
						var message = string.Format("Ваш баланс {0} руб. {1} доступ в сеть будет заблокирован.",
							client.PhysicalClient.Balance.ToString("0.00"),
							bufBal - sum < 0 ? "Завтра" : "Послезавтра");
						var now = SystemTime.Now();
						DateTime shouldBeSendDate;
						if (now.Hour < 22)
							shouldBeSendDate = new DateTime(now.Year, now.Month, now.Day, 12, 0, 0);
						else {
							shouldBeSendDate = SystemTime.Now().Date.AddDays(1).AddHours(12);
						}
						var smsMessage = new SmsMessage(client, message, shouldBeSendDate);
						smsMessage.Save();
						Messages.Add(smsMessage);
					}
				}
				if (client.NeedShowWarning(sum)) {
					client.ShowBalanceWarningPage = true;
					if (client.IsChanged(c => c.ShowBalanceWarningPage))
						if (client.ShowWarningBecauseNoPassport())
							Appeals.CreareAppeal("Включена страница Warning, клиент не имеет паспортных данных", client, AppealType.Statistic, false);
						else {
							Appeals.CreareAppeal("Включена страница Warning, клиент имеет низкий баланс", client, AppealType.Statistic, false);
						}
				}
				else {
					client.ShowBalanceWarningPage = false;
					if (client.IsChanged(c => c.ShowBalanceWarningPage))
						Appeals.CreareAppeal("Отключена страница Warning", client, AppealType.Statistic, false);
				}
			}
			if (client.CanBlock()) {
				client.Disabled = true;
				client.Sale = 0;
				client.StartNoBlock = null;
				client.Status = Status.Find((uint)StatusType.NoWorked);
				if (client.IsChanged(c => c.Disabled))
					Appeals.CreareAppeal("Клиент был заблокирован", client, AppealType.Statistic, false);
			}
			if ((client.YearCycleDate == null && client.BeginWork != null) || (SystemTime.Now().Date >= client.YearCycleDate.Value.AddYears(1).Date)) {
				client.FreeBlockDays = FreeDaysVoluntaryBlockin;
				client.YearCycleDate = SystemTime.Now();
			}
		}
		public static DateTime MagicDate = new DateTime(2013, 4, 1);

		private static void WriteOffFromLawyerPerson(Client client)
		{
			var person = client.LawyerPerson;

			var now = SystemTime.Now();
			var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
			var tariff = person.Tariff.Value;
			decimal sum = 0;
			if (now < MagicDate) {
				sum = tariff / daysInMonth;
				if (sum == 0)
					return;

				//если это последний день месяца то нам нужно учесть накопивщуюся ошибку округления
				if (now.Date == now.Date.LastDayOfMonth()) {
					sum += tariff - Math.Round(tariff / daysInMonth, 2) * daysInMonth;
				}
				person.Balance -= sum;
				person.UpdateAndFlush();
				new WriteOff(client, sum).SaveAndFlush();
			}
			else {
				var writeOffs = new List<WriteOff>();
				//если это день активации заказа, то списываем плату за разовые услуги и за периодические пропорционально оставшимся дням месяца
				var activateOrders = client.Orders.Where(o => o.BeginDate.Value.Date == now.Date);
				var disableOrders = client.Orders.Where(o => o.EndDate != null && o.EndDate.Value.Date == now.Date);
				if (activateOrders.Any()) {
					var periodicService = activateOrders.SelectMany(s => s.OrderServices)
						.Where(s => s.IsPeriodic);
					var notPeriodicService = activateOrders.SelectMany(s => s.OrderServices)
						.Where(s => !s.IsPeriodic);
					sum += notPeriodicService.Sum(s => s.Cost);
					sum += periodicService.Sum(s => s.Cost) / daysInMonth * (daysInMonth - now.Day + 1);
					foreach (var orderService in notPeriodicService) {
						writeOffs.Add(new WriteOff(client, orderService.Cost) {
							Comment = orderService.Description + " по заказу №" + orderService.Order.Number
						});
					}
					foreach (var orderService in periodicService) {
						writeOffs.Add(new WriteOff(client, orderService.Cost / daysInMonth * (daysInMonth - now.Day + 1)) {
							Comment = orderService.Description + " по заказу №" + orderService.Order.Number
						});
					}
				}
				else if (disableOrders.Any()) {
					//если это день деактивации заказа, то нужно вернуть сумму за оставшееся число дней в месяце за периодические услуги
					var periodicService = disableOrders.SelectMany(s => s.OrderServices)
						.Where(s => s.IsPeriodic);
					sum -= periodicService.Sum(s => s.Cost) / daysInMonth * (daysInMonth - now.Day);
					foreach (var orderService in periodicService) {
						writeOffs.Add(new WriteOff(client, -orderService.Cost / daysInMonth * (daysInMonth - now.Day)) {
							Comment = orderService.Description + " по заказу №" + orderService.Order.Number
						});
					}
				}
				else if (now.Date == now.Date.FirstDayOfMonth()) {
					//если это первый день месяца, то списываем плату за периодические услуги активных заказов
					var orderServices = client.Orders.Where(o => o.OrderStatus == OrderStatus.Enabled)
						.SelectMany(s => s.OrderServices)
						.Where(s => s.IsPeriodic);
					sum += orderServices.Sum(s => s.Cost);
					foreach (var orderService in orderServices) {
						writeOffs.Add(new WriteOff(client, orderService.Cost) {
							Comment = orderService.Description + " по заказу №" + orderService.Order.Number
						});
					}
				}
				person.Balance -= sum;
				person.UpdateAndFlush();
				foreach (var writeOff in writeOffs) {
					writeOff.SaveAndFlush();
				}
			}
		}
	}
}
