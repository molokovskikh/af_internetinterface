using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Common.MySql;
using Common.NHibernate;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;
using log4net;
using log4net.Config;

namespace Billing
{
	public class MainBilling
	{
		private readonly ILog _log = LogManager.GetLogger(typeof(MainBilling));

		private Mutex _mutex = new Mutex();
		private SaleSettings _saleSettings;

		public List<SmsMessage> Messages;

		// Основные тарифные планы (для начисления бонуса при первом платеже)
		public uint[] FirstPaymentBonusTariffIds = {
			45, // "Популярный"
			49, // "Оптимальный"
			81, // "Максимальный"
			85 //Народный
		};

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

		// вспомогательная
		private void WithTransaction(Action<ISession> action)
		{
			using (var transaction = new TransactionScope(OnDispose.Rollback)) {
				ArHelper.WithSession(action);
				transaction.VoteCommit();
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

							//Возможность сервисам отреагировать на платеж
							foreach (var clientService in updateClient.ClientServices.ToList())
								clientService.PaymentProcessed();
						}
						ProcessBonusesForFirstPayment(payment, session);
					}

					//Обработка платежей юриков
					if (updateClient.LawyerPerson != null) {
						updateClient.LawyerPerson.Balance += Convert.ToDecimal(payment.Sum);
						payment.BillingAccount = true;
						//Разблокировка при положительном балансе
						if (updateClient.LawyerPerson.Balance >= 0) {
							updateClient.SetStatus(StatusType.Worked, session);
							updateClient.CreareAppeal("Клиент разблокирован", AppealType.Statistic);
						}
						if (payment.Agent != null && payment.Agent.Name.IndexOf("Сбербанк") != -1) {
							//отправка сообщения в бухгалтерию о платеже юрика через Сбербанк 
							StringBuilder messageText = new StringBuilder();
							messageText.Append(string.Format("Лицевой счет юр. лица: {0}, Название юр. лица: {1}, Сумма платежа: {2} руб., Дата платежа: {3}.",
								updateClient.Id, updateClient.Name, payment.Sum, payment.PaidOn));
							EmailNotificationSender.Send(messageText, "Платеж юр. лица через Сбербанк");
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
				//Пытаемся удалить сервисы, которые отработали свое
				var services = session.Query<ClientService>().ToList();
				foreach (var assignedservice in services) {
					if (assignedservice.IsActivated) {
						// вызов события у сервиса по таймеру
						assignedservice.Service.OnTimer(session, assignedservice);
					}
					assignedservice.TryDeactivate();
				}
			});
		}

		private void ActivateServices(ISession session)
		{
			var services = session.Query<ClientService>().Where(s => !s.IsActivated);
			foreach (var service in services) {
				service.TryActivate();
				session.Save(service);
			}
		}

