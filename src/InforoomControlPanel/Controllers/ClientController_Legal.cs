using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.UI;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InforoomControlPanel.Models;
using InforoomControlPanel.ReportTemplates;
using NHibernate.Linq;
using NHibernate.Util;
using NHibernate.Validator.Engine;
using Remotion.Linq.Clauses;
using Agent = Inforoom2.Models.Agent;
using Client = Inforoom2.Models.Client;
using ClientService = Inforoom2.Models.ClientService;
using Contact = Inforoom2.Models.Contact;
using House = Inforoom2.Models.House;
using PhysicalClient = Inforoom2.Models.PhysicalClient;
using RequestType = Inforoom2.Models.RequestType;
using ServiceRequest = Inforoom2.Models.ServiceRequest;
using Status = Inforoom2.Models.Status;
using StatusType = Inforoom2.Models.StatusType;
using Street = Inforoom2.Models.Street;
using Newtonsoft.Json;

namespace InforoomControlPanel.Controllers
{
	public partial class ClientController
	{
		/// <summary>
		/// Регистрация юр. лица
		/// </summary>
		/// <returns></returns>
		public ActionResult RegistrationLegal()
		{
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.RegionList = regionList;
			return View();
		}

		/// <summary>
		/// Страница списка клиентов
		/// </summary>
		public ActionResult ListOrderArchive(int id)
		{
			var pager = new InforoomModelFilter<ClientOrder>(this);
			var date = SystemTime.Now();
			pager.GetCriteria(s => s.Client.Id == id && (s.EndDate <= date || s.IsDeactivated));
			var ordertList = pager.GetItems();
			var client = DbSession.Query<Client>().FirstOrDefault(s => s.Id == id);
			ViewBag.Client = client;
			ViewBag.ClientOrder = ordertList;
			//передаем на форму - список скоростей
			ViewBag.PackageSpeedList = DbSession.Query<PackageSpeed>().ToList();
			//передаем на форму - список пулов для регионов
			ViewBag.IpPoolRegionList = DbSession.Query<IpPoolRegion>().Where(s => s.Region == client.GetRegion()).ToList();
			ViewBag.Pager = pager;
			return View();
		}

		/// <summary>
		/// Регистрация юр. лица
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public ActionResult RegistrationLegal([EntityBinder] Client client)
		{
			var errors = ValidationRunner.ValidateDeep(client);
			// если ошибок нет
			if (errors.Length == 0) {
				//выставляем базовые значаения созданному клиенту
				LegalClient.GetBaseDataForRegistration(DbSession, client, GetCurrentEmployee());
				// сохраняем модель
				DbSession.Save(client); 
				return RedirectToAction("InfoLegal", new { client.Id });
			}
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.RegionList = regionList;
			ViewBag.Client = client;
			return View();
		}

