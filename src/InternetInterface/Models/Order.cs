using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Tools;
using Common.Web.Ui.Models.Audit;
using InternetInterface.Helpers;
using NHibernate;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NHibernate.Linq;
using InternetInterface.Services;
using InternetInterface.Models.Services;
using InternetInterface.Controllers;
using NPOI.SS.Formula.Functions;
using System.Net;

namespace InternetInterface.Models
{
	public enum OrderStatus
	{
		[Description("Новый")] New = 0,
		[Description("Активный")] Enabled = 1,
		[Description("Неактивный")] Disabled = 2
	}

	/// <summary>
	/// Заказ
	/// </summary>
	[ActiveRecord(Schema = "Internet", Table = "Orders", Lazy = true), Auditable, LogInsert(typeof(LogOrderInsert))]
	public class Order
	{
		public Order(LawyerPerson client)
			: this()
		{
			Client = client.client;
		}

		public Order()
		{
			Number = 1;
			BeginDate = SystemTime.Now();
			OrderServices = new List<OrderService>();
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, Description("Номер заказа"), Auditable("Номер заказа")]
		public virtual uint Number { get; set; }

		[Property, Auditable("Дата активации заказа"), Description("Дата активации заказа")]
		public virtual DateTime? BeginDate { get; set; }

		[Property, Auditable("Дата окончания заказа"), Description("Дата окончания заказа")]
		public virtual DateTime? EndDate { get; set; }

		[BelongsTo]
		public virtual ClientEndpoint EndPoint { get; set; }

		[HasMany(ColumnKey = "OrderId", Lazy = true, Cascade = ManyRelationCascadeEnum.SaveUpdate), ValidateCollectionNotEmpty("Невозможно создать заказ без услуг")]
		public virtual IList<OrderService> OrderServices { get; set; }

		[BelongsTo(Column = "ClientId")]
		public virtual Client Client { get; set; }

		//состояние заказа, выставленное пользователем
		[Property]
		public virtual bool Disabled { get; set; }

		//обработана ли активация заказ, устанавливает биллинг нужен для учета списаний не периодических услуг
		[Property]
		public virtual bool IsActivated { get; set; }

		//обработана ли деактивация заказа, устанавливает биллинг нужен для учета списаний периодических услуг
		[Property]
		public virtual bool IsDeactivated { get; set; }

		[Property, Description("Вспомогательное поле для указания фактического адреса подключения")]
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