		/// <summary>
		/// Начисляет бонусы за первый платеж (ProcessPayments)
		/// </summary>
		/// <param name="payment">Платеж</param>
		/// <param name="session">Сессия базы данных</param>
		private void ProcessBonusesForFirstPayment(Payment payment, ISession session)
		{
			var client = payment.Client;
			var str = ConfigurationManager.AppSettings["ProcessFirstPaymentBonus"];
			var processBonus = (str == "true" || str == "1");

			var clientPayments = client.Payments.Where(p => p.BillingAccount).ToList();
			var firstPayment = clientPayments.Count == 1;
			var correctSum = payment.Sum >= client.PhysicalClient.Tariff.Price;
			// Обработка случая, когда ровно 2 первых платежа пришли за 24 ч. и их сумма >= цены тарифа
			if (!firstPayment && !correctSum) {
				var dateDiff = (clientPayments.Count == 2) ? (clientPayments[1].PaidOn - clientPayments[0].PaidOn) : TimeSpan.Zero;
				if (dateDiff != TimeSpan.Zero && dateDiff.Duration() <= TimeSpan.FromDays(1d)) {
					firstPayment = true;
					correctSum = (clientPayments[0].Sum + clientPayments[1].Sum) >= client.PhysicalClient.Tariff.Price;
				}
			}

			var correctPlan = FirstPaymentBonusTariffIds.Contains(client.PhysicalClient.Tariff.Id);
			if (processBonus && firstPayment && correctSum && correctPlan) {
				var bonusPayment = new Payment(client, client.PhysicalClient.Tariff.Price) {
					Virtual = true,
					Comment = "Месяц в подарок"
				};
				session.Save(bonusPayment);

				var appeal = client.CreareAppeal("Был зачислен бонус за первый платеж в размере " + bonusPayment.Sum + " Рублей");
				session.Save(appeal);
				var message = "Вам начислен бонус в размере " + bonusPayment.Sum + " рублей.Благодарим за сотрудничество";
				var sms = SmsMessage.TryCreate(client, message, DateTime.Now.AddMinutes(1));
				if (sms != null)
					session.Save(sms);
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
						// Reset
						ArHelper.WithSession(
							s => s.CreateSQLQuery(@"update internet.Clients c
												set c.PaidDay = false;
												update internet.InternetSettings s
												set s.LastStartFail = true;")
								.ExecuteUpdate());

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

		/// <summary>
		/// Списания абоненской платы
		/// </summary>
		public virtual void ProcessWriteoffs()
		{
			// обнуление кол-ва ошибок
			var errorCount = 0;

			// выставление флага проведения списаний при ошибках в true
			LastStartFailChangeForTrue();

			// списание у физ. лиц
			ProcessAll(WriteOffFromPhysicalPerson,
				s => s.Query<Client>().Where(c => c.PhysicalClient != null && !c.PaidDay),
				ref errorCount);

			// списание у юр. лиц
			ProcessAll(WriteOffFromLawyerPerson,
				s => s.Query<Client>().Where(c => c.LawyerPerson != null && !c.PaidDay),
				ref errorCount);

			WithTransaction(s => {
				// учет оплаты работы агентов
				ProcessPaymentsForAgent(s);

				// учет бонусов клиентов
				ProcessFriendBonuses(s);
			});

			// при наличии ошибок выставление флага проведения списаний
			LastStartFailChangeForErrorsPresent(errorCount);
		}


		// вспомогательная 
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
		/// Принудительное выставление флага LastStartFail, для выполнения списаний
		/// </summary>
		private void LastStartFailChangeForTrue()
		{
			using (new SessionScope()) {
				ArHelper.WithSession(s => {
					var settings = s.Query<InternetSettings>().First();
					settings.LastStartFail = true;
					s.Save(settings);

					Messages = new List<SmsMessage>();
					_saleSettings = s.Query<SaleSettings>().First();
				});
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

		/// <summary>
		/// Списание абоненской платы с физического клиента
		/// </summary>
		/// <param name="session">Сессия базы данных</param>
		/// <param name="client">Клиент</param>
		private void WriteOffFromPhysicalPerson(ISession session, Client client)
		{
			//Сброс статуса "Заблокирован - Восстановление работы" у клиента
			if (_saleSettings.IsRepairExpaired(client)) {
				client.SetStatus(StatusType.Worked, session);
			}

			//Отсылка инфы о просроченных заявках
			AddExpiredServiceRequestNoteToRedmine(session, client);

			//Обновление расчетного периода для подключенного клиента
			UpdateRatedPeriodDate(client);

			//Обновление (назначение заново) скидки клиента
			UpdateDiscount(client);

			//Обработка списаний с клиента
			if (!client.PaidDay && client.RatedPeriodDate.GetValueOrDefault() != DateTime.MinValue && client.GetSumForRegularWriteOff() > 0) {
				if (client.StartNoBlock == null)
					client.StartNoBlock = SystemTime.Now();

				var writeOff = client.PhysicalClient.WriteOff(client.GetSumForRegularWriteOff());
				if (writeOff != null)
					session.Save(writeOff);
				session.Save(client.PhysicalClient);

				//Отсылаем смс если клиенту осталось работать 2 дня или меньше
				SendWarningSMS(session, client);

				//Обработка отображения предупреждения о балансе
				UpdateWarnings(client);
			}

			//Обработка аренды оборудования 
			try {
				ProcessHardwareRent(session, client);
			}
			catch (Exception e) {
				_log.Error(string.Format("Не удалось обработать аренду оборудования для клиента {0}", client.Id), e);
			}

			//Обработка блокировок
			ProcessBlock(session, client);

			//назначаем или переназначаем бесплатные блокировочные дни
			UpdateYearCycleDate(client);
		}

		/// <summary>
		/// Создаем заметку в Redmine о просроченной заявке для техподдержки (для физика)
		/// </summary>
		/// <param name="session">Сессия для работы с БД</param>
		/// <param name="client">Клиент</param>
		private void AddExpiredServiceRequestNoteToRedmine(ISession session, Client client)
		{
			foreach (var request in client.ServiceRequests) {
				//Если заявка отменена или закрыта, то она не может быть просроченна
				if (request.Status == ServiceRequestStatus.Close || request.Status == ServiceRequestStatus.Cancel)
					continue;

				//Уведомления отсылаются через 3 дня
				if ((SystemTime.Now() - request.ModificationDate).TotalDays < 3)
					continue;

				var cityName = "Неизвестный";
				var region = client.GetRegion();

				//на 21.11.14 в базе бывают люди без регионов. В основном в белгороде.
				if (region != null && region.City != null)
					cityName = region.City.Name;

				var issue = ConfigurationManager.AppSettings[cityName + "RedmineTask"];
				var issueId = string.IsNullOrEmpty(issue) ? 0u : Convert.ToUInt32(issue);
				if (issueId == 0u) {
					issue = ConfigurationManager.AppSettings["НеизвестныйRedmineTask"];
					issueId = string.IsNullOrEmpty(issue) ? 0u : Convert.ToUInt32(issue);
				}

				try {
					var rmUser = session.Query<RedmineUser>().ToList().FirstOrDefault(ru => ru.Login == "redmine");
					var textMessage = "Срок исполнения сервисной заявки №" + request.Id + " истек";
					var rmTaskNote = new RedmineJournal {
						RedmineIssueId = issueId,
						JournalType = "Issue",
						UserId = (rmUser != null) ? rmUser.Id : 0,
						Notes = textMessage,
						CreateDate = SystemTime.Now(),
						IsPrivate = false
					};
					session.Save(rmTaskNote);
				}
				catch (Exception ex) {
					_log.Error("Ошибка при создании заметки в RedMine по истекшей сервисной заявке", ex);
				}
			}
		}

		/// <summary>
		/// Оплата работы агентов
		/// </summary>
		private void ProcessPaymentsForAgent(ISession session)
		{
			var agentSettings = AgentTariff.GetPriceForAction(AgentActions.AgentPayIndex);
			var needToAgentSum = AgentTariff.GetPriceForAction(AgentActions.WorkedClient);
			var bonusesClients = session.Query<Client>().Where(c =>
				c.Request != null &&
				!c.Request.PaidBonus &&
				c.Request.Registrator != null &&
				c.BeginWork != null)
				.ToList();
			foreach (var client in bonusesClients) {
				if (client.Payments.Sum(p => p.Sum) >= needToAgentSum * agentSettings) {
					var request = client.Request;
					request.PaidBonus = true;
					session.Save(request);
					session.Save(new PaymentsForAgent {
						Action = AgentTariff.GetAction(AgentActions.WorkedClient),
						Agent = request.Registrator,
						RegistrationDate = SystemTime.Now(),
						Sum = needToAgentSum,
						Comment = string.Format("Клиент {0} начал работать (Заявка №{1})", client.Id, client.Request.Id)
					});
				}
			}
		}

		/// <summary>
		/// Назначение бонусов
		/// </summary>
		private void ProcessFriendBonuses(ISession session)
		{
			var friendBonusRequests = session.Query<Request>().Where(r =>
				r.Client != null &&
				r.FriendThisClient != null &&
				!r.PaidFriendBonus &&
				r.Client.BeginWork != null)
				.ToList();
			foreach (var friendBonusRequest in friendBonusRequests) {
				if (friendBonusRequest.Client.HavePaymentToStart()) {
					session.Save(new Payment(friendBonusRequest.FriendThisClient, 250) {
						RecievedOn = SystemTime.Now(),
						Virtual = true,
						Comment = string.Format("Подключи друга {0}", friendBonusRequest.FriendThisClient.Id)
					});
					friendBonusRequest.PaidFriendBonus = true;
					session.Save(friendBonusRequest);
				}
			}
		}

		/// <summary>
		/// Выставление флага LastStartFail в зависимости от наличия ошибок, для выполнения списаний
		/// </summary>
		private void LastStartFailChangeForErrorsPresent(int errorCount)
		{
			WithTransaction(s => {
				var settings = s.Query<InternetSettings>().First();
				settings.LastStartFail = errorCount > 0;
				settings.Save();
				s.Save(settings);
			});
		}

		/// <summary>
		/// Отправка предупредительного SMS  (для физика)
		/// </summary>
		private void SendWarningSMS(ISession session, Client client)
		{
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
		}

		/// <summary>
		/// Обновление календарного периода начисления абонентской платы (для физика)
		/// </summary>
		private void UpdateRatedPeriodDate(Client client)
		{
			var phisicalClient = client.PhysicalClient;
			if (phisicalClient.Balance >= 0 && !client.Disabled && client.RatedPeriodDate.GetValueOrDefault() != DateTime.MinValue) {
				var dtNow = SystemTime.Now();
				// Если дата расчетного периода (с поправкой на долговые дни) ровно на 1 месяц позади текущей даты
				if ((client.RatedPeriodDate.Value.AddMonths(1).Date - dtNow.Date).Days == -client.DebtDays) {
					var dtFrom = client.RatedPeriodDate.Value;
					var dtTo = dtNow;
					// Фактически обнулить кол-во долговых дней у клиента
					client.DebtDays += dtFrom.Day - dtTo.Day;
					var thisMonth = dtNow.Month;
					// Задать расчетный период с поправкой на долговые дни и на текущий месяц
					client.RatedPeriodDate = dtNow.AddDays(client.DebtDays);
					while (client.RatedPeriodDate.Value.Month != thisMonth) {
						client.RatedPeriodDate = client.RatedPeriodDate.Value.AddDays(-1);
					}
				}
				else if ((client.RatedPeriodDate.Value.AddMonths(1).Date - dtNow.Date).TotalDays < -client.DebtDays) {
					// Задать расчетный период с поправкой на долговые дни
					client.RatedPeriodDate = dtNow.AddDays(client.DebtDays);
					client.DebtDays = 0;
				}
			}
		}

		/// <summary>
		/// Обновить скидку, если задана дата StartNoBlock (для физика)
		/// </summary>
		private void UpdateDiscount(Client client)
		{
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
		}

		/// <summary>
		/// Предупреждение клиента о низком балансе (для физика)
		/// </summary>
		private void UpdateWarnings(Client client)
		{
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
		}

		/// <summary>
		/// Обработка аренды оборудования для клиента client (для физика)
		/// </summary>
		/// <param name="session">Сессия Nhibernate</param>
		/// <param name="client">Клиент, потенциально арендующий оборудование</param>
		private void ProcessHardwareRent(ISession session, Client client)
		{
			//Если нет активной аренды, то он нам и не нужен
			if (!client.HasActiveRentalHardware())
				return;

			//если клиент не заблокирован и не отключен, то он нас мало интересует
			var correctStatus = client.Status.Type == StatusType.NoWorked || client.Status.Type == StatusType.Dissolved;
			if (!correctStatus)
				return;

			//Кидаем исключение, так как когда у клиента нет данных о последней смене статуса - это как минимум странно
			//В идеале не должно никогда срабатывать 
			if (client.StatusChangedOn == null)
				throw new Exception(string.Format("У клиента {0} нет даты последней смены статуса", client.Id));

			//Создание задачи в Redmine и обработка списаний
			CreateRentalHardwareWriteOffs(session, client);
		}

		// вспомогательная
		/// <summary>
		/// Создание записи в редмайне для аренды оборудования
		/// </summary>
		/// <param name="session">Сессия Nhibernate</param>
		/// <param name="client">Клиент, потенциально арендующий оборудование</param>
		private void CreateRentalHardwareRedmineIssue(ISession session, Client client)
		{
			//Проверяем, существует ли уже активная задача по этому клиенту
			var fio = string.Format("{0} {1} {2}", client.PhysicalClient.Surname, client.PhysicalClient.Name, client.PhysicalClient.Patronymic);
			var region = client.PhysicalClient.HouseObj != null ? client.PhysicalClient.HouseObj.Region.Name : "регион не установлен";
			var redmineIssueName = string.Format("Возврат оборудования, ЛС {0}, {1}({2})", client.Id.ToString("D5"), fio, region);
			//АККУРАТНО, НЕЛЬЗЯ МЕНЯТЬ "Возврат оборудования, ЛС {0}," иначе задачи не будут находиться
			var clientIssues = session.Query<RedmineIssue>().Where(ri => ri.subject.Contains(string.Format("Возврат оборудования, ЛС {0}", client.Id))).ToList();
			var hasRedmineIssue = clientIssues.Any(i => i.status_id != 5);
			if (hasRedmineIssue)
				return;

			var description = new StringBuilder();
			description.AppendLine("Баланс клиента, равный " + client.Balance.ToString("F2") + " р., не пополнялся более 30 дней.");
			description.AppendLine(String.Format("http://stat.ivrn.net/ii/UserInfo/ShowPhysicalClient?filter.ClientCode={0}", client.Id));

			description.AppendLine();
			var redmineIssue = new RedmineIssue {
				project_id = 67, // Проект "Координация"
				status_id = 1, // Статус "Новый"
				assigned_to_id = 279, //Группа координаторы
				created_on = SystemTime.Now(),
				due_date = SystemTime.Today().AddDays(3),
				subject = redmineIssueName,
				description = description.ToString()
			};
			session.Save(redmineIssue);
		}

		// вспомогательная
		/// <summary>
		/// Обработка списаний за аренду оборудования
		/// </summary>
		/// <param name="session">Сессия Nhibernate</param>
		/// <param name="client">Клиент, потенциально арендующий оборудование</param>
		private void CreateRentalHardwareWriteOffs(ISession session, Client client)
		{
			// Обработка списаний за аренду конкретного оборудования
			var phisicalClient = client.PhysicalClient;
			//Смещение для выравнивания списаний по времени
			var count = 1;
			foreach (var clientHardware in client.RentalHardwareList) {
				//Неактивные аренды не сохраняет
				if (!clientHardware.IsActive)
					return;

				// Если пустая дата начала аренды, деактивировать услугу
				if (clientHardware.GiveDate == null) {
					var msg = clientHardware.Deactivate();
					session.Update(clientHardware);
					var appeal = client.CreareAppeal(msg, AppealType.System, false);
					session.Save(appeal);
				}
				else if (client.Status.Type == StatusType.Dissolved || (client.Status.Type == StatusType.NoWorked && (SystemTime.Now() - client.StatusChangedOn) > TimeSpan.FromDays(clientHardware.Hardware.FreeDays))) {
					//Создаем задачу в РМ
					CreateRentalHardwareRedmineIssue(session, client);

					// Если с даты изменения статуса клиента прошло > 30 дней, списать ежедневную плату за аренду
					var sum = client.GetPriceForHardware(clientHardware.Hardware);
					if (sum <= 0m) // В случае 0-й платы за аренду не создавать списание
						continue;

					var sumDiff = Math.Min(phisicalClient.MoneyBalance - sum, 0);
					var virtualWriteoff = Math.Min(Math.Abs(sumDiff), phisicalClient.VirtualBalance);
					var moneyWriteoff = sum - virtualWriteoff;
					var newWriteoff = new WriteOff {
						Client = client,
						WriteOffDate = SystemTime.Now().AddSeconds(count++), //списания должны быть выровнены по дате, а при одинаковых датах, возникает путаница
						WriteOffSum = Math.Round(sum, 2),
						MoneySum = Math.Round(moneyWriteoff, 2),
						VirtualSum = Math.Round(virtualWriteoff, 2),
						Sale = client.Sale,
						BeforeWriteOffBalance = client.Balance,
						Comment = clientHardware.Hardware.Name + ": ежедневная плата за аренду"
					};
					session.Save(newWriteoff);

					// сохранение измененных данных у физ. клиента
					phisicalClient.Balance -= newWriteoff.WriteOffSum;
					phisicalClient.VirtualBalance -= newWriteoff.VirtualSum;
					phisicalClient.MoneyBalance -= newWriteoff.MoneySum;
				}
			}
		}

		/// <summary>
		/// Обработка блокировок (для физика)
		/// </summary>
		private void ProcessBlock(ISession session, Client client)
		{
			if (client.CanBlock()) {
				client.SetStatus(Status.Get(StatusType.NoWorked, session));
				if (client.IsChanged(c => c.Disabled))
					client.CreareAppeal("Клиент был заблокирован", AppealType.Statistic);
			}
		}

		/// <summary>
		/// Обновление даты годового цикла (для физика)
		/// </summary>
		private void UpdateYearCycleDate(Client client)
		{
			if ((client.YearCycleDate == null && client.BeginWork != null) || (SystemTime.Now().Date >= client.YearCycleDate.Value.AddYears(1).Date)) {
				client.FreeBlockDays = _saleSettings.FreeDaysVoluntaryBlocking;
				client.YearCycleDate = SystemTime.Now();
			}
		}
	}
}