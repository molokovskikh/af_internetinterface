using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.MySql;
using Common.NHibernate;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
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
				ActiveRecordStarter.EventListenerComponentRegistrationHook += AuditListener.RemoveAuditListener;
				ActiveRecordInitialize.Init(ConnectionHelper.GetConnectionName(), new[] { typeof(Client).Assembly });
			}
		}

		public void SafeProcessPayments()
		{
			try {
				_mutex.WaitOne();

				ProcessPayments();
			}
			catch (Exception ex) {
				_log.Error("При обработке платежей", ex);
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
					ProcessWriteoffs();
			}
			catch (Exception ex) {
				_log.Error("Ошибка при начислении списаний", ex);
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
			var services = session.Query<ClientService>().Where(s => !s.IsActivated);
			foreach (var service in services) {
				service.TryActivate();
				session.Save(service);
			}
		}

		public void ProcessPayments()
		{
			WithTransaction(ActivateServices);

			WithTransaction(session => {
				var newEndPointForConnect = session.Query<ClientEndpoint>().Where(c => c.Client.PhysicalClient != null && !c.PayForCon.Paid && c.Client.BeginWork != null).ToList();
				foreach (var clientEndpoint in newEndPointForConnect) {
					var writeOff = new UserWriteOff(clientEndpoint.Client, clientEndpoint.PayForCon.Sum, "Плата за подключение");
					session.Save(writeOff);
					clientEndpoint.PayForCon.Paid = true;
					session.Save(clientEndpoint.PayForCon);
				}
			});

			WithTransaction(session => {
				var payments = session.Query<Payment>().Where(p => !p.BillingAccount);
				foreach (var payment in payments) {
					var updateClient = payment.Client;
					if (updateClient.PhysicalClient != null) {
						updateClient.PhysicalClient.AccountPayment(payment);
						payment.BillingAccount = true;
						SmsHelper.DeleteNoSendingMessages(updateClient);
						if (updateClient.HavePaymentToStart()) {
							updateClient.AutoUnblocked = true;
						}
						if (updateClient.RatedPeriodDate != null) {
							if (updateClient.PhysicalClient.Balance >= updateClient.GetPriceIgnoreDisabled() * updateClient.PercentBalance) {
								updateClient.ShowBalanceWarningPage = false;
								if (updateClient.IsChanged(c => c.ShowBalanceWarningPage))
									updateClient.CreareAppeal("Отключена страница Warning, клиент внес платеж", AppealType.Statistic);
							}
							foreach (var clientService in updateClient.ClientServices.ToList()) {
								clientService.PaymentProcessed();
							}
						}
						ProcessBonusesForFirstPayment(payment, session);
					}

					//Обработка платежей юриков
					if (updateClient.LawyerPerson != null) {
						updateClient.LawyerPerson.Balance += Convert.ToDecimal(payment.Sum);
						payment.BillingAccount = true;
						//Разблокировка при положительном балансе
						if (updateClient.LawyerPerson.Balance >= 0)
						{
							updateClient.SetStatus(StatusType.Worked,session);
							updateClient.CreareAppeal("Клиент разблокирован", AppealType.Statistic);
						}
					}
				}
			});
			WithTransaction(session => {
				var writeoffs = session.Query<UserWriteOff>().Where(w => !w.BillingAccount && w.Client != null);
				foreach (var userWriteOff in writeoffs) {
					var client = userWriteOff.Client;
					if (client.PhysicalClient != null) {
						var physicalClient = client.PhysicalClient;
						physicalClient.WriteOff(userWriteOff.Sum);
					}
					if (client.LawyerPerson != null) {
						var lawyerPerson = client.LawyerPerson;
						lawyerPerson.Balance -= userWriteOff.Sum;
					}
					userWriteOff.BillingAccount = true;
				}
			});
			WithTransaction(session => {
				var clients = session.Query<Client>()
					.Where(c => c.PhysicalClient != null && c.Disabled && c.AutoUnblocked)
					.ToList()
					.Where(c => c.PhysicalClient.Balance > c.GetPriceIgnoreDisabled() * c.PercentBalance)
					.ToList();
				foreach (var client in clients) {
					client.Enable();
					if (client.IsChanged(c => c.ShowBalanceWarningPage))
						client.CreareAppeal("Отключена страница Warning, клиент разблокирован", AppealType.Statistic);
					if (client.IsChanged(c => c.Disabled))
						client.CreareAppeal("Клиент разблокирован", AppealType.Statistic);
					SmsHelper.DeleteNoSendingMessages(client);
				}
				var lawyerPersons = session.Query<Client>().Where(c => c.LawyerPerson != null);
				foreach (var client in lawyerPersons) {
					if (client.NeedShowWarningForLawyer()) {
						if (client.WhenShowWarning == null ||
						    (SystemTime.Now() - client.WhenShowWarning.Value).TotalHours >= 3) {
							client.ShowBalanceWarningPage = true;
							client.WhenShowWarning = SystemTime.Now();
							if (!client.SendEmailNotification)
								client.SendEmailNotification = EmailNotificationSender.SendLawyerPersonNotification(client);
							if (client.IsChanged(c => c.ShowBalanceWarningPage))
								client.CreareAppeal("Включена страница Warning, клиент имеет низкий баланс", AppealType.Statistic);
						}
					}
					else {
						client.ShowBalanceWarningPage = false;
						client.SendEmailNotification = false;
						client.WhenShowWarning = null;
						if (client.IsChanged(c => c.ShowBalanceWarningPage))
							client.CreareAppeal("Отключена страница Warning", AppealType.Statistic);
					}
				}
				foreach (var assignedservice in session.Query<ClientService>()) {
					assignedservice.TryDeactivate();
				}
			});
		}

		/// <summary>
		/// Начисляет бонусы за первый платеж
		/// </summary>
		/// <param name="payment">Платеж</param>
		/// <param name="session">Сессия базы данных</param>
		private void ProcessBonusesForFirstPayment(Payment payment, ISession session)
		{
			var client = payment.Client;
			var firstPayment = client.Payments.Count == 1;
			var correctSum = payment.Sum >= client.PhysicalClient.Tariff.Price;
			var correctPlan = client.PhysicalClient.Tariff.Id != 77;
			var str = ConfigurationManager.AppSettings["ProcessFirstPaymentBonus"];
			var processBonus = str != null;
			if (processBonus && correctSum && correctPlan && firstPayment)
			{
				var p = new Payment(client, client.PhysicalClient.Tariff.Price);
				p.Virtual = true;
				session.Save(p);
				var appeal = client.CreareAppeal("Был зачислен бонус за первый платеж в размере " + client.PhysicalClient.Tariff.Price + " Рублей");
				session.Save(appeal);
				var message = "Вам начислен бонус в размере " + client.PhysicalClient.Tariff.Price + " рублей.Благодарим за сотрудничество";
				var sms = SmsMessage.TryCreate(client, message, DateTime.Now.AddMinutes(1));
				if (sms != null)
					session.Save(sms);
			}
		}

		/// <summary>
		/// Списания абоненской платы
		/// </summary>
		public virtual void ProcessWriteoffs()
		{
			var errorCount = 0;
			using (new SessionScope()) {
				ArHelper.WithSession(s => {
					var settings = s.Query<InternetSettings>().First();
					settings.LastStartFail = true;
					s.Save(settings);

					Messages = new List<SmsMessage>();
					_saleSettings = s.Query<SaleSettings>().First();
				});
			}

			ProcessAll(WriteOffFromPhysicalClient,
				s => s.Query<Client>().Where(c => c.PhysicalClient != null && !c.PaidDay),
				ref errorCount);

			ProcessAll(WriteOffFromLawyerPerson,
				s => s.Query<Client>().Where(c => c.LawyerPerson != null && !c.PaidDay),
				ref errorCount);

			WithTransaction(s => {
				var agentSettings = AgentTariff.GetPriceForAction(AgentActions.AgentPayIndex);
				var needToAgentSum = AgentTariff.GetPriceForAction(AgentActions.WorkedClient);
				var bonusesClients = s.Query<Client>().Where(c =>
					c.Request != null &&
					!c.Request.PaidBonus &&
					c.Request.Registrator != null &&
					c.BeginWork != null)
					.ToList();
				foreach (var client in bonusesClients) {
					if (client.Payments.Sum(p => p.Sum) >= needToAgentSum * agentSettings) {
						var request = client.Request;
						request.PaidBonus = true;
						s.Save(request);
						s.Save(new PaymentsForAgent {
							Action = AgentTariff.GetAction(AgentActions.WorkedClient),
							Agent = request.Registrator,
							RegistrationDate = SystemTime.Now(),
							Sum = needToAgentSum,
							Comment = string.Format("Клиент {0} начал работать (Заявка №{1})", client.Id, client.Request.Id)
						});
					}
				}

				var friendBonusRequests = s.Query<Request>().Where(r =>
					r.Client != null &&
					r.FriendThisClient != null &&
					!r.PaidFriendBonus &&
					r.Client.BeginWork != null)
					.ToList();
				foreach (var friendBonusRequest in friendBonusRequests) {
					if (friendBonusRequest.Client.HavePaymentToStart()) {
						s.Save(new Payment(friendBonusRequest.FriendThisClient, 250) {
							RecievedOn = SystemTime.Now(),
							Virtual = true,
							Comment = string.Format("Подключи друга {0}", friendBonusRequest.FriendThisClient.Id)
						});
						friendBonusRequest.PaidFriendBonus = true;
						s.Save(friendBonusRequest);
					}
				}
			});
			WithTransaction(s => {
				var settings = s.Query<InternetSettings>().First();
				settings.LastStartFail = errorCount > 0;
				settings.Save();
				s.Save(settings);
			});
		}

		private void ProcessAll(Action<ISession, Client> action, Func<ISession, IQueryable<Client>> query, ref int errorCount)
		{
			var ids = new List<uint>();
			WithTransaction(session => { ids = query(session).Select(c => c.Id).ToList(); });

			foreach (var id in ids) {
				try {
					WithTransaction(session => {
						var client = session.Load<Client>(id);
						action(session, client);
						client.ClientServices.ToArray().Each(s => s.WriteOffProcessed());
						client.PaidDay = true;
						session.Save(client);
					});
				}
				catch (Exception ex) {
					errorCount++;
					_log.Error(string.Format("Ошибка при обработке клиента {0}", id), ex);
				}
			}
		}

		/// <summary>
		/// Списание абоненской платы с физического клиента
		/// </summary>
		/// <param name="session">Сессия бд</param>
		/// <param name="client">Объект клиента</param>
		private void WriteOffFromPhysicalClient(ISession session, Client client)
		{
			if (_saleSettings.IsRepairExpaired(client)) {
				client.SetStatus(StatusType.Worked, session);
			}

			//Отсылка инфы о просроченных заявках
			SendExpiredServiceRequestEmails(client);

			var phisicalClient = client.PhysicalClient;
			var balance = phisicalClient.Balance;
			if (balance >= 0 && !client.Disabled && client.RatedPeriodDate.GetValueOrDefault() != DateTime.MinValue) {

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

			//Обработка списаний с клиента
			if (!client.PaidDay && client.RatedPeriodDate.GetValueOrDefault() != DateTime.MinValue && client.GetSumForRegularWriteOff() > 0) {
				if (client.StartNoBlock == null)
					client.StartNoBlock = SystemTime.Now();

				var writeOff = phisicalClient.WriteOff(client.GetSumForRegularWriteOff());
				if (writeOff != null)
					session.Save(writeOff);
				session.Save(phisicalClient);

				//Отсылаем смс если клиенту осталось работать 2 дня или меньше
				if (client.ShouldNotifyOnLowBalance()) {
					var message = string.Format("Ваш баланс {0:C} {1:d} доступ в сеть будет заблокирован.",
						client.PhysicalClient.Balance,
						client.GetPossibleBlockDate());
					var now = SystemTime.Now();
					DateTime shouldBeSendDate;
					if (now.Hour < 22)
						shouldBeSendDate = new DateTime(now.Year, now.Month, now.Day, 12, 0, 0);
					else {
						shouldBeSendDate = SystemTime.Now().Date.AddDays(1).AddHours(12);
					}
					var sms = SmsMessage.TryCreate(client, message, shouldBeSendDate);
					if (sms != null) {
						session.Save(sms);
						Messages.Add(sms);
					}
				}

				//Обработка отображения предупреждения о балансе
				if (client.NeedShowWarning(client.GetSumForRegularWriteOff())) {
					client.ShowBalanceWarningPage = true;
					if (client.IsChanged(c => c.ShowBalanceWarningPage))
						if (client.ShowWarningBecauseNoPassport())
							client.CreareAppeal("Включена страница Warning, клиент не имеет паспортных данных", AppealType.Statistic);
						else {
							client.CreareAppeal("Включена страница Warning, клиент имеет низкий баланс", AppealType.Statistic);
						}
				}
				else {
					client.ShowBalanceWarningPage = false;
					if (client.IsChanged(c => c.ShowBalanceWarningPage))
						client.CreareAppeal("Отключена страница Warning", AppealType.Statistic);
				}
			} //конец обработки списаний

			//Обработка блокировок
			if (client.CanBlock()) {
				client.SetStatus(Status.Get(StatusType.NoWorked, session));
				if (client.IsChanged(c => c.Disabled))
					client.CreareAppeal("Клиент был заблокирован", AppealType.Statistic);
			}

			//назначаем или переназначаем бесплатные блокировочные дни
			if ((client.YearCycleDate == null && client.BeginWork != null) || (SystemTime.Now().Date >= client.YearCycleDate.Value.AddYears(1).Date)) {
				client.FreeBlockDays = _saleSettings.FreeDaysVoluntaryBlocking;
				client.YearCycleDate = SystemTime.Now();
			}
		}

		/// <summary>
		/// Отправляем письма о просроченных заявках техподдержке
		/// </summary>
		/// <param name="client">Клиент</param>
		private void SendExpiredServiceRequestEmails(Client client)
		{
			foreach (var request in client.ServiceRequests) {
				if (request.Status == ServiceRequestStatus.Close || request.Status == ServiceRequestStatus.Cancel)
					continue;
				if ((SystemTime.Now() - request.RegDate).TotalDays < 3)
					continue;

				var address = ConfigurationManager.AppSettings["BlockForRepairNotificationMail"];
				if (address == null)
					throw new Exception("Параметр BlockForRepairNotificationMail должен быть задан в config");
				var mailer = new Mailer(new FolderSender(ConfigurationManager.AppSettings["SmtpServer"]));

				var regionName = "неизвестно";
				var region = client.GetRegion();

				//на 21.11.14 в базе бывают люди без регионов. В основном в белгороде.
				if (region != null)
					regionName = region.Name;
				if (client.PhysicalClient != null && client.PhysicalClient.City != null)
					regionName = client.PhysicalClient.City;

				var textMessage = "Срок исполнения сервисной заявки #" + request.Id + " (" + regionName + ") истек";
				mailer.SendText("internet@ivrn.net", address, textMessage, textMessage);
			}
		}

		/// <summary>
		/// Списания с юр. лиц
		/// </summary>
		/// <param name="session">Сессия базы данных</param>
		/// <param name="client">Клиент</param>
		private static void WriteOffFromLawyerPerson(ISession session, Client client)
		{
			var person = client.LawyerPerson;
			var writeoffs = client.LawyerPerson.Calculate(SystemTime.Today());
			person.Balance -= writeoffs.Sum(w => w.Sum);
			session.Save(person);
			session.SaveEach(writeoffs);

			if (client.CanBlock()) {
				client.SetStatus(Status.Get(StatusType.NoWorked, session));
				if (client.IsChanged(c => c.Disabled))
					client.CreareAppeal("Клиент был заблокирован в связи с отрицательным балансом", AppealType.Statistic);
			}
		}
	}
}