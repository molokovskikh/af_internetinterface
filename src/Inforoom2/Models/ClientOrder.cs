using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using InternetInterface.Controllers.Filter;
using NHibernate;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using NHibernate.Linq;
using Inforoom2.Models.Services;

namespace Inforoom2.Models
{
	//перенесено из старой админки (нужен при проверке на необходимость показывать Варнинг)
	[Class(0, Table = "Orders", Schema = "internet", NameType = typeof(ClientOrder)), Description("Заказ")]
	public class ClientOrder : BaseModel, ILogAppeal
	{
		public ClientOrder()
		{
			OrderServices = new List<OrderService>();
		}

		[Property, Description("Номер заказа")]
		public virtual int Number { get; set; }

		[Property, Description("Дата активации заказа")]
		public virtual DateTime? BeginDate { get; set; }

		[Property, Description("Дата окончания заказа")]
		public virtual DateTime? EndDate { get; set; }

		//состояние заказа, выставленное пользователем
		[Property]
		public virtual bool Disabled { get; set; }

		//обработана ли активация заказ, устанавливает биллинг нужен для учета списаний не периодических услуг
		[Property, Description("Активирован")]
		public virtual bool IsActivated { get; set; }

		//обработана ли деактивация заказа, устанавливает биллинг нужен для учета списаний периодических услуг
		[Property, Description("Деактивирован")]
		public virtual bool IsDeactivated { get; set; }

		[Bag(0, Table = "OrderServices", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "OrderId")]
		[OneToMany(2, ClassType = typeof(OrderService)), Description("Услуги")]
		public virtual IList<OrderService> OrderServices { get; set; }

		[ManyToOne(Column = "EndPoint"), Description("Точка подключения")]
		public virtual ClientEndpoint EndPoint { get; set; }

		[ManyToOne(Column = "ClientId")]
		public virtual Client Client { get; set; }


		//Это поле нужно заменить, в результате перехода на новые адреса
		[Property, Description("Адрес подключения (текст)")]
		public virtual string ConnectionAddress { get; set; }


		public virtual bool HasEndPointDisabled => EndPoint != null && EndPoint.Disabled && EndPoint.IsEnabled != null;

		public virtual bool HasEndPointNeverBeenEnabled => EndPoint != null && EndPoint.Disabled && EndPoint.IsEnabled == null;

		public virtual void SetNewBornState(ClientEndpoint endpoint)
		{
			if (endpoint.Id == 0) {
				endpoint.IsEnabled = null;
			}
			if (endpoint.IsEnabled == null) {
				endpoint.Disabled = true;
			}
		}

		[Property]
		protected virtual string NewEndPointState { get; set; }

		public virtual bool HasEndPointFutureState => !string.IsNullOrEmpty(NewEndPointState);

		/// <summary>
		/// Состояние точки подключения (для смены, при активации заказа)
		/// </summary>
		public virtual EndpointStateBox EndPointFutureState
		{
			get
			{
				if (string.IsNullOrEmpty(NewEndPointState)) {
					return null;
				}
				return (EndpointStateBox)JsonConvert.DeserializeObject(NewEndPointState, typeof(EndpointStateBox));
			}
			set
			{
				if (value == null) {
					NewEndPointState = null;
				}
				else {
					NewEndPointState = JsonConvert.SerializeObject(value);
				}
			}
		}

		public virtual void SetStaticIpAsOrderService(ISession dbSession, ClientOrder order, string staticIp,
			bool ifDescriptionNotExists = false)
		{
			decimal priceForIp = 0;
			var priceItem = dbSession.Query<Service>().FirstOrDefault(s => s.Id == Service.GetIdByType(typeof (FixedIp)));
			if (priceItem != null) {
				priceForIp = priceItem.Price;
			}
			var phrase = string.Format("Плата за фиксированный Ip адрес ({0})", staticIp);
			if (ifDescriptionNotExists == false ||
				order.OrderServices.All(s => s.Description != phrase)) {
				var staticIpService = new OrderService {
					Cost = priceForIp,
					Order = order,
					Description = phrase
				};
				dbSession.Save(staticIpService);
				order.OrderServices.Add(staticIpService);
			}
		}

