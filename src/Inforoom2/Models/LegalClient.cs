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
using InternetInterface.Models;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;
using NHibernate.Linq;
using System.Globalization;
using Inforoom2.Models.Services;

namespace Inforoom2.Models
{
	/// <summary>
	/// Юридическое лицо
	/// </summary>
	[Class(0, Table = "LawyerPerson", Schema = "internet", NameType = typeof (LegalClient))]
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
			return new List<string>()
			{
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
				var oldPlan = oldPropertyValue == null ? null : ((Region) oldPropertyValue);
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
			var rate = (decimal) float.Parse(param, CultureInfo.InvariantCulture);
			var cond = Balance < -((Plan ?? 0)*rate) && Balance < 0;
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

			return ((StatusType) Client.Status.Id) != StatusType.Dissolved ? null : lastClosedOrder?.EndDate;
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
			return ((StatusType) Client.Status.Id);
		}

		public static void GetBaseDataForRegistration(ISession dbSession, Client client, Employee employee)
		{
			client.Recipient = dbSession.Query<Recipient>().FirstOrDefault(r => r.INN != null && r.INN == "3666152146");
			client.Status = dbSession.Query<Status>().FirstOrDefault(r => r.Id == (int) StatusType.BlockedAndNoConnected);
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

		public virtual void UpdateStaticAddressList(ref ClientEndpoint currentEndpoint, StaticIp[] staticAddress,
			Employee employee)
		{
			var appealUpdate = "";
			var appealRemove = "";
			var appealInsert = "";

			var recipientsToRemove = currentEndpoint.StaticIpList.Where(s => !staticAddress.Any(d => d.Ip == s.Ip)).ToList();
			currentEndpoint.StaticIpList.RemoveEach(recipientsToRemove);
			//Формирование оповещения--->
			if (recipientsToRemove.Count > 0) {
				appealRemove =
					$" <br/><strong>удалены услуги:</strong> <br/>{string.Join("<br/>", recipientsToRemove.Select(s => $"<br/><i>было</i> {s.Ip} : {s.Mask} ").ToList())}";
			} //<----Формирование оповещения

			if (staticAddress != null) {
				for (int i = 0; i < staticAddress.Length; i++) {
					bool isToBeAdded = true;
					for (int j = 0; j < currentEndpoint.StaticIpList.Count; j++) {
						if (staticAddress[i].Id != 0 && currentEndpoint.StaticIpList[j].Id == staticAddress[i].Id) {
							//Формирование оповещения--->
							if (currentEndpoint.StaticIpList[j].Ip != staticAddress[i].Ip ||
							    currentEndpoint.StaticIpList[j].Mask != staticAddress[i].Mask) {
								appealUpdate += (appealUpdate == string.Empty ? " <br/><strong>обновлены ip:</strong>" : "");
								appealUpdate +=
									$"<br/><i>было</i> {currentEndpoint.StaticIpList[j].Ip} : {currentEndpoint.StaticIpList[j].Mask}";
								appealUpdate += $"<br/><i>стало</i> {staticAddress[i].Ip} : {staticAddress[i].Mask}";
							} //<---Формирование оповещения

							currentEndpoint.StaticIpList[j].Ip = staticAddress[i].Ip;
							currentEndpoint.StaticIpList[j].Mask = staticAddress[i].Mask;
							currentEndpoint.StaticIpList[j].EndPoint = currentEndpoint;
							isToBeAdded = false;
							break;
						}
					}
					if (isToBeAdded && Regex.IsMatch(staticAddress[i].Ip, NetworkSwitch.IPRegExp)) {
						var newItem = new StaticIp()
						{
							EndPoint = currentEndpoint,
							Ip = staticAddress[i].Ip,
							Mask = staticAddress[i].Mask
						};
						currentEndpoint.StaticIpList.Add(newItem);

						//Формирование оповещения--->
						if (appealInsert == string.Empty)
							appealInsert = "<br/><strong>добавлены ip:</strong>";
						appealInsert +=
							$"<br/> {newItem.Ip} : {staticAddress[i].Mask}";
						//<---Формирование оповещения
					}
				}
			}
			appealInsert = appealInsert + appealUpdate + appealRemove;
			if (appealInsert != string.Empty) {
				currentEndpoint.Client.Appeals.Add(new Appeal($"По подключению №{currentEndpoint.Id} " + appealInsert,
					currentEndpoint.Client, AppealType.System,
					employee));
			}
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

		private void SetStaticIpAsOrderService(ISession dbSession, ClientOrder order, string staticIp)
		{
			decimal priceForIp = 0;
			var priceItem = dbSession.Query<Service>().FirstOrDefault(s => s.Id == Service.GetIdByType(typeof(FixedIp)));
			if (priceItem != null)
			{
				priceForIp = priceItem.Price;
			}
			var staticIpService = new OrderService
			{
				Cost = priceForIp,
				Order = order,
				Description = string.Format("Плата за фиксированный Ip адрес ({0})", staticIp)
			};
			dbSession.Save(staticIpService);
			order.OrderServices.Add(staticIpService);
		}

		public virtual void UpdateClientOrder(ISession dbSession, ClientOrder order,
			ClientEndpoint endpoint, Inforoom2.Helpers.ConnectionHelper connection, StaticIp[] staticAddress,
			bool noEndpoint, Employee employee, out string message)
		{
			var newOrder = false;
			message = "";
			//получаем базовые значения
			var settings = new SettingsHelper(dbSession);
			staticAddress = staticAddress ?? new StaticIp[0];
			if (order.BeginDate.HasValue && order.EndDate.HasValue) {
				var lessThanPast = DateTime.Compare(order.EndDate.Value.Date, SystemTime.Now().Date);
				var lessThanCurrent = DateTime.Compare(order.EndDate.Value.Date, order.BeginDate.Value.Date);
				if (lessThanPast != 1 || lessThanCurrent != 1) {
					message = "Дата окончания может быть выставлена только для будущего периода";
					return;
				}
			}

			var client = Client;
			//проверка необходимости обновлять заказ
			var currentOrder = client.LegalClientOrders.FirstOrDefault(s => s.Id == order.Id);

			order.OrderServices = order.OrderServices ?? new List<OrderService>();

			if (currentOrder != null) {
				currentOrder.Number = order.Number;
				currentOrder.BeginDate = order.BeginDate;
				currentOrder.EndDate = order.EndDate;
				currentOrder.ConnectionAddress = order.ConnectionAddress;
				UpdateOrderServiceList(currentOrder.OrderServices, order.OrderServices, currentOrder, employee);
				currentOrder.OrderServices = currentOrder.OrderServices ?? new List<OrderService>();
			}
			else {
				order.Client = Client;
				currentOrder = order;
				newOrder = true;
			}

			string endpointNewAppeal = "";
			if (noEndpoint) {
				currentOrder.EndPoint = null;
			}
			else {
				var currentEndpoint = client.Endpoints.FirstOrDefault(s => s.Id == endpoint.Id);
				int port = 0;
				int.TryParse(connection.Port ?? "0", out port);

				if (currentEndpoint != null) {
					currentEndpoint.PackageId = connection.PackageId;
					currentEndpoint.Monitoring = connection.Monitoring;
					currentEndpoint.Pool = connection.GetPool(dbSession);
					currentEndpoint.Switch = connection.GetSwitch(dbSession);
					currentEndpoint.Port = port;

				}
				else {
					currentEndpoint = endpoint;
					currentEndpoint.Client = client;
					currentEndpoint.PackageId = connection.PackageId;
					currentEndpoint.Monitoring = connection.Monitoring;
					currentEndpoint.Pool = connection.GetPool(dbSession);
					currentEndpoint.Switch = connection.GetSwitch(dbSession);
					currentEndpoint.Port = port;
					endpointNewAppeal =
						$"по которому создана новая точка подключения:<br/> <strong>коммутатор:</strong> {currentEndpoint.Switch.Name} <strong>порт:</strong> {currentEndpoint.Port}";

					///Не понял зачем нужно это поле - не используется. 
					//if (client.AdditionalStatus != null && client.AdditionalStatus.ShortName == "Refused")
					//{
					//	client.AdditionalStatus = null;
					//	client.AdditionalStatus = null;
					//}
				}


				if (currentEndpoint.Switch == null) {
					message = "Заказ не сохранен : не указан коммутатор для точки подключения";
					return;
				}

				if (currentEndpoint.Switch.PortCount < currentEndpoint.Port) {
					message = "Заказ не сохранен : у коммутатора отсутствует указанный порт";
					return;
				}
				if (currentEndpoint.PackageId == null || currentEndpoint.PackageId == 0) {
					message = "Заказ не сохранен : cкорость точки подключения не задана";
					return;
				}

				if (client.Status.Id == (int) StatusType.BlockedAndNoConnected)
					client.Status = dbSession.Load<Status>((int) StatusType.BlockedAndConnected);

				if (staticAddress != null && staticAddress.Length > 0) {
					if (client.WorkingStartDate == null)
						client.WorkingStartDate = DateTime.Now;
					if (client.RatedPeriodDate == null)
						client.RatedPeriodDate = DateTime.Now;
				}

				IPAddress address;
				if (connection.StaticIp != null && IPAddress.TryParse(connection.StaticIp, out address)) {
					if (currentEndpoint.Ip == null) {
						SetStaticIpAsOrderService(dbSession, order, connection.StaticIp);
					}
					currentEndpoint.Ip = address;
				}
				else
					currentEndpoint.Ip = null;

				client.ConnectedDate = DateTime.Now;

				UpdateStaticAddressList(ref currentEndpoint, staticAddress, employee);

				currentOrder.EndPoint = currentEndpoint;

				client.Endpoints.Add(currentEndpoint);
				//синхронизация (нужна предварительная валидация клиента и др.)
				client.SyncServices(dbSession, settings);
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
				currentOrder.SendMailAboutUpdate(dbSession,employee);
            }

			if (client.Disabled && client.LegalClientOrders.Count > 0)
				client.Disabled = false;
		}
	}
}