		/// <summary>
		/// Страница редактирования клиента - физ. лица (учавствует и в пост-запросе)
		/// TODO: этот божественный метод нужно распилить. Лучше не использовать Глубокую валидацию,
		/// TODO: (в идеале байндить объекты обычным байндером)
		/// TODO: ну или хотя бы выделить сложным ветвлениям по методу
		/// </summary>
		/// <param name="id">идентификатор</param>
		/// <param name="clientModelItem">модель клиента</param>
		/// <param name="subViewName">подПредставление</param>
		/// <param name="appealType">тип оповещений</param>
		/// <param name="writeOffState">объединение списка "списаний" - месяц / год</param>
		/// <param name="clientStatus">статус клиента</param>
		/// <returns></returns>
		public ActionResult InfoLegal(int id, object clientModelItem = null, string subViewName = "", int appealType = 0,
			int writeOffState = 0, int clientStatus = 0, string clientStatusChangeComment = "")
		{
			Client client;
			//--------------------------------------------------------------------------------------| Обработка представлений (редактирование клиента)
			bool updateSce = false;
			// Получаем клиента
			if (clientModelItem != null && clientModelItem as Client != null) {
				var clientModel = (Client)clientModelItem;
				//подпредставление "контакты"
				if (subViewName == "_Contacts") {
					var listForAppeal = new List<string>();
					//такая структора из-за работы байндера (не менять, а если менять, то колностью работу со списком контактов)
					//удаление контактов
					var contactsToRemove = clientModel.Contacts.Where(s => s.ContactString == string.Empty).ToList();
					foreach (var item in contactsToRemove) {
						clientModel.Contacts.Remove(item);
						DbSession.Delete(item);
					}
					//обновление контактов, задание нужных полей при добавлении
					clientModel.Contacts.Each(s => {
						s.Client = clientModel;
						s.ContactString = s.ContactFormatString;
						s.ContactName = s.ContactName == string.Empty ? null : s.ContactName; // в БД по умолчанию Null а не пустое значение  
						if (s.Date == DateTime.MinValue) {
							s.Date = SystemTime.Now();
						}
						if (s.WhoRegistered == null) {
							s.WhoRegistered = GetCurrentEmployee();
							listForAppeal.Add(s.ContactFormatString);
						}
					});
					if (listForAppeal.Count != 0) {
						clientModel.Appeals.Add(new Appeal($"Обновлены контактные данные. Добавлены: {string.Join(",", listForAppeal)}",
							clientModel, AppealType.System, GetCurrentEmployee()));
					}
				}
				// проводим валидацию модели клиента
				//		errors = ValidationRunner.ValidateDeep(clientModel);
				var errors = new ValidationErrors();
				if (subViewName == "_PrivateLegalInfo") {
					errors = ValidationRunner.Validate(clientModel);
					errors.AddRange(ValidationRunner.Validate(clientModel.LegalClient));
				}
				if (subViewName == "_Appeals") {
					clientModel.Appeals.Each(s => errors.AddRange(ValidationRunner.Validate(s)));
				}
				if (subViewName == "_Contacts") {
					clientModel.Contacts.Each(s => errors.AddRange(ValidationRunner.Validate(s)));
				}
				if (subViewName == "_Payments") {
					clientModel.Payments.Each(s => errors.AddRange(ValidationRunner.Validate(s)));
				}
				if (subViewName == "_WriteOffs") {
					clientModel.WriteOffs.Each(s => errors.AddRange(ValidationRunner.Validate(s)));
				}
				if (subViewName == "_Endpoint") {
					clientModel.Endpoints.Where(s => !s.Disabled).Each(s => errors.AddRange(ValidationRunner.Validate(s)));
					updateSce = true;
				}
				if (subViewName == "_legalOrders") {
					clientModel.LegalClientOrders.Each(s => errors.AddRange(ValidationRunner.Validate(s)));
					updateSce = true;
				}

				if (string.IsNullOrEmpty(clientStatusChangeComment) && clientStatus != 0 &&
				    clientStatus != (int)clientModel.Status.Type
				    && clientStatus == (int)StatusType.Dissolved) {
					errors.Add(new InvalidValue("Не указана причина изменения статуса", typeof(Status), "clientStatusChangeComment",
						clientModel,
						clientModel, new List<object>()));
					ErrorMessage("Не указана причина изменения статуса");
					ViewBag.clientStatusChangeComment = clientStatusChangeComment;
				}

				//обновление статуса клиента, если он отличается от текущего
				if (clientStatus != 0 && clientStatus != (int)clientModel.Status.Type && errors.Length == 0) {
					var newStatus = DbSession.Query<Status>().FirstOrDefault(s => s.Id == clientStatus);
					var messageAlert = clientModel.TryToChangeStatus(DbSession, newStatus, GetCurrentEmployee(), ref updateSce);
					if (!string.IsNullOrEmpty(messageAlert)) {
						ErrorMessage(messageAlert);
						errors.Add(new InvalidValue("", typeof(Status), "Name", newStatus, newStatus, new List<object>()));
					}
					else {
						if (!string.IsNullOrEmpty(clientStatusChangeComment)) {
							clientModel.Appeals.Add(new Appeal($"Комментарий к изменению статуса: {clientStatusChangeComment}",
								clientModel, AppealType.System, GetCurrentEmployee()));
						}
					}
				}
				//если нет ошибок
				if (errors.Length == 0) {
					// сохраняем модель

					clientModel._oldAdressStr = clientModel.LegalClient.ActualAddress;
					clientModel._Name = clientModel.LegalClient.ShortName;

					DbSession.Update(clientModel);
					if (updateSce) {
						Inforoom2.Helpers.SceHelper.UpdatePackageId(DbSession, clientModel);
					}
					return RedirectToAction("InfoLegal",
						new {
							@id = id,
							@clientModelItem = clientModelItem,
							@subViewName = subViewName,
							@appealType = appealType,
							@writeOffState = writeOffState
						});
				}
				//иначе передаем сессию на форму, где в конце представления ею рефрешим все модели, чтобы не сохранились значения с ошибками. (из-за байндера)
				else {
					ViewBag.SessionToRefresh = DbSession;
				}
				client = clientModel;
			}
			else {
				client = DbSession.Query<Client>().FirstOrDefault(i => i.LegalClient != null && i.Id == id);
			}
			//--------------------------------------------------------------------------------------| Получение списка оповещений
			// список оповещений
			var appeals = client.Appeals == null || client.Appeals.Count == 0 ? new List<Appeal>() : client.Appeals.Select(s => new Appeal() {
				Client = s.Client,
				AppealType = s.AppealType,
				Date = s.Date,
				Employee = s.Employee,
				inforoom2 = s.inforoom2,
				Message = s.Message
			}).ToList();
			//если задан тип сообщений, фильтруем по типу
			if (appealType != 0) {
				if (appealType == (int)AppealType.ClientToRead) {
					appeals = appeals.Where(s => (int) s.AppealType == (int)AppealType.ClientToRead || (int)s.AppealType == (int)AppealType.ClientToReadDone).ToList();
				} else {
					appeals = appeals.Where(s => (int) s.AppealType == appealType).ToList();
				}
			}
			else {
				//при выводе всех сообщений, нужно учесть комментарии к сервисным заявкам
				string pattern = "<li><span>{0}</span> - <span>{1}:</span><br/><span>{2}</span></li>";
				appeals.AddEach(client.ServiceRequests.Select(s => {
					return new Appeal() {
						Client = s.Client,
						AppealType = AppealType.All,
						Date = s.CreationDate,
						Employee = s.Employee,
						inforoom2 = true,
						Message =
							"<p>Сервисная заявка № <a target=_blank href=" +
							Url.Action("ServiceRequestEdit", "ServiceRequest", new { @id = s.Id }) + ">" +
							s.Id + "</a>. " + s.Description + "</p>" + "<p><ul>" +
							String.Join("", s.ServiceRequestComments.Select(d =>
								string.Format(pattern, d.CreationDate, d.Author.Name, d.Comment)).ToList()
								) + "</ul></p>"
					};
				}).ToList());
			}
			appeals = appeals.OrderByDescending(s => s.Date).ToList();

			//--------------------------------------------------------------------------------------| Получение списка списаний
			//получение списка списаний
			var writeoffsAndUserWriteOff = new List<object>();
			writeoffsAndUserWriteOff.AddRange(client.WriteOffs.Select(s => new WriteOff() {
				Id = s.Id,
				Sale = s.Sale,
				Comment = s.Comment,
				BeforeWriteOffBalance = s.BeforeWriteOffBalance,
				WriteOffDate = s.WriteOffDate,
				WriteOffSum = s.WriteOffSum,
				VirtualSum = s.VirtualSum,
				MoneySum = s.MoneySum
			}).ToList());
			//дополнение списка пользовательскими списаниями
			writeoffsAndUserWriteOff.AddRange(client.UserWriteOffs.Where(s => !s.Ignore).Select(s => new UserWriteOff() {
				Comment = s.Comment,
				Date = s.Date,
				Sum = s.Sum,
				Id = s.Id,
				Employee = s.Employee,
				IsProcessedByBilling = s.IsProcessedByBilling
			}).ToList());
			//сортировка списка списаний
			writeoffsAndUserWriteOff =
				writeoffsAndUserWriteOff.OrderByDescending(
					s => s as WriteOff != null ? ((WriteOff)s).WriteOffDate : ((UserWriteOff)s).Date).ToList();
			//--------------------------------------------------------------------------------------| Передача значений на форму
			var activeServices = client.RentalHardwareList.Where(rh => rh.IsActive).ToList();
			ViewBag.RentIsActive = activeServices.Count > 0;
			//передаем на форму - текущее представление (для фокусировки на нем)
			ViewBag.CurrentSubViewName = subViewName;
			//передаем на форму - список списаний
			ViewBag.WriteoffsAndUserWriteOff = writeoffsAndUserWriteOff;
			//передаем на форму - тип обертки списаний (месяц / год)
			ViewBag.WriteOffState = writeOffState;
			//передаем на форму - список оповещений
			ViewBag.Appeals = appeals;
			//передаем на форму - фильтр типа оповещений
			ViewBag.AppealType = appealType;
			//передаем на форму - список неопознанных звонков
			ViewBag.UnresolvedCalls = DbSession.Query<UnresolvedCall>().OrderByDescending(s => s.Id).Take(10).ToList();
			//передаем на форму - список скоростей
			ViewBag.PackageSpeedList = DbSession.Query<PackageSpeed>().ToList();
			//передаем на форму - список пулов для регионов
			ViewBag.IpPoolRegionList = DbSession.Query<IpPoolRegion>().Where(s => s.Region == client.GetRegion()).ToList();
			//передаем на форму - список статусов клиента
			ViewBag.StatusList = DbSession.Query<Status>().Where(s =>
				s.Id == (int)StatusType.Worked
				|| s.Id == (int)StatusType.NoWorked
				|| s.Id == (int)StatusType.Dissolved).OrderBy(s => s.Name).ToList();
			//список коммутаторов для региона клиента
			ViewBag.SwitchList =
				DbSession.Query<Switch>().Where(s => client.LegalClient.Region != null && s.Zone.Region.Id == client.LegalClient.Region.Id).OrderBy(s => s.Name).ToList();
			//передаем на форму - список регионов
			ViewBag.RegionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			//ViewBag.PlanList = DbSession.Query<Plan>().Where(s => !s.Disabled).OrderBy(s => s.Name).ToList();

			ViewBag.ServiceToActivate =
				DbSession.Query<Service>().FirstOrDefault(s => s.Id == Service.GetIdByType(typeof(WorkLawyer)));
			//передаем на форму - клиента
			ViewBag.Client = client;


			ViewBag.ActionName = ((string)ViewBag.ActionName) + " hid";
			return View();
		}