		/// <summary>
		/// Обновление статических адресов по договору
		/// </summary>
		/// <param name="currentEndpoint">Точка подключения</param>
		/// <param name="staticAddress">Список статических адресов</param>
		/// <param name="employee">Пользователь</param>
		public virtual void UpdateStaticAddressList(ref ClientEndpoint currentEndpoint, StaticIp[] staticAddress,
			Employee employee)
		{
			var appealUpdate = "";
			var appealRemove = "";
			var appealInsert = "";
			const string IPRegExp =
				@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";

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
					if (isToBeAdded && Regex.IsMatch(staticAddress[i].Ip, IPRegExp)) {
						var newItem = new StaticIp() {
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

		public virtual Client GetAppealClient(ISession session)
		{
			return Client;
		}

		public virtual List<string> GetAppealFields()
		{
			return new List<string>() {
				"Number",
				"BeginDate",
				"EndDate",
				"IsDeactivated",
				"ConnectionAddress",
				"IsActivated",
				"EndPoint",
				"OrderServices"
			};
		}

		public virtual bool IsToBeClosed()
		{
			return EndDate.HasValue && EndDate.Value <= SystemTime.Now().Date;
		}

		public virtual string GetAdditionalAppealInfo(string property, object oldPropertyValue, ISession session)
		{
			string message = "";
			if (property == "IsActivated") {
				// получаем псевдоним из описания 
				property = this.GetDescription("IsActivated");
				var oldPlan = oldPropertyValue == null ? null : ((bool?)oldPropertyValue);
				var currentPlan = IsDeactivated;
				if (oldPlan != null) {
					message += property + " было: " + (oldPlan.HasValue && oldPlan.Value ? "да" : "нет") + " <br/>";
				}
				else {
					message += property + " было: " + "значение отсуствует <br/> ";
				}
				if (currentPlan != null) {
					message += property + " стало: " + (currentPlan ? "да" : "нет") + " <br/>";
				}
				else {
					message += property + " стало: " + "значение отсуствует <br/> ";
				}
			}
			if (property == "IsDeactivated") {
				// получаем псевдоним из описания 
				property = this.GetDescription("IsDeactivated");
				var oldPlan = oldPropertyValue == null ? null : ((bool?)oldPropertyValue);
				var currentPlan = IsDeactivated;
				if (oldPlan != null) {
					message += property + " было: " + (oldPlan.HasValue && oldPlan.Value ? "да" : "нет") + " <br/>";
				}
				else {
					message += property + " было: " + "значение отсуствует <br/> ";
				}
				if (currentPlan != null) {
					message += property + " стало: " + (currentPlan ? "да" : "нет") + " <br/>";
				}
				else {
					message += property + " стало: " + "значение отсуствует <br/> ";
				}
			}
			return message;
		}

		public virtual void SendMailAboutCreate(ISession session, Employee employee)
		{
			var addressRaw = ConfigHelper.GetParam("OrderNotificationMail");
			var addressList = addressRaw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			var topic = "Уведомление о создании заказа";
			var body = ClientOrder.MailPattern(this, "создание", employee) + OrderService.MailPattern(this, employee);
			EmailSender.SendEmail(addressList, topic, body);
		}

		public virtual void SendMailAboutUpdate(ISession session, Employee employee)
		{
			var addressRaw = ConfigHelper.GetParam("OrderNotificationMail");
			var addressList = addressRaw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			var topic = "Уведомление о внесение изменений в заказ";
			var body = ClientOrder.MailPattern(this, "изменение", employee) + OrderService.MailPattern(this, employee);
			EmailSender.SendEmail(addressList, topic, body);
		}

		public virtual void SendMailAboutClose(ISession session, Employee employee)
		{
			var addressRaw = ConfigHelper.GetParam("OrderNotificationMail");
			var addressList = addressRaw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			var topic = "Уведомление о закрытии заказа";
			var body = ClientOrder.MailPattern(this, "закрытие", employee) + OrderService.MailPattern(this, employee);
			EmailSender.SendEmail(addressList, topic, body);
		}

		public static string MailPattern(ClientOrder order, string operationName, Employee employee)
		{
			var sum = order.OrderServices.Sum(x => x.Cost);
			var message = new StringBuilder();
			message.AppendLine(string.Format("Зарегистрировано {0} заказа для Юр.Лица", operationName));
			message.AppendLine(string.Format("Клиент: {0}-{1}", order.Client.Id.ToString("D5"), order.Client.Name));
			message.AppendLine(string.Format("Заказ: № {0}", order.Number));
			message.AppendLine(string.Format("Сумма {0}", sum));
			message.AppendLine(string.Format("Оператор: {0}", employee.Name));
			return message.ToString();
		}
	}

	//перенесено из старой админки (нужен при проверке на необходимость показывать Варнинг)
	[Class(0, Table = "OrderServices", Schema = "internet", NameType = typeof(OrderService), Lazy = true),
	 Description("Услуга")]
	public class OrderService : BaseModel
	{
		public OrderService()
		{
		}

		public OrderService(ClientOrder order, decimal cost, bool isPeriodic)
		{
			Order = order;
			IsPeriodic = isPeriodic;
			Cost = cost;
			if (IsPeriodic)
				Description = "Доступ к сети Интернет";
			else
				Description = "Подключение";
		}

		[Property, NotNullNotEmpty(Message = "Услуги должны иметь читаемые имена"), Description("Описание")]
		public virtual string Description { get; set; }

		[Property, Description("Стоимость")]
		public virtual decimal Cost { get; set; }

		[Property, Description("Услуга периодичная")]
		public virtual bool IsPeriodic { get; set; }

		[ManyToOne(Column = "OrderId")]
		public virtual ClientOrder Order { get; set; }


		public static string MailPattern(ClientOrder order, Employee employee)
		{
			var message = new StringBuilder();
			foreach (var oneOrder in order.OrderServices) {
				message.AppendLine(String.Format(@"Описание услуги: {0}Услуга: {1}", oneOrder.Description,
					oneOrder.IsPeriodic ? "Периодичная" : "Разовая"));
			}
			return message.ToString();
		}
	}
}