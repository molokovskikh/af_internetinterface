using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Castle.Components.Validator;
using Common.Tools;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;
using NHibernate.Linq;
using System.Globalization;
using Inforoom2.Models.Services;
using Newtonsoft.Json;
using NHibernate.Bytecode;

namespace Inforoom2.Models
{
	/// <summary>
	/// Юридическое лицо
	/// </summary>
	[Class(0, Table = "LawyerPerson", Schema = "internet", NameType = typeof(LegalClient))]
	public class LegalClient : BaseModel, IClientExpander, ILogAppeal
	{
		[Property(NotNull = true), Description("Баланс юридического лица")]
		public virtual decimal Balance { get; set; }

		[Property(NotNull = true), Description("Дата, когда списывается абонентская плата")]
		public virtual DateTime PeriodEnd { get; set; }

		[ManyToOne(Column = "RegionId", Cascade = "save-update"), NotNull(Message = "Укажите регион"), Description("Регион")]
		public virtual Region Region { get; set; }

		[Property(Column = "FullName"), NotNullNotEmpty(Message = "Введите полное наименование"),
		 Description("Полное наименование")]
		public virtual string Name { get; set; }

		[Property, NotNullNotEmpty(Message = "Введите краткое наименование"), Description("Краткое наименование")]
		public virtual string ShortName { get; set; }

		[Property(Column = "LawyerAdress"), NotNullNotEmpty(Message = "Введите юридический адрес"),
		 Description("Юридический адрес")]
		public virtual string LegalAddress { get; set; }

		[Property(Column = "ActualAdress"), NotNullNotEmpty(Message = "Введите фактический адрес"),
		 Description("Фактический адрес")]
		public virtual string ActualAddress { get; set; }

		[ManyToOne(Column = "_Address", Cascade = "save-update"), Description("Адрес")]
		public virtual Address Address { get; set; }

		[Property, Description("ИНН"), NotNullNotEmpty(Message = "Введите ИНН")]
		public virtual string Inn { get; set; }

		[Property, Description("Контактное лицо"), NotNullNotEmpty(Message = "Укажите контактное лицо")]
		public virtual string ContactPerson { get; set; }

		[Property, Description("Почтовый адрес"), NotNullNotEmpty(Message = "Введите почтовый адрес")]
		public virtual string MailingAddress { get; set; }

		public virtual List<string> GetAppealFields()
		{
			return new List<string>() {
				"Balance",
				"PeriodEnd",
				"Region",
				"Name",
				"ShortName",
				"LegalAddress",
				"ActualAddress",
				"Address",
				"Inn",
				"ContactPerson",
				"MailingAddress"
			};
		}

		public virtual Client GetAppealClient(ISession session)
		{
			return Client;
		}