		/// <summary>
		/// Страница редактирования клиента - физ. лица  (непосредствено редактирования). // структура такая из-за того, что в проекте используется [EntityBinder] и все под него заточено. 
		/// 1) EntityBinder - есть во всем проекте (делать иначе в каком-то конкретном случае - плохо, а менять весь проект долго) и поэтому не Angular (или др.).
		/// 2) Почему не Ajax - он используется там, где не нужно выводить пользователю валидацию по полям
		/// </summary>
		/// <param name="client">модель клиента с формы</param>
		/// <param name="subViewName">подпредставление</param>
		/// <param name="newUserAppeal">новое оповещение о клиенте</param>
		/// <param name="writeOffState">тип объединения списаний (месяц / год)</param>
		/// <param name="clientStatus">статус клиента</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult InfoLegal([EntityBinder] Client client, string subViewName, string newUserAppeal = "",
			int writeOffState = 0, int clientStatus = 0, string clientStatusChangeComment = "")
		{
			//создание нового оповещения
			if (!string.IsNullOrEmpty(newUserAppeal)) {
				var newAppeal = new Appeal(newUserAppeal, client, AppealType.User) { Employee = GetCurrentEmployee() };
				newAppeal.Message = newAppeal.Message.ReplaceSharpWithRedmine();
				DbSession.Save(newAppeal);
			}

			//обработка модели клиента, сохранение, передача необходимых данных на форму.
			return InfoLegal(client.Id, clientModelItem: client, subViewName: subViewName, writeOffState: writeOffState,
				clientStatus: clientStatus, clientStatusChangeComment: clientStatusChangeComment);
		}

