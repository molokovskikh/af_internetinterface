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
using Common.Tools.Calendar;
using Common.Web.Ui.Helpers;
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
		private SaleSettings _saleSettings;

		public List<SmsMessage> Messages;

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

		public void On()
		{
			try
			{
				_mutex.WaitOne();

				OnMethod();
			}
			catch (Exception ex) {
				_log.Error("Ошибка в методе OnMethod", ex);
			}
			finally 
			{ 
				_mutex.ReleaseMutex();
			}
		}


		public void Run()
		{
			try
			{
				_mutex.WaitOne();
				var now = SystemTime.Now();
				bool errorFlag;
				bool normalFlag;
				using (new SessionScope()) {
					var settings = ActiveRecordMediator<InternetSettings>.FindFirst();
					errorFlag = settings.LastStartFail && (settings.NextBillingDate - now).TotalMinutes > 0;
					normalFlag = (settings.NextBillingDate - now).TotalMinutes <= 0;
					if (normalFlag) {
						ArHelper.WithSession(s => s.CreateSQLQuery(@"
	update internet.Clients c
	set c.PaidDay = false;

	update internet.InternetSettings s
	set s.LastStartFail = true;").ExecuteUpdate());

						var billingTime = InternetSettings.FindFirst();
						if (now.Hour < 22)
						{
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
			finally 
			{ 
				_mutex.ReleaseMutex();
			}
		}

		public void OnMethod()
		{
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				foreach (var cserv in ClientService.Queryable.Where(c => !c.Activated && !c.Diactivated).ToList()) {
					cserv.Activate();
				}
				var newClients = Client.FindAll(DetachedCriteria.For(typeof (Client))
			                                		.CreateAlias("PhysicalClient", "PC", JoinType.InnerJoin)
			                                		.Add(Restrictions.Eq("PC.ConnectionPaid", false))
			                                		.Add(Restrictions.IsNotNull("BeginWork"))
			                                		.Add(Restrictions.IsNotNull("PhysicalClient")));
				foreach (var newClient in newClients) {
					var client = newClient.PhysicalClient;
					client.ConnectionPaid = true;
					var writeOff = client.WriteOff(client.ConnectSum);
					client.UpdateAndFlush();
					newClient.UpdateAndFlush();

					if (writeOff != null)
						writeOff.Save();
				}
				transaction.VoteCommit();
			}
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				var newPayments = Payment.FindAll(DetachedCriteria.For(typeof (Payment)).Add(Restrictions.Eq("BillingAccount", false)));
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
							if (physicalClient.Balance >= updateClient.GetPriceForTariff()) {
								updateClient.ShowBalanceWarningPage = false;
							}
						if (updateClient.ClientServices != null)
							for (var i = updateClient.ClientServices.Count - 1; i > -1; i--) {
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
				var clients =
					Client.Queryable.Where(
						c => c.PhysicalClient != null && c.Disabled && c.AutoUnblocked).ToList().Where(
							c => c.PhysicalClient.Balance >= c.GetPriceForTariff()*c.PercentBalance).ToList();
				var workStatus = Status.Find((uint) StatusType.Worked);
				foreach (var client in clients) {
					client.Status = workStatus;
					client.RatedPeriodDate = null;
					client.DebtDays = 0;
					client.ShowBalanceWarningPage = false;
					client.Disabled = false;
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
						}
					}
					else {
						client.ShowBalanceWarningPage = false;
						client.SendEmailNotification = false;
						client.WhenShowWarning = null;
					}
					client.Update();
				}
				foreach (var cserv in ClientService.FindAll()) {
					cserv.Diactivate();
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
				_saleSettings  = SaleSettings.FindFirst();
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
					c.BeginWork != null).ToList();
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
				transaction.VoteCommit();
			}
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {

				var settings = ActiveRecordMediator<InternetSettings>.FindFirst();
				settings.LastStartFail = errorCount > 0;
				settings.Save();

				ClientService.Queryable.ToList().Each(cs => cs.WriteOff());

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
			if ((balance >= 0) &&
				(!client.Disabled) &&
					(client.RatedPeriodDate != DateTime.MinValue) && (client.RatedPeriodDate != null)) {
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
				else {
					if ((client.RatedPeriodDate.Value.AddMonths(1).Date - dtNow.Date).TotalDays < -client.DebtDays) {
						client.RatedPeriodDate = dtNow.AddDays(client.DebtDays);
						;
						client.DebtDays = 0;
					}
				}
			}
			if (client.StartNoBlock != null) {
				decimal sale = 0;
				var monthOnStart = SystemTime.Now().TotalMonth(client.StartNoBlock.Value);
				if (monthOnStart >= _saleSettings.PeriodCount)
					sale = _saleSettings.MinSale + (monthOnStart - _saleSettings.PeriodCount)*_saleSettings.SaleStep;
				if (sale > _saleSettings.MaxSale)
					sale = _saleSettings.MaxSale;
				if (sale >= _saleSettings.MinSale)
					client.Sale = sale;
			}
			if (client.GetPrice() > 0 && !client.PaidDay) {
				if (client.RatedPeriodDate != DateTime.MinValue && client.RatedPeriodDate != null) {
					if (client.StartNoBlock == null)
						client.StartNoBlock = SystemTime.Now();
					var daysInInterval = client.GetInterval();
					var price = client.GetPrice();
					var sum = price/daysInInterval;

					var writeOff = phisicalClient.WriteOff(sum);
					if (writeOff != null)
						writeOff.Save();

					phisicalClient.Update();

					var bufBal = phisicalClient.Balance;
					var minimumBalance = bufBal - sum < 0;
					if (minimumBalance) {
						client.ShowBalanceWarningPage = true;
						if (client.SendSmsNotifocation) {
							if (phisicalClient.Balance > 0) {
								var message = string.Format("Ваш баланс {0} руб. Завтра доступ в сеть будет заблокирован.",
									client.PhysicalClient.Balance.ToString("0.00"));
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
					}
					else {
						client.ShowBalanceWarningPage = false;
					}
				}
			}
			if (client.CanBlock()) {
				client.Disabled = true;
				client.Sale = 0;
				client.StartNoBlock = null;
				client.Status = Status.Find((uint) StatusType.NoWorked);
			}
			if (client.YearCycleDate == null || (SystemTime.Now().Date >= client.YearCycleDate.Value.AddYears(1).Date)) {
				client.FreeBlockDays = FreeDaysVoluntaryBlockin;
				client.YearCycleDate = SystemTime.Now();
			}
		}

		private static void WriteOffFromLawyerPerson(Client client)
		{
			var person = client.LawyerPerson;
			if (client.Disabled)
				return;

			var now = SystemTime.Now();
			var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
			var tariff = person.Tariff.Value;
			var sum = tariff/daysInMonth;
			if (sum == 0)
				return;

			//если это последний день месяца то нам нужно учесть накопивщуюся ошибку округления
			if (now.Date == now.Date.LastDayOfMonth()) {
				sum += tariff - Math.Round(tariff/daysInMonth, 2) * daysInMonth;
			}

			person.Balance -= sum;
			person.UpdateAndFlush();
			new WriteOff(client, sum).SaveAndFlush();
		}
	}
}