		public virtual string GetAdditionalAppealInfo(string property, object oldPropertyValue, ISession session)
		{
			string message = "";
			// для свойства Tariff
			if (property == "Region") {
				// получаем псевдоним из описания 
				property = this.GetDescription("Region");
				var oldPlan = oldPropertyValue == null ? null : ((Region)oldPropertyValue);
				var currentPlan = this.Region;
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

		public virtual decimal? Plan
		{
			get
			{
				return Client.LegalClientOrders.Where(o => o.IsActivated && !o.IsDeactivated)
					.SelectMany(o => o.OrderServices)
					.Where(s => s.IsPeriodic)
					.Sum(s => s.Cost);
			}
		}

		public virtual bool NeedShowWarning()
		{
			var param = ConfigHelper.GetParam("LawyerPersonBalanceWarningRate");
			var rate = (decimal)float.Parse(param, CultureInfo.InvariantCulture);
			var cond = Balance < -((Plan ?? 0) * rate) && Balance < 0;
			return cond;
		}

		[OneToOne(PropertyRef = "LegalClient")]
		public virtual Client Client { get; set; }

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
			return ShortName;
		}

		public virtual DateTime? GetRegistrationDate()
		{
			return Client.CreationDate;
		}

		public virtual DateTime? GetDissolveDate()
		{
			var lastClosedOrder =
				Client.LegalClientOrders.Where(o => o.IsDeactivated && o.EndDate != null)
					.ToList()
					.OrderByDescending(f => f.EndDate.Value)
					.FirstOrDefault();

			return ((StatusType)Client.Status.Id) != StatusType.Dissolved ? null : lastClosedOrder?.EndDate;
		}

		public virtual string GetPlan()
		{
			return $"{(this.Plan ?? 0).ToString("0.00")} руб.";
		}

		public virtual decimal GetBalance()
		{
			return Balance;
		}

		public virtual StatusType GetStatus()
		{
			return ((StatusType)Client.Status.Id);
		}

		public static void GetBaseDataForRegistration(ISession dbSession, Client client, Employee employee)
		{
			client.Recipient = dbSession.Query<Recipient>().FirstOrDefault(r => r.INN != null && r.INN == "3666152146");
			client.Status = dbSession.Query<Status>().FirstOrDefault(r => r.Id == (int)StatusType.BlockedAndNoConnected);
			client.Disabled = true;
			client.Type = ClientType.Lawer;
			client._Name = client.LegalClient.ShortName;
			client.Disabled = true;
			client.WhoRegistered = employee;
			client.WhoRegisteredName = client.WhoRegistered.Name;
			client.PercentBalance = 0m;
			client.CreationDate = DateTime.Now;
			client.FreeBlockDays = 0;
			client.SendSmsNotification = false;
			client._oldAdressStr = client.LegalClient.ActualAddress;
			client.YearCycleDate = null;
		}


		public virtual void UpdateOrderServiceList(IList<OrderService> recipient, IList<OrderService> donator,
			ClientOrder order, Employee employee)
		{
			var appealUpdate = "";
			var appealRemove = "";
			var appealInsert = "";
			var recipientsToRemove = recipient.Where(s => !donator.Any(d => d.Id == s.Id)).ToList();
			recipient.RemoveEach(recipientsToRemove);

			//Формирование оповещения--->
			if (recipientsToRemove.Count > 0) {
				appealRemove =
					$" <br/><strong>удалены услуги:</strong> <br/>{string.Join("<br/>", recipientsToRemove.Select(s => $"{(s.IsPeriodic ? "периодическая" : "разовая")} услуга '{s.Description}' стоимостью в {s.Cost.ToString("0.00")} руб.)").ToList())}";
			} //<----Формирование оповещения

			for (int i = 0; i < donator.Count; i++) {
				bool isToBeAdded = true;
				donator[i].Order = order;
				for (int j = 0; j < recipient.Count; j++) {
					if (donator[i].Id != 0 && donator[i].Id == recipient[j].Id) {
						//Формирование оповещения--->
						if (recipient[j].IsPeriodic != donator[i].IsPeriodic || recipient[j].Description != donator[i].Description ||
						    recipient[j].Cost != donator[i].Cost) {
							appealUpdate += (appealUpdate == string.Empty ? " <br/><strong>обновлены услуги:</strong>" : "");
							appealUpdate +=
								$"<br/><i>было</i> - {(recipient[j].IsPeriodic ? "периодическая" : "разовая")} услуга '{recipient[j].Description}' стоимостью в {recipient[j].Cost.ToString("0.00")} руб.";
							appealUpdate +=
								$"<br/><i>стало</i> - {(donator[i].IsPeriodic ? "периодическая" : "разовая")} услуга '{donator[i].Description}' стоимостью в {donator[i].Cost.ToString("0.00")} руб.";
						} //<---Формирование оповещения

						recipient[j].Cost = donator[i].Cost;
						recipient[j].Order = donator[i].Order;
						recipient[j].Description = donator[i].Description;
						recipient[j].IsPeriodic = donator[i].IsPeriodic;
						recipient[j].Order = order;
						isToBeAdded = false;
						break;
					}
				}
				if (isToBeAdded) {
					//Формирование оповещения--->
					if (appealInsert == string.Empty)
						appealInsert = "<br/><strong>добавлены услуги:</strong>";
					appealInsert +=
						$"<br/>{(donator[i].IsPeriodic ? "периодическая" : "разовая")} услуга '{donator[i].Description}' стоимостью в {donator[i].Cost.ToString("0.00")} руб.";
					//<---Формирование оповещения
					recipient.Add(donator[i]);
				}
			}

			appealInsert = appealInsert + appealUpdate + appealRemove;
			if (appealInsert != string.Empty) {
				order.Client.Appeals.Add(new Appeal($"По заказу №{order.Number} " + appealInsert, order.Client, AppealType.System,
					employee));
			}
		}