		[HttpPost]
		public ActionResult EditClientOrder([EntityBinder] Client client, string subViewName,
			ClientOrder order, ClientEndpoint endpoint, Inforoom2.Helpers.ConnectionHelper connection, StaticIp[] staticAddress,
			bool noEndpoint)
		{
			if (staticAddress != null)
				foreach (var item in staticAddress) {
					item.Mask = item.Mask.HasValue && item.Mask.Value == 0 ? 32 : item.Mask;
				}
			if (client == null || client.Id == 0 || client.LegalClient == null) {
				return RedirectToAction("List");
			}
			string message = "";
			client.LegalClient.UpdateClientOrder(DbSession, order, endpoint, connection, staticAddress, noEndpoint,
				//если сообщение заполнено, значит есть ошибки, необходим откат транзакции*
				GetCurrentEmployee(), out message);
			if (message != "") {
				ErrorMessage(message);
				DbSession.Refresh(client);
				//откат транзакции
				PreventSessionUpdate();
				return RedirectToAction("InfoLegal", new { @Id = client.Id, @subViewName = subViewName });
			}
			//обработка модели клиента, сохранение, передача необходимых данных на форму.
			return InfoLegal(client.Id, clientModelItem: client, subViewName: subViewName);
		}

		
		public ActionResult EndpointLegalWarningAdd(int endpointId)
		{
			var endpoint = DbSession.Load<ClientEndpoint>(endpointId);
			EndpointWarningAdd(endpoint);
			return RedirectToAction("InfoLegal", new { endpoint.Client.Id });
		}
		public ActionResult EndpointLegalWarningRemove(int endpointId)
		{
			var endpoint = DbSession.Load<ClientEndpoint>(endpointId);
			EndpointWarningRemove(endpoint);
			return RedirectToAction("InfoLegal", new { endpoint.Client.Id });
		}