		/// <summary>
		/// Обновление статических адресов по договору
		/// </summary>
		/// <param name="currentEndpoint">Точка подключения</param>
		/// <param name="staticAddress">Список статических адресов</param>
		/// <param name="employee">Пользователь</param>
		public virtual void UpdateStaticAddressList(ref ClientEndpoint currentEndpoint, StaticIp[] staticAddress,
			Partner employee)
		{
			var appealUpdate = "";
			var appealRemove = "";
			var appealInsert = "";
			const string IPRegExp =
				@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";

			var recipientsToRemove = currentEndpoint.StaticIps.Where(s => !staticAddress.Any(d => d.Ip == s.Ip)).ToList();
			currentEndpoint.StaticIps.RemoveEach(recipientsToRemove);
			//Формирование оповещения--->
			if (recipientsToRemove.Count > 0) {
				appealRemove =
					$" <br/><strong>удалены услуги:</strong> <br/>{string.Join("<br/>", recipientsToRemove.Select(s => $"<br/><i>было</i> {s.Ip} : {s.Mask} ").ToList())}";
			} //<----Формирование оповещения

			if (staticAddress != null) {
				for (int i = 0; i < staticAddress.Length; i++) {
					bool isToBeAdded = true;
					for (int j = 0; j < currentEndpoint.StaticIps.Count; j++) {
						if (staticAddress[i].Id != 0 && currentEndpoint.StaticIps[j].Id == staticAddress[i].Id) {
							//Формирование оповещения--->
							if (currentEndpoint.StaticIps[j].Ip != staticAddress[i].Ip ||
							    currentEndpoint.StaticIps[j].Mask != staticAddress[i].Mask) {
								appealUpdate += (appealUpdate == string.Empty ? " <br/><strong>обновлены ip:</strong>" : "");
								appealUpdate +=
									$"<br/><i>было</i> {currentEndpoint.StaticIps[j].Ip} : {currentEndpoint.StaticIps[j].Mask}";
								appealUpdate += $"<br/><i>стало</i> {staticAddress[i].Ip} : {staticAddress[i].Mask}";
							} //<---Формирование оповещения

							currentEndpoint.StaticIps[j].Ip = staticAddress[i].Ip;
							currentEndpoint.StaticIps[j].Mask = staticAddress[i].Mask;
							currentEndpoint.StaticIps[j].EndPoint = currentEndpoint;
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
						currentEndpoint.StaticIps.Add(newItem);

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
				currentEndpoint.Client.Appeals.Add(new Appeals() {
					Appeal = $"По подключению №{currentEndpoint.Id} " + appealInsert,
					Client = currentEndpoint.Client, AppealType = AppealType.System, Partner = employee
				});
			}
		}

		private void SetStaticIpAsOrderService(ISession dbSession, Order order, string staticIp)
		{
			decimal priceForIp = 0;
			var priceItem = Service.GetByType(typeof(PinnedIp));
			if (priceItem != null) {
				priceForIp = priceItem.Price;
			}
			var staticIpService = new OrderService {
				Cost = priceForIp,
				Order = order,
				Description = string.Format("Плата за фиксированный Ip адрес ({0})", staticIp)
			};
#if DEBUG
			dbSession?.Save(staticIpService); //условие для поддержки старых тестов
#else
			dbSession.Save(staticIpService);
#endif
			order.OrderServices.Add(staticIpService);
		}

		/// <summary>
		/// Активация заказа
		/// </summary>
		/// <param name="dbSession"></param>
		public virtual void Activate(ISession dbSession)
		{
			IsActivated = true;
			//Получение нового состояния точки подключения по данному заказу
			var newEndPointStateRaw = EndPointFutureState;
			//Если такое есть нужно обновить точку подключения в соответствии с новым состоянием или заменить, если в состоянии указан Endpoint.Id 
			//Если НЕТ, то снимаем флаг деактивации с текущей точки подключения (позволяем ей активироваться в будущем) 
			if (newEndPointStateRaw != null) {
				//для новой точки подключния

				//для новых настроек точки подключния
				if (EndPoint != null) {
					var currentEndpoint = EndPoint;
					var newEndPointState = newEndPointStateRaw;
					var connection = newEndPointState.ConnectionHelper;
					//обновляем значения Endpoint
					if (connection != null) {
						var oldWarningState = currentEndpoint.Client.ShowBalanceWarningPage;

						currentEndpoint.SetStablePackgeId(connection.PackageId);
						currentEndpoint.Monitoring = connection.Monitoring;
						currentEndpoint.Pool = (connection.Pool.HasValue ? (uint?)Convert.ToUInt32(connection.Pool.Value) : null);

						//выставляем флаг автоприсвоения Ip адреса
						if (connection.StaticIpAutoSet) {
							currentEndpoint.IpAutoSet = true;
						} else {
							if (currentEndpoint.IpAutoSet.HasValue && currentEndpoint.IpAutoSet.Value)
								currentEndpoint.IpAutoSet = false;
						} 

						//Обновляем фиксированный Ip
						IPAddress address;
						if (connection.StaticIp != null && IPAddress.TryParse(connection.StaticIp ?? "", out address)) {
							if (currentEndpoint.Ip == null) {
								SetStaticIpAsOrderService(dbSession, this, connection.StaticIp);
							}
							currentEndpoint.Ip = address;
						} else {
							if (currentEndpoint.Ip != null && connection.StaticIp == null) {
								currentEndpoint.Client.CreareAppeal($"По заказу №{this.Number} для точки подключения №{currentEndpoint.Id} отменена фиксация IP адреса - {currentEndpoint.Ip}",logBalance:false);
							}
							currentEndpoint.Ip = null;
						}

						if (oldWarningState != currentEndpoint.Client.ShowBalanceWarningPage) {
							currentEndpoint.Client.ShowBalanceWarningPage = oldWarningState;
							dbSession.Save(currentEndpoint.Client);
						}
					}
					Partner currentPartner = null;
#if DEBUG
					currentPartner = dbSession?.Query<Partner>().FirstOrDefault(s => s.Id == newEndPointState.EmployeeId); //условие для поддержки старых тестов
#else
				currentPartner = dbSession.Query<Partner>().FirstOrDefault(s => s.Id == newEndPointState.EmployeeId);
#endif
					//Обновляем статические Ip
					UpdateStaticAddressList(ref currentEndpoint, newEndPointState.StaticIpList ?? new StaticIp[0], currentPartner);
				}
				EndPointFutureState = null;
			}
			else {
				//снимаем флаг деактивации с текущей точки подключения 
				if (EndPoint != null && !EndPoint.IsEnabled.HasValue) {
					EndPoint.Disabled = false;
#if DEBUG
					dbSession?.Save(EndPoint); //условие для поддержки старых тестов
#else
			dbSession.Save(EndPoint);
#endif
					//Обновляем дату подключения 
					Client.ConnectedDate = DateTime.Now;
				}
			}
		}

		/// <summary>
		/// Деактивация заказа
		/// </summary>
		/// <param name="dbSession">сессия хибернейта</param>
		/// <param name="justCleanSwitchAndPort">неполная деактивация, чистака коммутаторов и портов</param>
		public virtual void Deactivate(ISession dbSession, bool justCleanSwitchAndPort = false)
		{
			if (!justCleanSwitchAndPort) {
				IsDeactivated = true;
				//Деактивируем текущую точку подключения, если в других заказах она отсутствует
				//Создаем соответствующее уведомление
				Client.Appeals.Add(new Appeals() { Appeal = String.Format("Деактивирован заказ {0}", Description), AppealType = AppealType.System, Date = SystemTime.Now(), Client = Client });
			}
			if (EndPoint != null && !EndPoint.Disabled) {
				if (!Client.Orders.Where(s => s.IsDeactivated == false && s.Id != this.Id && s.EndPoint != null).Select(x => x.EndPoint.Id).Contains(EndPoint.Id)) {
					Client.Appeals.Add(new Appeals() { Appeal = $"Точка подключения №{EndPoint.Id} (коммутатор: {EndPoint.Switch?.Id} - {EndPoint.Switch?.Name}, порт: {EndPoint.Port}) была деактивирована.", AppealType = AppealType.System, Date = SystemTime.Now(), Client = Client });
					if (dbSession == null) {
						ConnectionAddress = ConnectionAddress + $" (коммутатор: {EndPoint.Switch?.Id} - {EndPoint.Switch?.Name}, порт: {EndPoint.Port})";
						EndPoint.Switch = null;
						EndPoint.Port = null;
						EndPoint.Disabled = true;
                        EndPoint.Ip = null;
                        EndPoint.StaticIps.RemoveEach(EndPoint.StaticIps);
                    }
					else {
						dbSession.CreateSQLQuery("UPDATE internet.clientendpoints SET Disabled = 1 , Switch = null , Port = null, Ip = null WHERE Id = " + EndPoint.Id).UniqueResult();
                        dbSession.CreateSQLQuery("DELETE FROM internet.StaticIps WHERE EndPoint = " + EndPoint.Id).UniqueResult();
                        dbSession.CreateSQLQuery($"UPDATE internet.orders SET ConnectionAddress = ' (коммутатор: {EndPoint.Switch?.Id} - {EndPoint.Switch?.Name}, порт: {EndPoint.Port}) ' WHERE Id = {Id}").UniqueResult();
					}
				}
			}
		}

		/// <summary>
		/// Статус заказа
		/// </summary>
		public virtual OrderStatus OrderStatus
		{
			get
			{
				if (Disabled)
					return OrderStatus.Disabled;
				if (BeginDate.Value.Date <= SystemTime.Now().Date) {
					if (EndDate == null || EndDate.Value.Date > SystemTime.Now().Date) {
						return OrderStatus.Enabled;
					}
					return OrderStatus.Disabled;
				}
				return OrderStatus.New;
			}
		}

		public virtual
			string Description
		{
			get { return string.Format("{0}, услуги {1}", Id, OrderServices.Implode()); }
		}

		public virtual
			bool CanEdit
			()
		{
			return !IsActivated;
		}

		public static
			uint GetNextNumber
			(ISession session, uint clientId)
		{
			var client = session.Get<Client>(clientId);
			if (client != null && client.Orders.Count > 0)
				return client.Orders.Max(o => o.Number) + 1;
			return 1;
		}
	}

	public class EndpointStateBox
	{
		public BaseConnectionHelper ConnectionHelper { get; set; }
		public StaticIp[] StaticIpList { get; set; }
		public int EmployeeId { get; set; }
	}

	public class BaseConnectionHelper
	{
		public string Port { get; set; }
		public int? Pool { get; set; }
		public int Switch { get; set; }
		public int Brigad { get; set; }
		public string StaticIp { get; set; }
		public bool StaticIpAutoSet { get; set; }
		public bool Monitoring { get; set; }
		public int PackageId { get; set; }
	}
}