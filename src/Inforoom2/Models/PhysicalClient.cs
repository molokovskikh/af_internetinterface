using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Common.Tools;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping;
using NHibernate.Mapping.Attributes;
using NHibernate.Util;
using NHibernate.Validator.Constraints;
using System.Net;
using System.Web;
using Inforoom2.Components;
using Inforoom2.Models.Services;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель физического клиента
	/// </summary>
	[Class(0, Table = "PhysicalClients", Schema = "internet", NameType = typeof(PhysicalClient)), Description("Клиент")]
	public class PhysicalClient : BaseModel, ILogAppeal, IClientExpander
	{
		[Property, Description("Пароль физического клиента")]
		public virtual string Password { get; set; }

		[ManyToOne(Column = "_Address", Cascade = "save-update"), NotNull(Message = "Адрес указан не полностью!"),
		 Description("Адрес")]
		public virtual Address Address { get; set; }

		[Property(Column = "_Email", NotNull = true), Description("Электронаня почта физического клиента")]
		public virtual string Email
		{
			get
			{
				if (Client != null && Client.Contacts != null) {
					var contactMail = this.Client.Contacts.FirstOrDefault(s => s.Type == ContactType.Email);
					return contactMail != null ? contactMail.ContactString : "";
				}
				return "";
			}
			set
			{
				if (Client != null && Client.Contacts != null) {
					var contactMail = this.Client.Contacts.FirstOrDefault(s => s.Type == ContactType.Email);
					if (contactMail != null) {
						contactMail.ContactString = value;
					}
				}
			}
		}

		public PhysicalClient()
		{
			LastTimePlanChanged = SystemTime.Now();
		}

		[ManyToOne(Column = "Tariff"), NotNull(Message = "Выберите тариф"), Description("Тариф")]
		public virtual Plan Plan { get; set; }

		[Property(Column = "_LastTimePlanChanged")]
		public virtual DateTime LastTimePlanChanged { get; set; }

		[Property, Description("Общий баланс на счете клиента,который он может потратить")]
		public virtual decimal Balance { get; set; }

		[Property, Description("Сумма,которая взята с клиента за подключение(для клиентов 0,у частного сектора 5000)")]
		public virtual decimal ConnectSum { get; set; }

		[Property, Description("Сумма внесенная сотрудниками клиенту")]
		public virtual decimal VirtualBalance { get; set; }

		[Property, Description("Клиент проверен. устанавливается в админке, при редактировании клиента")]
		public virtual bool Checked { get; set; }

		[Property, Description("Реальные внесенные деньги клиентом на свой счет")]
		public virtual decimal MoneyBalance { get; set; }

		[Property(Column = "IdDocType"), Description("Документ, удостоверяющий личность")]
		public virtual CertificateType CertificateType { get; set; }

		[Property(Column = "IdDocName"), Description("Название документа, удостоверяющего личность")]
		public virtual string CertificateName { get; set; }

		[Property, Description("Номер паспорта")]
		public virtual string PassportNumber { get; set; }

		[Property, Description("Серия паспорта")]
		public virtual string PassportSeries { get; set; }

		[DataType(DataType.Date)]
		[Property, Description("Дата выдачи паспорта")]
		public virtual DateTime PassportDate { get; set; }

		[Property(Column = "RegistrationAdress"), Description("Адрес регистрации")]
		public virtual string RegistrationAddress { get; set; }

		[Property(Column = "WhoGivePassport"), Description("Кем выдан")]
		public virtual string PassportResidention { get; set; }

		[Property, Description("Номер абонента Ситилайн")]
		public virtual int? ExternalClientId { get; set; }

		[Property(Column = "_PhoneNumber", NotNull = true), Description("Номер телефона клиента")]
		public virtual string PhoneNumber
		{
			get
			{
				if (Client != null && Client.Contacts != null) {
					var contactPhone = this.Client.Contacts.FirstOrDefault(s => s.Type == ContactType.MobilePhone);
					return contactPhone != null ? contactPhone.ContactString : "";
				}
				return "";
			}
			set
			{
				if (Client != null && Client.Contacts != null) {
					var contactPhone = this.Client.Contacts.FirstOrDefault(s => s.Type == ContactType.MobilePhone);
					if (contactPhone != null) {
						contactPhone.ContactString = value;
					}
				}
			}
		}

		[Property(NotNull = true), NotNullNotEmpty(Message = "Введите имя"), Description("Имя")]
		public virtual string Name { get; set; }

		[Property(NotNull = true), NotNullNotEmpty(Message = "Введите фамилию"), Description("Фамилия")]
		public virtual string Surname { get; set; }

		[Property(NotNull = true), NotNullNotEmpty(Message = "Введите отчество"), Description("Отчество")]
		public virtual string Patronymic { get; set; }

		[DataType(DataType.Date)]
		[Property(Column = "DateOfBirth"), Description("Дата рождения клиента")]
		public virtual DateTime BirthDate { get; set; }

		[OneToOne(PropertyRef = "PhysicalClient")]
		public virtual Client Client { get; set; }

		public virtual string SafeIdDocName
		{
			get
			{
				if (CertificateType == CertificateType.Passport)
					return "Паспорт";
				return CertificateName;
			}
		}

		public virtual string FullName
		{
			get { return Surname + " " + Name + " " + Patronymic; }
		}

		public virtual UserWriteOff RequestChangePlan(Plan planToSwitchOn)
		{
			var price = Plan.GetTransferPrice(planToSwitchOn);
			if (!IsEnoughBalance(price)) {
				return null;
			}
			return SwitchPlan(planToSwitchOn, price);
		}

		private UserWriteOff SwitchPlan(Plan planTo, decimal price)
		{
			var comment = string.Format("Изменение тарифа, старый '{0}' новый '{1}'", Plan.Name, planTo.Name);
			Plan = planTo;
			WriteOff(price);
			var writeOff = new UserWriteOff {
				Client = Client,
				Date = SystemTime.Now(),
				Sum = price,
				Comment = comment,
				IsProcessedByBilling = true
			};
			LastTimePlanChanged = SystemTime.Now();
			if (Client.Internet.ActivatedByUser)
				Client.Endpoints.Where(s=>!s.Disabled).ForEach(e => {
					e.SetStablePackgeId(Plan.PackageSpeed.PackageId);
				});
			return writeOff;
		}

		public virtual bool IsEnoughBalance(decimal sum)
		{
			if (sum < 0) {
				return false;
			}
			return Balance - sum > 0;
		}

		public virtual WriteOff WriteOff(decimal sum, bool writeoffVirtualFirst = false)
		{
			var writeoff = CalculateWriteoff(sum, writeoffVirtualFirst);

			if (writeoff == null)
				return null;

			Balance -= writeoff.WriteOffSum;
			VirtualBalance -= writeoff.VirtualSum;
			MoneyBalance -= writeoff.MoneySum;

			return writeoff;
		}


		public virtual DateTime? PlanChangerOnDate()
		{
			return PlanChanger.PlanchangerTimeOffDate(Client);
		}

		public virtual int? PlanChangeOnInDays()
		{
			var onDate = PlanChangerOnDate();
			if (!onDate.HasValue) {
				return null;
			}
			return (onDate.Value - SystemTime.Now()).Days;
		}

		public virtual bool IsPlanChangerOn()
		{
			return PlanChanger.IsPlanchangerTimeOff(Client);
		}

		public virtual bool HasClientAppeals(string message = "")
		{
			if (message != string.Empty) {
				return Client.ClientAppeals().Count(s => s.Message == message) > 0;
			}
			return Client.ClientAppeals().Count > 0;
		}

		public virtual bool HasNotShownClientAppeals(string message = "")
		{
			if (message != string.Empty) {
				return Client.ClientAppeals().Count(s => s.Message == message) > 0;
			}
			return Client.ClientAppealsNotShown().Count > 0;
		}

		public virtual WriteOff CalculateWriteoff(decimal sum, bool writeoffVirtualFirst = false)
		{
			if (sum <= 0)
				return null;

			decimal virtualWriteoff;
			decimal moneyWriteoff;

			if (writeoffVirtualFirst) {
				virtualWriteoff = Math.Min(sum, VirtualBalance);
			}
			else {
				virtualWriteoff = Math.Min(Math.Abs(Math.Min(MoneyBalance - sum, 0)), VirtualBalance);
			}
			moneyWriteoff = sum - virtualWriteoff;

			return new WriteOff {
				Client = Client,
				WriteOffDate = SystemTime.Now(),
				WriteOffSum = Math.Round(sum, 2),
				MoneySum = Math.Round(moneyWriteoff, 2),
				VirtualSum = Math.Round(virtualWriteoff, 2),
				Sale = Client.Discount,
				BeforeWriteOffBalance = Client.Balance
			};
		}


		///  Генерация пароля для пользователя
		///  *взято из старой админки////////////////////// 
		public static string GeneratePassword(PhysicalClient ph)
		{
			var availableChars = "23456789qwertyupasdfghjkzxcvbnmQWERTYUPASDFGHJKLZXCVBNM";
			var password = String.Empty;
			while (password.Length < 8) {
				int availableChars_elem = 0;

				var rngCsp = new RNGCryptoServiceProvider();
				var randomNumber = new byte[1];
				do {
					rngCsp.GetBytes(randomNumber);
				} while (!(randomNumber[0] < (availableChars.Length - 1) * (Byte.MaxValue / (availableChars.Length - 1))));

				availableChars_elem = (randomNumber[0] % (availableChars.Length - 1)) + 1;

				password += availableChars[availableChars_elem];
			}
			ph.Password = Md5.GetHash(password);
			return password;
		}

		//////////////////////////////////////////////////

		public virtual Client GetAppealClient(ISession session)
		{
			return session.Query<Client>().FirstOrDefault(s => s.PhysicalClient.Id == this.Id);
		}

		public virtual List<string> GetAppealFields()
		{
			return new List<string>() {
				"Name",
				"Surname",
				"Patronymic",
				"CertificateType",
				"CertificateName",
				"PassportSeries",
				"PassportNumber",
				"PassportDate",
				"PassportResidention",
				"RegistrationAddress",
				"Plan",
				"Checked",
				"Address"
			};
		}

		public virtual string GetAdditionalAppealInfo(string property, object oldPropertyValue, ISession session)
		{
			string message = "";
			// для свойства Tariff
			if (property == "Plan") {
				// получаем псевдоним из описания 
				property = this.Plan.GetDescription();
				var oldPlan = oldPropertyValue == null ? null : ((Plan)oldPropertyValue);
				var currentPlan = this.Plan;
				if (oldPlan != null) {
					message += property + " было: " + oldPlan.Name + " <br/>";
				}
				else {
					message += property + " было: " + "значение отсуствует <br/> ";
				}
				if (currentPlan != null) {
					message += property + " стало: " + currentPlan.Name + " <br/>";
				}
				else {
					message += property + " стало: " + "значение отсуствует <br/> ";
				}
			}
			return message;
		}

		public virtual object GetExtendedClient => this;

		public virtual IList<Contact> GetContacts()
		{
			return Client.Contacts;
		}

		public virtual string GetConnetionAddress()
		{
			return Address.GetStringForPrint();
		}

		public virtual string GetName()
		{
			return FullName;
		}

		public virtual DateTime? GetRegistrationDate()
		{
			return Client.CreationDate;
		}

		public virtual DateTime? GetDissolveDate()
		{
			return ((StatusType)Client.Status.Id) == StatusType.Dissolved ? Client.StatusChangedOn : null;
		}

		public virtual string GetPlan()
		{
			return $"{Plan.Name}, {Plan.Price} руб.";
		}

		public virtual decimal GetBalance()
		{
			return Balance;
		}

		public virtual StatusType GetStatus()
		{
			return ((StatusType)Client.Status.Id);
		}


		public virtual void UpdatePackageId(ClientEndpoint clientEndpoint)
		{
			if (clientEndpoint == null)
				return;
			if (Plan != null && Client.Internet.ActivatedByUser)
				clientEndpoint.SetStablePackgeId(Plan.PackageSpeed.PackageId);
			else
				clientEndpoint.SetStablePackgeId(null);
		}

		/// <summary>
		/// Добавление точки подключения клиенту
		/// </summary>
		/// <param name="dbSession">Хибер Сессия</param>
		/// <param name="endpointId">Id точки подключения</param>
		/// <param name="connectSum">сумма за подключение</param>
		/// <param name="connection">Параметры подключения</param>
		/// <param name="staticAddress">Статические адреса</param>
		/// <param name="errorMessage">Выходая ошибка</param>
		public virtual void SaveSwitchForClient(ISession dbSession, int endpointId, string connectSum,
			ConnectionHelper connection, StaticIp[] staticAddress, out string errorMessage, Employee employee)
		{
			var client = this.Client;
			//получаем базовые значения
			var settings = new SettingsHelper(dbSession);
			staticAddress = staticAddress ?? new StaticIp[0];
			var newFlag = false;
			var clientEntPoint = new ClientEndpoint();

			//если нет эндпоинта в БД, это новое подключение
			var endPoint = dbSession.Get<ClientEndpoint>(endpointId);
			if (endPoint != null)
				clientEntPoint = endPoint;
			else
				newFlag = true;


			//сохраняем начальные значения точки подключения
			var oldPort = clientEntPoint.Port;
			var oldSwitch = clientEntPoint.Switch;

			var nullFlag = false;
			if (!string.IsNullOrEmpty(connection.StaticIp)) {
				clientEntPoint.Ip = null;
				nullFlag = true;
			}
			errorMessage = connection.Validate(dbSession, true, endpointId);

			decimal _connectSum = -1;
			connectSum = connectSum.Replace(".", ",");
			var validateSum =
				!(!string.IsNullOrEmpty(connectSum) &&
				  (!decimal.TryParse(connectSum, out _connectSum) || (_connectSum <= 0 && client.PhysicalClient != null)));
			if (!validateSum)
				errorMessage = "Введена невалидная сумма. ";

			if (string.IsNullOrEmpty(connection.StaticIp) || nullFlag) {
				if (validateSum && string.IsNullOrEmpty(errorMessage) || validateSum &&
				    (oldSwitch != null && connection.Switch == oldSwitch.Id && connection.Port == oldPort.ToString())) {
					//обновляем/задаем поля точки подключения
					if (clientEntPoint.Ip == null && !string.IsNullOrEmpty(connection.StaticIp)) {
						decimal priceForIp = 0;
						var priceItem = dbSession.Query<Service>().FirstOrDefault(s => s.Id == Service.GetIdByType(typeof(FixedIp)));
						if (priceItem != null) {
							priceForIp = priceItem.Price;
						}
						dbSession.Save(new UserWriteOff(client, priceForIp,
							string.Format("Плата за фиксированный Ip адрес ({0})", connection.StaticIp)));
					}
					clientEntPoint.Client = client;
					clientEntPoint.Port = connection.GetPortNumber().Value;
					clientEntPoint.Pool = connection.GetPool(dbSession);
					clientEntPoint.Switch = connection.GetSwitch(dbSession);
					clientEntPoint.Monitoring = connection.Monitoring;

					//сохраняем значение фиксированного Ip для точки подкючения
					IPAddress address;
					if (connection.StaticIp != null && IPAddress.TryParse(connection.StaticIp, out address))
						clientEntPoint.Ip = address;
					else
						clientEntPoint.Ip = null;

					//если подключение новое, добавляем точку подключения клиенту и активируем необходимые сервисы по "базовым значениям"
					if (newFlag) {
						if (client.Endpoints.Count == 0) {
							clientEntPoint.IsEnabled = true;
							clientEntPoint.Disabled = false;
							clientEntPoint.SetStablePackgeId(Plan?.PackageSpeed?.PackageId);

							client.SetStatus(StatusType.Worked, dbSession);

							if (client.RatedPeriodDate == null && !client.Disabled) {
								client.RatedPeriodDate = SystemTime.Now();
							}
							if (client.WorkingStartDate == null && !client.Disabled) {
								client.WorkingStartDate = SystemTime.Now();
							}
							var internet = client.ClientServices.First(i => (ServiceType) i.Service.Id == ServiceType.Internet);
							internet.ActivateFor(client, dbSession);
							var iptv = client.ClientServices.First(i => (ServiceType) i.Service.Id == ServiceType.Iptv);
							iptv.ActivateFor(client, dbSession);
						}
						AddEndpoint(dbSession, clientEntPoint, settings);
						if (client.Status.Additional.Count > 0 && client.Status.Additional.Any(s => s.ShortName == "Refused")) {
							client.Status.Additional.Clear();
						}
					}

					//обновляем значение даты подключения клиента
					client.ConnectedDate = SystemTime.Now();
                    if (client.Status.Id == (uint)StatusType.BlockedAndNoConnected)
						client.Status = dbSession.Load<Status>((int)StatusType.BlockedAndConnected);

					//синхронизируем сервисы клиента на основе settings(ов)
					client.SyncServices(dbSession, settings);

					//Если клиент не включался и ему сразу был дан статический IP
					//То данные, которые проставляются DHCP сервисом, необходимо проставлять вручную, если их нет
					if (staticAddress.Length > 0) {
						if (client.WorkingStartDate == null)
							client.WorkingStartDate = SystemTime.Now();
                        if (client.RatedPeriodDate == null)
							client.RatedPeriodDate = SystemTime.Now();
                    }

					dbSession.Save(
						new Appeal(
							String.Format(newFlag ? "Создана новая точка подключения. {0} #{1}" : "Обновлена точка подключения. {0} #{1}",
								clientEntPoint.Switch.Name, clientEntPoint.Switch.Id), client, AppealType.System, employee));

					client.PhysicalClient.UpdatePackageId(clientEntPoint);
					dbSession.Save(client);

					//обновляем статические адреса для клиентской точки подключения
					dbSession.Query<StaticIp>().Where(s => s.EndPoint == clientEntPoint && !s.EndPoint.Disabled).ToList().Where(
						s => !staticAddress.Select(f => f.Id).Contains(s.Id)).ToList()
						.ForEach(s => dbSession.Delete(s));
					foreach (var s in staticAddress) {
						IPAddress ipAddress = null;
						if (!string.IsNullOrEmpty(s.Ip))
							if (IPAddress.TryParse(s.Ip, out ipAddress)) {
								s.EndPoint = clientEntPoint;
								dbSession.Save(s);
							}
					}
					//	_connectSum = 0m;

					//создаем платеж за подключение
					if (!string.IsNullOrEmpty(connectSum) && _connectSum > 0) {
						ConnectSum = _connectSum;
						var payments = dbSession.Query<PaymentForConnect>().Where(p => p.EndPoint == clientEntPoint && !p.EndPoint.Disabled).ToList();
						if (!payments.Any())
							dbSession.Save(new PaymentForConnect(_connectSum, clientEntPoint, employee));
						else {
							var payment = payments.First();
							payment.Sum = _connectSum;
							payment.Paid = false;
							dbSession.Save(payment);
							dbSession.Save(new Appeal($"Добавлен платеж за подключение {payment.Sum} руб. Точка № {payment.EndPoint.Id}. ", client, AppealType.System, employee));
							dbSession.Save(client);
						}
						//обновляем PackageId у SCE клиента
						SceHelper.UpdatePackageId(dbSession, client);
					}
					else {
						if (staticAddress.Length > 0) {
							errorMessage += (String.IsNullOrEmpty(errorMessage) ? "" : ". ") + "Ошибка ввода IP адреса. ";
						}
					}
				}
			}
		}

		public virtual void UpdateFirstVisitData(PhysicalClient newPhysicalClient)
		{
			var oldphysicalClient = this;
			if (!oldphysicalClient.Client.Lunched || oldphysicalClient.Client.AbsentPassportData()) {
				oldphysicalClient.Surname = newPhysicalClient.Surname;
				oldphysicalClient.Name = newPhysicalClient.Name;
				oldphysicalClient.Patronymic = newPhysicalClient.Patronymic;
				oldphysicalClient.BirthDate = newPhysicalClient.BirthDate;
				oldphysicalClient.CertificateType = newPhysicalClient.CertificateType;
				oldphysicalClient.PassportSeries = newPhysicalClient.PassportSeries;
				oldphysicalClient.PassportNumber = newPhysicalClient.PassportNumber;
				oldphysicalClient.CertificateName = newPhysicalClient.CertificateName;
				oldphysicalClient.PassportResidention = newPhysicalClient.PassportResidention;
				oldphysicalClient.PassportDate = newPhysicalClient.PassportDate;
				oldphysicalClient.RegistrationAddress = newPhysicalClient.RegistrationAddress;
			} else {
				oldphysicalClient.BirthDate = newPhysicalClient.BirthDate;
			}
		}

		public virtual Contact GetClientNotificationEmail(bool confirmed = true)
		{
			Contact contactMail = null;
			if (confirmed)
				contactMail = Client.Contacts.FirstOrDefault(s => s.Type == ContactType.NotificationEmailConfirmed);
			else
				contactMail = Client.Contacts.FirstOrDefault(s => s.Type == ContactType.NotificationEmailRaw);
			return contactMail ?? new Contact() { Client = Client, Type = ContactType.NotificationEmailRaw };
		}

		public virtual void AddEndpoint(ISession dbSession, ClientEndpoint endpoint, SettingsHelper settings)
		{
			Client.Endpoints.Add(endpoint);
			Client.SyncServices(dbSession, settings);
		}

	    public static void EndpointCreateIfNeeds(ISession dSession, Client currentClient, string currentIp, Employee currentEmployee, ref string errorMessage, bool isGybrid = false)
	    {
            var address = IPAddress.Parse(currentIp);
#if DEBUG
            var lease = dSession.Query<Lease>().FirstOrDefault(l => l.Endpoint == null);
#else
				var lease = DbSession.Query<Lease>().FirstOrDefault(l => l.Ip == address && l.Endpoint == null);
#endif
            ClientEndpoint currentEnpoint = null;
            if (lease != null)
            {
                currentEnpoint = dSession.Query<ClientEndpoint>().FirstOrDefault(s => s.Switch.Id == lease.Switch.Id && s.Port == lease.Port && s.Client.Id != currentClient.Id);
                if (currentEnpoint != null)
                {
                    errorMessage = "Ошибка: точка подключения не задана!";
                    var email = ConfigurationManager.AppSettings["ErrorNotifierMail"];
                    var adminPanelNewClientPage = ConfigurationManager.AppSettings["adminPanelNewPhysicalClientPage"];
                    var href = $"<a href='"+ adminPanelNewClientPage + currentClient.Id + $"'>{currentClient.Id}</a>";
                    //отправка сообщения об ошибки
                    EmailSender.SendEmail(new string[] { email },
                        "Ошибка в  Inforoom2 при автоматическом создании точки подключения по ЛС " + currentClient.Id,
                        $"При создании точки подключения для клиента {href} выяснилось, <br/>что на порту коммутатора уже находится точка подключения {currentEnpoint.Id}");
                }
            }
            if (currentClient.Endpoints.Count == 0 && lease != null && currentEnpoint == null)
            {
                if (string.IsNullOrEmpty(lease.Switch.Name))
                {
                    var addr = currentClient.PhysicalClient.Address;
                    if (addr != null)
                        lease.Switch.Name = addr.House.Street.Region.City.Name + ", " + addr.House.Street.PublicName() + ", " + addr.House.Number;
                    else
                        lease.Switch.Name = currentClient.Id + ": адрес неопределен";
                }

                var endpoint = new ClientEndpoint();
                endpoint.Client = currentClient;
                endpoint.Switch = lease.Switch;
                endpoint.Port = lease.Port;
                endpoint.Disabled = false;
                endpoint.IsEnabled = true;

                //учет гибрида
                if (isGybrid && lease.Pool?.Relay != null) {
                    endpoint.Mac = lease.Mac;
                }
                
                currentClient.SetStatus(Status.Get(StatusType.Worked, dSession));
                if (currentClient.RatedPeriodDate == null && !currentClient.Disabled)
                {
                    currentClient.RatedPeriodDate = SystemTime.Now();
                }
                if (currentClient.WorkingStartDate == null && !currentClient.Disabled)
                {
                    currentClient.WorkingStartDate = SystemTime.Now();
                }
                //обновляем значение даты подключения клиента
                currentClient.ConnectedDate = SystemTime.Now();

                endpoint.SetStablePackgeId(currentClient.PhysicalClient.Plan.PackageSpeed.PackageId);

                dSession.Save(endpoint);
                lease.Endpoint = endpoint;

                var paymentForConnect = new PaymentForConnect(currentClient.PhysicalClient.ConnectSum, endpoint);
                //Пытаемся найти сотрудника
                paymentForConnect.Employee = currentEmployee;


                var internet = currentClient.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Internet);
                internet.ActivateFor(currentClient, dSession);
                var iptv = currentClient.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Iptv);
                iptv.ActivateFor(currentClient, dSession);

                if (currentClient.IsNeedRecofiguration)
                    SceHelper.UpdatePackageId(dSession, currentClient);

                dSession.Save(lease.Switch);
                dSession.Save(paymentForConnect);
                dSession.Save(lease);
            }
        }
	}

	public enum CertificateType
	{
		[Display(Name = "Паспорт РФ")] [Description("Паспорт РФ")] Passport = 0,
		[Display(Name = "Иной документ")] [Description("Иной документ")] Other = 1
	}
}