		[HttpPost]
		public ActionResult DeleteClientEndpoint(int endpointId)
		{
			var endpoint = DbSession.Load<ClientEndpoint>(endpointId);
			var date = SystemTime.Now();
			if (endpoint.Client.IsLegalClient && endpoint.Client.LegalClientOrders.Count(s =>
				(s.EndDate > date || s.EndDate == null) && (s.Disabled == false || s.IsDeactivated == false) && s.EndPoint.Id == endpointId) > 0) {
				ErrorMessage("Точка подключения не может быть деактивирована, т.к. она используется в других действующих заказах.");
			}
			else {
				var connectionAddress = endpoint.Client.RemoveEndpoint(endpoint, DbSession);
				DbSession.Save(endpoint.Client);
				SuccessMessage($"Точка подключения №{endpointId} была успешно деактивирована.");
				endpoint.Client.Appeals.Add(new Appeal($"Точка подключения №{endpointId} ({connectionAddress}) была деактивирована.",
					endpoint.Client, AppealType.Statistic, GetCurrentEmployee()));
			}
			return RedirectToAction("InfoLegal", new { endpoint.Client.Id });
		}

		[HttpPost]
		public ActionResult CloseClientOrder(int orderId, DateTime? orderCloseDate)
		{
			var order = DbSession.Load<ClientOrder>(orderId);
			if (orderCloseDate != null) {
				order.Disabled = false;
				order.EndDate = orderCloseDate;
			}
			else {
				order.Disabled = true;
				order.EndDate = DateTime.Today;
			}
			order.SendMailAboutClose(DbSession, GetCurrentEmployee());

			var message = $"Заказ №{order.Number} будет закрыт и перенесен в архив {order.EndDate.GetValueOrDefault().ToShortDateString()}";
			order.Client.Appeals.Add(new Appeal(message, order.Client, AppealType.System, GetCurrentEmployee()));
			SuccessMessage(message);
			DbSession.Save(order);

			return RedirectToAction("InfoLegal", new { order.Client.Id });
		}


		/// <summary>
		/// получение клиентского эндпоинта
		/// </summary>
		[HttpPost]
		public ActionResult UpdateConnectionAddress(int orderId, string newAddress)
		{
			var order = DbSession.Get<ClientOrder>(orderId);
			order.ConnectionAddress = newAddress;
			DbSession.Save(order);
			return RedirectToAction("InfoLegal", new { order.Client.Id, subViewName = "_LegalOrders" });
		}

		/// <summary>
		/// получение клиентского эндпоинта
		/// </summary>
		[HttpPost]
		public JsonResult GetConnectionAddress(int id)
		{
			var order = DbSession.Get<ClientOrder>(id);
			return Json(order.ConnectionAddress ?? "", JsonRequestBehavior.AllowGet);
		}