		/// <summary>
		/// Обновление Endpoint(a) 
		/// </summary>
		/// <param name="dbSession">Сессия хибера</param>
		/// <param name="currentOrder">Текущий заказ</param>
		/// <param name="currentEndpoint">Текущая точка подключения</param>
		/// <param name="futureEndpoint">Будушая точка подключения (созданная для конкретного заказа)</param>
		/// <param name="connection">Настройки подключения</param>
		/// <param name="employee">Пользователь</param>
		/// <returns></returns>
		private string UpdateClientEndpointByConnectionSettings(ISession dbSession, ref ClientOrder currentOrder, ref ClientEndpoint currentEndpoint,
			Inforoom2.Helpers.ConnectionHelper connection, Employee employee)
		{
			int port = 0;
			int.TryParse(connection.Port ?? "0", out port);
			currentOrder.EndPointFutureState = null;
			var changesState = LegalOrderEndpointHelper.CheckChangesState(currentOrder, currentEndpoint, connection);

			if (changesState == LegalOrderEndpointHelper.ChangesState.CurrentEndpointInsert
			    || changesState == LegalOrderEndpointHelper.ChangesState.CurrentEndpointUpdateDisabled
			    || changesState == LegalOrderEndpointHelper.ChangesState.FutureEndpointUpdate) {
				currentEndpoint = currentEndpoint ?? new ClientEndpoint();
				currentEndpoint.Client = currentOrder.Client;
				currentEndpoint.SetStablePackgeId(connection.PackageId);
				currentEndpoint.Monitoring = connection.Monitoring;
				currentEndpoint.Pool = connection.GetPool(dbSession);
				currentEndpoint.IpAutoSet = connection.StaticIpAutoSet;
				currentEndpoint.Switch = connection.GetSwitch(dbSession);
				currentEndpoint.Port = port;
				currentEndpoint.IsEnabled = null;
				currentEndpoint.Disabled = true;
				if (changesState == LegalOrderEndpointHelper.ChangesState.CurrentEndpointInsert)
					return $"по которому создана новая точка подключения:<br/> <strong>коммутатор:</strong> {currentEndpoint.Switch?.Name} <strong>порт:</strong> {currentEndpoint.Port}";
				else
					return "";
			}

			if (changesState == LegalOrderEndpointHelper.ChangesState.CurrentEndpointUpdateFull
			    || changesState == LegalOrderEndpointHelper.ChangesState.FutureStateUpdateFull) {
				var currentSwitch = connection.GetSwitch(dbSession);
				var newEndpoint = new ClientEndpoint();
				newEndpoint.Client = currentOrder.Client;
				newEndpoint.SetStablePackgeId(connection.PackageId);
				newEndpoint.Monitoring = connection.Monitoring;
				newEndpoint.Pool = connection.GetPool(dbSession);
				newEndpoint.IpAutoSet = connection.StaticIpAutoSet;
				newEndpoint.Switch = currentSwitch;
				newEndpoint.Port = port;
				newEndpoint.Disabled = true;
				currentEndpoint = newEndpoint;
				return "";
			}

			if (changesState == LegalOrderEndpointHelper.ChangesState.CurrentEndpointUpdatePartial ||
			    changesState == LegalOrderEndpointHelper.ChangesState.FutureStateUpdatePartial) {
				var newStateOfEndpoint = new EndpointStateBox();
				newStateOfEndpoint.ConnectionHelper = connection;
				newStateOfEndpoint.EmployeeId = employee.Id;
				currentOrder.EndPointFutureState = newStateOfEndpoint;
			}

			if (currentOrder.EndPoint != null && currentOrder.EndPoint.Id != 0 && currentEndpoint != null && currentEndpoint.Id != 0 && currentEndpoint.Id != currentOrder.EndPoint.Id) {
				if (currentOrder.EndPoint.Disabled && currentOrder.EndPoint.IsEnabled == null) {
					currentOrder.Client.Endpoints.Remove(currentOrder.EndPoint);
					dbSession.Delete(currentOrder.EndPoint);
				}
				return "";
			}

			return "";
		}