		/// <summary>
		/// получение клиентского эндпоинта
		/// </summary>
		[HttpPost]
		public JsonResult UpdateEndpoint(int id, int order = 0)
		{
			EndpointStateBox tempState = null;
			var orderObj = DbSession.Get<ClientOrder>(order);
			//проверка наличия изменений

			var endpoint = DbSession.Get<ClientEndpoint>(id);

			if (orderObj != null && orderObj.HasEndPointFutureState && orderObj.EndPoint != null && orderObj.EndPoint.Id == id) {
				tempState = orderObj.EndPointFutureState;
			}

			if (endpoint != null) {
				var lastUsageCause = endpoint.Client.LegalClientOrders.Where(s => s.EndPoint != null && s.EndPoint.Id == id);
				if (order != 0) {
					lastUsageCause.Where(s => s.Id == order);
				}
				var lastUsage = lastUsageCause.OrderByDescending(s => s.Id).FirstOrDefault();
				string address = lastUsage != null ? lastUsage.ConnectionAddress : "";
				var endpointBox = new ViewModelClientEndpoint() {
					Id = endpoint.Id,
					Switch = endpoint.Switch.Id,
					Ip = tempState != null && tempState.ConnectionHelper != null ? tempState.ConnectionHelper.StaticIp :
						endpoint.Ip != null ? endpoint.Ip.ToString() : "",
					Pool = tempState != null && tempState.ConnectionHelper != null ? tempState.ConnectionHelper.Pool.HasValue ? tempState.ConnectionHelper.Pool.Value : 0
						: endpoint.Pool != null ? endpoint.Pool.Id : 0,
					Port = endpoint.Port,
					IpAutoSet = tempState != null && tempState.ConnectionHelper != null ? tempState.ConnectionHelper.StaticIpAutoSet : endpoint.IpAutoSet??false,
					ConnectionAddress = address,
					PackageId = tempState != null && tempState.ConnectionHelper != null ? tempState.ConnectionHelper.PackageId : endpoint.PackageId ?? 0,
					Monitoring = tempState != null && tempState.ConnectionHelper != null ? tempState.ConnectionHelper.Monitoring : endpoint.Monitoring,
					LeaseList = endpoint.LeaseList.OrderByDescending(s => s.LeaseBegin).ThenBy(s => s.LeaseEnd)
						.GroupBy(s => s.Mac).Select(s => new Tuple<string, bool>(s.First().Ip.ToString(), s.First().LeaseEnd < SystemTime.Now())).ToList(),
					StaticIpList = tempState != null ? tempState.StaticIpList != null && tempState.StaticIpList.Length > 0
						? tempState.StaticIpList.Select(s => new { @Id = s.Id, @Ip = s.Ip, @Mask = s.Mask, @Subnet = s.GetSubnet() }).ToList() : new StaticIp[0].Select(s => new { @Id = s.Id, @Ip = s.Ip, @Mask = s.Mask, @Subnet = s.GetSubnet() }).ToList() :
						endpoint.StaticIpList.Select(s => new { @Id = s.Id, @Ip = s.Ip, @Mask = s.Mask, @Subnet = s.GetSubnet() }).ToList()
				};
				return Json(endpointBox, JsonRequestBehavior.AllowGet);
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// получение клиентского эндпоинта
		/// </summary>
		[HttpPost]
		public JsonResult UpdateOrder(int id, int clientId = 0)
		{
			if (id != null && id != 0) {
				var order = DbSession.Get<ClientOrder>(id);
				if (order != null) {
					var orderBox = new ViewModelClientOrder() {
						EndPoint = order.EndPoint != null ? order.EndPoint.Id : 0,
						Number = order.Number,
						BeginDate = order.BeginDate.GetValueOrDefault().ToShortDateString(),
						EndDate = order.EndDate.HasValue ? order.EndDate.Value.ToShortDateString() : "",
						OrderServices =
							order.OrderServices.Select(
								s => new OrderService() { Id = s.Id, Cost = s.Cost, Description = s.Description, IsPeriodic = s.IsPeriodic })
								.ToList(),
						ClientEndpoints = order.Client.Endpoints.Where(s => !s.Disabled).Select(s => s.Id).ToList()
					};
					return Json(orderBox, JsonRequestBehavior.AllowGet);
				}
			}
			if ((id == null || id == 0) && clientId != 0) {
				var client = DbSession.Get<Client>(clientId);
				if (client != null) {
					var orderBox = new ViewModelClientOrder() {
						EndPoint = 0,
						Number = client.LegalClientOrders.Select(s => s.Number).MaxOrDefault() + 1,
						BeginDate = SystemTime.Now().ToShortDateString(),
						EndDate = "",
						OrderServices = new List<OrderService>(),
						ClientEndpoints = client.Endpoints.Where(s => !s.Disabled).Select(s => s.Id).ToList()
					};
					return Json(orderBox, JsonRequestBehavior.AllowGet);
				}
			}
			return Json(null, JsonRequestBehavior.AllowGet);
		}
	}
}