		/// <summary>
		/// Обновление/Создание заказа юр.лица
		/// </summary>
		/// <param name="dbSession">Сессия хибера</param>
		/// <param name="order">Заказ</param>
		/// <param name="endpoint">Точка подключения</param>
		/// <param name="connection">Информация по подключению</param>
		/// <param name="staticAddress">Статические адреса</param>
		/// <param name="noEndpoint">Необходимо ли искользовать точку подключения в заказе</param>
		/// <param name="employee">Пользователь</param>
		/// <param name="message">Сообщение (исходящее)</param>
		public virtual void UpdateClientOrder(ISession dbSession, ClientOrder order,
			ClientEndpoint endpoint, Inforoom2.Helpers.ConnectionHelper connection, StaticIp[] staticAddress,
			bool noEndpoint, Employee employee, out string message)
		{
			order.EndPointFutureState = null;
			var client = Client;
			var newOrder = false;
			var changesJustForANewEndoint = false;
			EndpointStateBox orderPastEndPointFutureState = null;
			message = "";
			//получение базовых значений
			var settings = new SettingsHelper(dbSession);
			staticAddress = staticAddress ?? new StaticIp[0];
			//валидация дат по заказу
			if (order.BeginDate.HasValue && order.EndDate.HasValue) {
				var lessThanPast = order.EndDate.Value.Date <= SystemTime.Now().Date; //DateTime.Compare(order.EndDate.Value.Date, SystemTime.Now().Date);
				var lessThanCurrent = order.EndDate.Value.Date < order.BeginDate.Value.Date; //DateTime.Compare(order.EndDate.Value.Date, order.BeginDate.Value.Date);
				if (lessThanPast || lessThanCurrent) {
					message = "Дата окончания может быть выставлена только для будущего периода";
					return;
				}
			}
			//получение заказа из БД
			ClientOrder currentOrder = order.Id != 0 ? client.LegalClientOrders.FirstOrDefault(s => s.Id == order.Id) : null;
			order.OrderServices = order.OrderServices ?? new List<OrderService>();
			//если таковой в БД есть, обновляем его поля 
			if (currentOrder != null) {
				orderPastEndPointFutureState = currentOrder.EndPointFutureState;
				currentOrder.Number = order.Number;
				currentOrder.BeginDate = order.BeginDate;
				currentOrder.EndDate = order.EndDate;
				currentOrder.ConnectionAddress = order.ConnectionAddress;
				UpdateOrderServiceList(currentOrder.OrderServices, order.OrderServices, currentOrder, employee);
				currentOrder.OrderServices = currentOrder.OrderServices ?? new List<OrderService>();
			}
			else {
				//если заказа в БД не оказалось, использование нового
				order.Client = Client;
				currentOrder = order;
				newOrder = true;
			}
			//проверка привязки точки подключения к заказу
			string endpointNewAppeal = "";
			if (noEndpoint) {
				if (currentOrder.EndPoint != null && currentOrder.EndPoint.Id != 0 && currentOrder.EndPoint.Disabled && currentOrder.EndPoint.IsEnabled == null) {
					currentOrder.Client.Endpoints.Remove(currentOrder.EndPoint);
					dbSession.Delete(currentOrder.EndPoint);
				}
				currentOrder.EndPoint = null;
				currentOrder.EndPointFutureState = null;
			}
			else {
				//если привязка есть,
				//попыка получить точку подключения из БД по текущему Id 
				//или Id из нового состояния точки подключения (новой т.п.)

				var currentEndpoint = client.Endpoints.FirstOrDefault(s => s.Id == endpoint.Id) ?? endpoint;
				currentOrder.EndPointFutureState = null;
				//обновление точки подключения
				endpointNewAppeal = UpdateClientEndpointByConnectionSettings(dbSession, ref currentOrder, ref currentEndpoint, connection, employee);
				//валидация 
				if (currentEndpoint.Switch == null) {
					message = "Заказ не сохранен : не указан коммутатор для точки подключения";
					return;
				}
				if (currentEndpoint.Switch.PortCount < currentEndpoint.Port || currentEndpoint.Port == 0) {
					message = "Заказ не сохранен : у коммутатора отсутствует указанный порт";
					return;
				}
				if (currentEndpoint.PackageId == null || currentEndpoint.PackageId == 0) {
					message = "Заказ не сохранен : cкорость точки подключения не задана";
					return;
				}


				//правим статус зарегистрированного клиента
				if (client.Status.Id == (int)StatusType.BlockedAndNoConnected)
					client.Status = dbSession.Load<Status>((int)StatusType.BlockedAndConnected);


				if (!currentOrder.HasEndPointFutureState) {
					IPAddress address;
					if (connection.StaticIp != null && IPAddress.TryParse(connection.StaticIp, out address)) {
						//подключение услуги "фиксированный ip"
						if (currentEndpoint.Ip == null) {
							order.SetStaticIpAsOrderService(dbSession, order, connection.StaticIp);
							currentEndpoint.IpAutoSet = false;
						}
						currentEndpoint.Ip = address;
					}
					else
						currentEndpoint.Ip = null;
					if (currentEndpoint.IpAutoSet.HasValue && currentEndpoint.IpAutoSet.Value) {
						order.SetStaticIpAsOrderService(dbSession, order, "авто-назначение",true);
					}
					currentOrder.EndPoint = currentEndpoint;
					var newEndpointCreated = false;
					if (currentEndpoint.Id == 0) {
						newEndpointCreated = true;
						client.Endpoints.Add(currentEndpoint);
					}
					dbSession.Save(currentEndpoint);
					if (newEndpointCreated) {
						currentOrder.UpdateStaticAddressList(ref currentEndpoint, staticAddress, employee);
					}
					else {
						if (!(currentEndpoint.StaticIpList.Count == staticAddress.Length
						      && (currentEndpoint.StaticIpList.Count == 0 || currentEndpoint.StaticIpList.Any(s => staticAddress.Any(f => f.Ip == s.Ip && f.Mask == s.Mask))))) {
							var newStateOfEndpoint = new EndpointStateBox();
							newStateOfEndpoint.EmployeeId = employee.Id;
							newStateOfEndpoint.ConnectionHelper = null;
							newStateOfEndpoint.StaticIpList = staticAddress;
							currentOrder.EndPointFutureState = newStateOfEndpoint;
						}
					}
				}
				else {
					currentOrder.EndPoint = currentEndpoint;
					//добалвяем в новое состояние список статических адресов
					var epState = currentOrder.EndPointFutureState;
					epState.StaticIpList = staticAddress;
					currentOrder.EndPointFutureState = epState;

					if (currentOrder.EndPointFutureState?.ConnectionHelper?.StaticIpAutoSet == true) {
						order.SetStaticIpAsOrderService(dbSession, order, "авто-назначение", true);
					}
				}
			}


			if (newOrder) {
				client.LegalClientOrders.Add(currentOrder);
				currentOrder.Client.Appeals.Add(new Appeal(
					$"Зарегистрирован заказ №<strong>{currentOrder.Number}</strong> <br/>" + endpointNewAppeal, currentOrder.Client, AppealType.System,
					employee));
				currentOrder.SendMailAboutCreate(dbSession, employee);
			}
			else {
				currentOrder.Client.Appeals.Add(new Appeal($"Обновлен заказ №<strong>{currentOrder.Number}</strong>", currentOrder.Client,
					AppealType.System, employee));
				currentOrder.SendMailAboutUpdate(dbSession, employee);
			}
			//TODO: это верно? 
			if (client.Disabled && client.LegalClientOrders.Count > 0)
				client.Disabled = false;
		}
	}
}