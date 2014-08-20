using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Queries;
using InternetInterface.Services;
using NHibernate.Linq;
using TextHelper = InternetInterface.Helpers.TextHelper;

namespace InternetInterface.Controllers
{
	public class ClientFilter
	{
		public uint ClientCode { get; set; }
		public AppealType appealType { get; set; }
		public string grouped { get; set; }
		public bool Editing { get; set; }
		public bool EditConnectInfoFlag { get; set; }
		public int EditingConnect { get; set; }
	}

	public class SessionResult
	{
		public SessionResult(Internetsessionslog lease)
		{
			Lease = lease;
			Date = lease.LeaseBegin;
		}

		public SessionResult(Appeals appeal)
		{
			Appeal = appeal;
			Date = appeal.Date;
		}

		public DateTime Date { get; set; }
		public Internetsessionslog Lease { get; set; }
		public Appeals Appeal { get; set; }
	}


	[Helper(typeof(PaginatorHelper))]
	[Helper(typeof(TextHelper))]
	[Helper(typeof(BindingHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class UserInfoController : InternetInterfaceController
	{
		public void SearchUserInfo([DataBind("filter")] ClientFilter filter, [DataBind("userWO")] UserWriteOff writeOff)
		{
			var client = DbSession.Load<Client>(filter.ClientCode);
			PropertyBag["filter"] = filter;
			SendParam(filter, filter.grouped, filter.appealType);
			PropertyBag["Editing"] = filter.Editing;
			PropertyBag["appealType"] = filter.appealType;
			PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClient>(new PhysicalClient());
			PropertyBag["Switches"] = NetworkSwitch.All(DbSession, client.GetRegion());
			PropertyBag["RegionList"] = DbSession.Query<RegionHouse>().ToList();
		}

		public void Leases([DataBind("filter")] LeaseLogFilter filter)
		{
			PropertyBag["_client"] = DbSession.Load<Client>(filter.ClientCode);
			PropertyBag["filter"] = filter;
			PropertyBag["Leases"] = filter.Find(DbSession);
		}

		public void LawyerPersonInfo([DataBind("filter")] ClientFilter filter)
		{
			var client = DbSession.Load<Client>(filter.ClientCode);
			PropertyBag["grouped"] = filter.grouped;
			PropertyBag["filter"] = filter;
			PropertyBag["appealType"] = filter.appealType == 0 ? AppealType.User : filter.appealType;
			CommonEditorValues(client);

			var packagesForTariff = DbSession.Query<Tariff>().Select(t => t.PackageId).ToList();
			PropertyBag["Speeds"] =
				DbSession.Query<PackageSpeed>().Where(p => !packagesForTariff.Contains(p.PackageId)).OrderBy(p => p.PackageId);

			PropertyBag["Client"] = client.LawyerPerson;
			PropertyBag["UserInfo"] = false;
			PropertyBag["LegalPerson"] = client.LawyerPerson;
			if (PropertyBag["VB"] == null)
				PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(new LawyerPerson());

			PropertyBag["Editing"] = filter.Editing;
			var ordersInfo = client.GetOrderInfo(DbSession);
			PropertyBag["OrdersInfo"] = ordersInfo;

			LoadBalanceData(filter.grouped, client);

			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["Appeals"] = Appeals.GetAllAppeal(DbSession, client, filter.appealType);

			PropertyBag["EConnect"] = filter.EditingConnect;

			if(client.Orders.All(o => o.OrderServices.Count == 0))
				PropertyBag["Message"] = Message.Error("Не задана абонентская плата для клиента ! Клиент отключен !");

			PropertyBag["CallLogs"] = UnresolvedCall.LastCalls;
			PropertyBag["Contacts"] = client.Contacts.OrderBy(c => c.Type).ToList();
			PropertyBag["EditConnectInfoFlag"] = filter.EditConnectInfoFlag;
			PropertyBag["RegionList"] = RegionHouse.All(DbSession);
			SendConnectInfo(client);
			ConnectPropertyBag(filter.ClientCode);
			SendUserWriteOff();
		}

		[AccessibleThrough(Verb.Post)]
		public void BindPhone(uint clientCode, ulong phoneId)
		{
			var client = DbSession.Load<Client>(clientCode);
			var phone = DbSession.Load<UnresolvedCall>(phoneId);
			if (phone != null && client != null) {
				var number = phone.PhoneNumber;
				var registrator = InitializeContent.Partner;
				var contact = new Contact(registrator, client, ContactType.ConnectedPhone, number);
				DbSession.Save(contact);
				DbSession.Delete(phone);
				var appeal = new Appeals {
					Client = client,
					Date = DateTime.Now,
					AppealType = AppealType.System,
					Partner = registrator,
					Appeal = string.Format("Номер {0} был привязан к данному клиенту", number)
				};
				DbSession.Save(appeal);
			}
			RedirectToReferrer();
		}

		[AccessibleThrough(Verb.Post)]
		public void SaveContacts([ARDataBind("Contacts", AutoLoad = AutoLoadBehavior.NewInstanceIfInvalidKey)] Contact[] contacts, uint ClientID)
		{
			var client = DbSession.Load<Client>(ClientID);
			var telephoneRegex = new Regex(@"^(\d{10})$");
			foreach (var contact in contacts) {
				var replaseContact = contact.Text.Replace("-", string.Empty);
				contact.Text = telephoneRegex.IsMatch(replaseContact) ? replaseContact : contact.Text;
				contact.Client = client;
				contact.Registrator = InitializeContent.Partner;
				contact.Date = DateTime.Now;
				DbSession.Save(contact);
			}
			RedirectToUrl("../Search/Redirect.rails?filter.ClientCode=" + ClientID);
		}

		public void DeleteContact(uint contactId)
		{
			var contact = DbSession.Load<Contact>(contactId);
			DbSession.Delete(contact);
			RedirectToReferrer();
		}

		public void LoadContactEditModule(uint ClientID)
		{
			RedirectToUrl("../Search/Redirect.rails?filter.ClientCode=" + ClientID + "&filter.EditConnectInfoFlag=" + true);
		}

		public void ActivateService(uint clientId, uint serviceId, DateTime? startDate, DateTime? endDate)
		{
			var servise = DbSession.Load<Service>(serviceId);
			var client = DbSession.Load<Client>(clientId);
			var dtn = DateTime.Now;
			if (servise.InterfaceControl) {
				var clientService = new ClientService {
					Client = client,
					Service = servise,
					BeginWorkDate =
						startDate == null
							? dtn
							: new DateTime(startDate.Value.Year,
								startDate.Value.Month,
								startDate.Value.Day, dtn.Hour,
								dtn.Minute,
								dtn.Second),
					EndWorkDate = endDate == null
						? endDate
						: new DateTime(endDate.Value.Year,
							endDate.Value.Month,
							endDate.Value.Day,
							dtn.Hour,
							dtn.Minute,
							dtn.Second),
					Activator = InitializeContent.Partner
				};
				SetARDataBinder(AutoLoadBehavior.NullIfInvalidKey);
				BindObjectInstance(clientService, "clientService");

				try {
					Notify(client.Activate(clientService));
					if (client.IsNeedRecofiguration)
						SceHelper.UpdatePackageId(DbSession, client);
					DbSession.Save(client);
				}
				catch (ServiceActivationException e) {
					Error(e.Message);
				}
			}
			RedirectToUrl(client.Redirect());
		}

		public void DiactivateService(uint clientId, uint serviceId)
		{
			var client = DbSession.Load<Client>(clientId);
			var cservice = client.ClientServices.FirstOrDefault(c => c.Service.Id == serviceId && c.IsActivated);
			if (cservice != null) {
				Notify(client.Deactivate(cservice));
				if (client.IsNeedRecofiguration)
					SceHelper.UpdatePackageId(DbSession, client);
			}
			RedirectToUrl(client.Redirect());
		}

		[return: JSONReturnBinder]
		public string GetSubnet()
		{
			var mask = Int32.Parse(Request.Form["mask"]);
			return SubnetMask.CreateByNetBitLength(mask).ToString();
		}

		[return: JSONReturnBinder]
		public string GetStaticIp()
		{
			var endPontId = UInt32.Parse(Request.Form["endPontId"]);
			var lease = DbSession.Query<Lease>().FirstOrDefault(l => l.Endpoint.Id == endPontId);
			if (lease != null)
				return lease.Ip.ToString();
			return string.Empty;
		}

		public void SaveSwitchForClient(uint ClientID, [DataBind("ConnectInfo")] ConnectInfo ConnectInfo,
			uint BrigadForConnect,
			[ARDataBind("staticAdress", AutoLoad = AutoLoadBehavior.NewInstanceIfInvalidKey)] StaticIp[] staticAdress,
			uint EditConnect, string ConnectSum,
			[DataBind("order")] Order Order, bool withoutEndPoint, uint currentEndPoint)
		{
			var errors = ValidateDeep(Order);
			if(errors.ErrorsCount > 0) {
				Error(errors.ErrorMessages.First());
				RedirectToReferrer();
				return;
			}
			var needNewServiceForStaticIp = false;
			var settings = new Settings(DbSession);
			var client = DbSession.Load<Client>(ClientID);
			var newFlag = false;
			var clientEntPoint = new ClientEndpoint();

			//если это не создание ордера, то это его редактировани
			//если editConnect == 0, то это новый ордер
			var existingOrder = DbSession.Query<Order>().FirstOrDefault(o => o.Id == EditConnect);

			if (!client.IsPhysical()) {
				if (existingOrder != null) {
					if (existingOrder.EndPoint != null)
						clientEntPoint = existingOrder.EndPoint;
					else if (!withoutEndPoint && currentEndPoint == 0)
						newFlag = true;
				}
				else {
					newFlag = true;
				}
			}
			else {
				var endPoint = DbSession.Get<ClientEndpoint>(EditConnect);
				if (endPoint != null)
					clientEntPoint = endPoint;
				else
					newFlag = true;
			}

			var olpPort = clientEntPoint.Port;
			var oldSwitch = clientEntPoint.Switch;
			var nullFlag = false;
			if (ConnectInfo.static_IP == null) {
				clientEntPoint.Ip = null;
				nullFlag = true;
			}
			var errorMessage = Validation.ValidationConnectInfo(ConnectInfo, false, clientEntPoint.Id);
			decimal _connectSum;
			var validateSum =
				!(!string.IsNullOrEmpty(ConnectSum) && (!decimal.TryParse(ConnectSum, out _connectSum) || (_connectSum <= 0 && !client.IsPhysical())));
			if (!validateSum)
				errorMessage = "Введена невалидная сумма";

			bool savedEndpoint = false;
			if(!withoutEndPoint && currentEndPoint == 0) {
				if ((ConnectInfo.static_IP != string.Empty) || (nullFlag)) {
					if (validateSum && string.IsNullOrEmpty(errorMessage) || validateSum &&
						(oldSwitch != null && ConnectInfo.Switch == oldSwitch.Id && ConnectInfo.Port == olpPort.ToString())) {
						if (client.GetClientType() == ClientType.Phisical) {
							client.PhysicalClient.UpdatePackageId(clientEntPoint);
						}
						else {
							var packageSpeed = DbSession.Query<PackageSpeed>().Where(p => p.PackageId == ConnectInfo.PackageId).ToList().FirstOrDefault();
							clientEntPoint.PackageId = packageSpeed.PackageId;
						}
						if (clientEntPoint.Ip == null && !string.IsNullOrEmpty(ConnectInfo.static_IP))
							if (client.IsPhysical()) {
								DbSession.Save(new UserWriteOff(client, 200, string.Format("Плата за фиксированный Ip адрес ({0})", ConnectInfo.static_IP)));
							}
							else {
								needNewServiceForStaticIp = true;
							}
						clientEntPoint.Client = client;
						IPAddress address;
						if (ConnectInfo.static_IP != null && IPAddress.TryParse(ConnectInfo.static_IP, out address))
							clientEntPoint.Ip = address;
						else
							clientEntPoint.Ip = null;
						clientEntPoint.Port = Int32.Parse(ConnectInfo.Port);
						clientEntPoint.Switch = DbSession.Load<NetworkSwitch>(ConnectInfo.Switch);
						clientEntPoint.Monitoring = ConnectInfo.Monitoring;
						if (newFlag) {
							client.AddEndpoint(clientEntPoint, settings);
							if (client.AdditionalStatus != null && client.AdditionalStatus.ShortName == "Refused") {
								client.AdditionalStatus = null;
							}
							if (!client.IsPhysical())
								if (existingOrder == null)
									Order.EndPoint = clientEntPoint;
								else
									existingOrder.EndPoint = clientEntPoint;
						}
						if (newFlag || clientEntPoint.WhoConnected == null) {
							if (client.IsPhysical() && client.ConnectGraph != null) {
								clientEntPoint.WhoConnected = client.ConnectGraph.Brigad;
							}
							else {
								var brigad = DbSession.Get<Brigad>(BrigadForConnect);
								clientEntPoint.WhoConnected = brigad;
							}
						}
						client.ConnectedDate = DateTime.Now;
						if (client.Status.Id == (uint)StatusType.BlockedAndNoConnected)
							client.Status = DbSession.Load<Status>((uint)StatusType.BlockedAndConnected);
						client.SyncServices(settings);
						DbSession.Save(client);

						DbSession.Query<StaticIp>().Where(s => s.EndPoint == clientEntPoint).ToList().Where(
							s => !staticAdress.Select(f => f.Id).Contains(s.Id)).ToList()
							.ForEach(s => DbSession.Delete(s));

						foreach (var s in staticAdress) {
							if (!string.IsNullOrEmpty(s.Ip))
								if (Regex.IsMatch(s.Ip, NetworkSwitch.IPRegExp)) {
									s.EndPoint = clientEntPoint;
									DbSession.Save(s);
								}
						}

						var connectSum = 0m;
						if (!string.IsNullOrEmpty(ConnectSum) && decimal.TryParse(ConnectSum, out connectSum) && connectSum > 0) {
							var payments = DbSession.Query<PaymentForConnect>().Where(p => p.EndPoint == clientEntPoint).ToList();
							if (!payments.Any())
								DbSession.Save(new PaymentForConnect(connectSum, clientEntPoint));
							else {
								var payment = payments.First();
								payment.Sum = connectSum;
								DbSession.Save(payment);
							}
						}
						savedEndpoint = true;
					}
				}
				else {
					errorMessage = string.Empty;
					errorMessage += "Ошибка ввода IP адреса";
				}
			}

			if ((savedEndpoint || withoutEndPoint || currentEndPoint > 0) && !client.IsPhysical()) {
				if (existingOrder == null) {
					existingOrder = Order;
				}
				else {
					existingOrder.BeginDate = Order.BeginDate;
					existingOrder.EndDate = Order.EndDate;
					existingOrder.Number = Order.Number;
				}
				if (currentEndPoint > 0) {
					existingOrder.EndPoint = DbSession.Get<ClientEndpoint>(currentEndPoint);
				}
				if (existingOrder.Client == null)
					existingOrder.Client = client;

				DbSession.SaveOrUpdate(existingOrder);
				foreach (var orderService in existingOrder.OrderServices.ToList()) {
					if(Order.OrderServices.All(s => s.Id != orderService.Id)) {
						existingOrder.OrderServices.Remove(orderService);
						DbSession.Delete(orderService);
					}
				}

				if (needNewServiceForStaticIp) {
					var staticIpService = new OrderService {
						Cost = 200,
						Order = existingOrder,
						Description = string.Format("Плата за фиксированный Ip адрес ({0})", ConnectInfo.static_IP)
					};
					DbSession.Save(staticIpService);
					existingOrder.OrderServices.Add(staticIpService);
				}

				foreach (var orderService in Order.OrderServices) {
					if(orderService.Id > 0) {
						var service = existingOrder.OrderServices.First(s => s.Id == orderService.Id);
						service.Description = orderService.Description;
						service.IsPeriodic = orderService.IsPeriodic;
						service.Cost = orderService.Cost;
						DbSession.Update(service);
					}
					else {
						orderService.Order = existingOrder;
						existingOrder.OrderServices.Add(orderService);
						DbSession.Save(orderService);
					}
				}
				if (client.Disabled
					&& client.Orders.Count > 0)
					client.Disabled = false;
				DbSession.Save(client);
				DbSession.Save(existingOrder);
				RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID);
				return;
			}

			PropertyBag["Editing"] = true;
			PropertyBag["ChBrigad"] = BrigadForConnect;
			Error(errorMessage);
			RedirectToReferrer();
		}

		public void CreateAppeal(string Appeal, uint ClientID)
		{
			if (!string.IsNullOrEmpty(Appeal))
				DbSession.Save(new Appeals {
					Appeal = Appeal,
					Date = DateTime.Now,
					Partner = InitializeContent.Partner,
					Client = DbSession.Load<Client>(ClientID),
					AppealType = AppealType.User
				});
			RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID);
		}

		public void PassAndShowCard(uint ClientID)
		{
			if (Partner.AccesedPartner.Contains("SSI")) {
				var client = DbSession.Load<Client>(ClientID);
				var physicalClient = client.PhysicalClient;
				var password = CryptoPass.GeneratePassword();
				physicalClient.Password = CryptoPass.GetHashString(password);
				DbSession.Save(physicalClient);
				var endPoint = client.Endpoints.FirstOrDefault();
				if (endPoint != null)
					PropertyBag["WhoConnected"] = endPoint.WhoConnected;
				else {
					PropertyBag["WhoConnected"] = null;
				}
				PropertyBag["Client"] = physicalClient;
				PropertyBag["_client"] = client;
				PropertyBag["Password"] = password;
				PropertyBag["AccountNumber"] = client.Id.ToString("00000");
				PropertyBag["ConnectInfo"] = client.GetConnectInfo(DbSession).FirstOrDefault();
				RenderView("ClientRegisteredInfo");
			}
		}

		public void LoadEditConnectMudule(uint ClientID, int EditConnectFlag)
		{
			RedirectToUrl("../Search/Redirect.rails?filter.ClientCode=" + ClientID + "&filter.EditingConnect=" + EditConnectFlag);
		}

		[AccessibleThrough(Verb.Post)]
		public void LoadEditMudule(uint ClientID, AppealType appealType)
		{
			Flash["Editing"] = true;
			RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID + "&filter.Editing=true&filter.appealType=" + appealType);
		}

		public void ClientRegisteredInfo()
		{
		}

		public void ClientRegisteredInfoFromDiller()
		{
		}

		[AccessibleThrough(Verb.Post)]
		public void EditLawyerPerson(uint ClientID, int Speed, string grouped, AppealType appealType, string comment)
		{
			SetBinder(new DecimalValidateBinder { Validator = Validator });
			var _client = DbSession.Query<Client>().First(c => c.Id == ClientID);
			var updateClient = _client.LawyerPerson;

			BindObjectInstance(updateClient, ParamStore.Form, "LegalPerson");
			BindObjectInstance(_client, ParamStore.Form, "_client");

			if (IsValid(updateClient)) {
				if (!string.IsNullOrEmpty(comment)) {
					_client.LogComment = comment;
					updateClient.LogComment = comment;
				}

				_client.PostUpdate();
				DbSession.Save(_client);
				DbSession.Save(updateClient);

				RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID);
			}
			else {
				updateClient.SetValidationErrors(Validator.GetErrorSummary(updateClient));
				PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(updateClient);
				DbSession.Evict(updateClient);
				RenderView("LawyerPersonInfo");
				PropertyBag["LegalPerson"] = updateClient;
				PropertyBag["grouped"] = grouped;
				var filter = new ClientFilter {
					ClientCode = ClientID,
					grouped = grouped,
					appealType = appealType,
					Editing = true,
					EditingConnect = 0
				};
				LawyerPersonInfo(filter);
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void EditInformation(uint ClientID,
			string group, uint house_id, AppealType appealType, string comment,
			[DataBind("filter")] ClientFilter filter)
		{
			Message message = null;
			var client = DbSession.Load<Client>(ClientID);
			//var statusEntity = DbSession.Load<Status>(status);
			var updateClient = client.PhysicalClient;
			var oldStatus = client.Status;

			var iptv = client.Iptv;
			var internet = client.Internet;

			updateClient.UpdateHouse(DbSession.Get<House>(house_id));
			SetARDataBinder();
			BindObjectInstance(iptv, "iptv", AutoLoadBehavior.NullIfInvalidKey);
			BindObjectInstance(internet, "internet", AutoLoadBehavior.NullIfInvalidKey);
			BindObjectInstance(updateClient, ParamStore.Form, "Client", AutoLoadBehavior.NullIfInvalidKey);
			BindObjectInstance(client, ParamStore.Form, "_client");

			if (oldStatus != client.Status) {
				if (oldStatus.ManualSet) {
					if (client.Status.Type == StatusType.Dissolved && (client.HaveService<HardwareRent>() || client.HaveService<IpTvBoxRent>())) {
						GetErrorSummary(updateClient)
							.RegisterErrorMessage("Status", "Договор не может быть расторгнут тк у клиента имеется арендованное" +
								" оборудование, перед расторжением договора нужно изъять оборудование");
					}
				}
				else {
					client.Status = oldStatus;
					message = Message.Warning(string.Format("Статус не был изменен, т.к. нельзя изменить статус '{0}' вручную. Остальные данные были сохранены.", client.Status.Name));
				}
			}

			if (IsValid(updateClient)) {
				if (!string.IsNullOrEmpty(comment)) {
					client.LogComment = comment;
					updateClient.LogComment = comment;
				}

				client.PostUpdate();
				updateClient.UpdatePackageId();

				if (client.IsChanged(s => s.Status)) {
					if (client.Status.Type == StatusType.NoWorked) {
						client.AutoUnblocked = false;
						client.Disabled = true;
						client.StartNoBlock = null;
						client.Sale = 0;
						if (client.IsChanged(c => c.Disabled))
							client.CreareAppeal("Оператором клиент был заблокирован", AppealType.Statistic);
					}
					else if (client.Status.Type != StatusType.Dissolved) {
						client.AutoUnblocked = true;
						client.Disabled = false;
						client.ShowBalanceWarningPage = false;
						if (client.IsChanged(c => c.Disabled))
							client.CreareAppeal("Оператором клиент был разблокирован", AppealType.Statistic);
						if (client.IsChanged(c => c.ShowBalanceWarningPage))
							client.CreareAppeal("Оператором отключена страница Warning", AppealType.Statistic);
					}
					if (client.Status.Type == StatusType.Dissolved) {
						var endpointLog = client.Endpoints
							.Where(e => e.Switch != null)
							.Implode(e => String.Format("Коммутатор {0} порт {1}", e.Switch.Name, e.Port), Environment.NewLine);
						client.CreareAppeal(endpointLog, AppealType.System, false);
						client.Endpoints.Clear();
						client.PhysicalClient.HouseObj = null;
						client.Disabled = true;
						client.AutoUnblocked = false;
					}
				}

				if (!client.NeedShowWarning())
					client.ShowBalanceWarningPage = false;

				DbSession.SaveOrUpdate(updateClient);
				DbSession.SaveOrUpdate(client);
				if (message == null)
					message = Message.Notify("Данные изменены");
				Flash["Message"] = message;
				RedirectToUrl("../UserInfo/SearchUserInfo?filter.ClientCode=" + ClientID + "&filter.appealType=" + appealType);
			}
			else {
				updateClient.SetValidationErrors(Validator.GetErrorSummary(updateClient));
				PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClient>(updateClient);
				DbSession.Evict(updateClient);
				RenderView("SearchUserInfo");
				Flash["Editing"] = true;
				Flash["_client"] = client;
				Flash["Client"] = updateClient;
				filter.ClientCode = client.Id;
				PropertyBag["filter"] = filter;
				SendParam(filter, group, appealType);
			}
		}

		private void SendParam(ClientFilter filter, string grouped, AppealType appealType)
		{
			var client = DbSession.Load<Client>(filter.ClientCode);

			LoadBalanceData(grouped, client);
			PropertyBag["iptv"] = client.Iptv;
			PropertyBag["internet"] = client.Internet;
			PropertyBag["grouped"] = grouped;
			PropertyBag["BalanceText"] = string.Empty;

			CommonEditorValues(client);

			PropertyBag["Appeals"] = Appeals.GetAllAppeal(DbSession, client, appealType);
			PropertyBag["Client"] = client.PhysicalClient;
			PropertyBag["EditAddress"] = client.AdditionalStatus == null ? false : client.AdditionalStatus.ShortName == "Refused";

			PropertyBag["Regions"] = DbSession.Query<RegionHouse>().ToList();
			PropertyBag["ChHouse"] = client.PhysicalClient.HouseObj ?? new House();
			PropertyBag["Tariffs"] = Tariff.All(DbSession);
			PropertyBag["channels"] = ChannelGroup.All(DbSession);
			PropertyBag["ChStatus"] = client.Status != null ? client.Status.Id : DbSession.Query<Status>().First().Id;
			PropertyBag["naznach_text"] = DbSession.Query<ConnectGraph>().Count(c => c.Client.Id == filter.ClientCode) != 0
				? "Переназначить в график"
				: "Назначить в график";

			PropertyBag["UserInfo"] = true;
			PropertyBag["CallLogs"] = UnresolvedCall.LastCalls;
			PropertyBag["Contacts"] = client.Contacts.OrderBy(c => c.Type).ToList();

			if (client.Status.Id != (uint)StatusType.BlockedAndNoConnected)
				PropertyBag["EConnect"] = filter.EditingConnect;
			else
				PropertyBag["EConnect"] = 0;

			PropertyBag["EditConnectInfoFlag"] = filter.EditConnectInfoFlag;
			PropertyBag["sendSmsNotification"] = client.SendSmsNotification;
			PropertyBag["isService"] = false;
			PropertyBag["RegionList"] = DbSession.Query<RegionHouse>().ToList();
			ConnectPropertyBag(filter.ClientCode);
			SendConnectInfo(client);
			SendUserWriteOff();
		}

		private void CommonEditorValues(Client client)
		{
			PropertyBag["statuses"] = client.GetAvailableStatuses(DbSession);
			var services = DbSession.Query<Service>().OrderBy(s => s.HumanName).ToList();
			PropertyBag["services"] = services.Where(s => s.CanActivateInWeb(client)).ToList();
			PropertyBag["activeServices"] = client.ClientServices.Where(c => c.Service.InterfaceControl).OrderBy(s => s.Service.HumanName).ToList();
			PropertyBag["rentableHardwares"] = DbSession.Query<RentableHardware>().OrderBy(h => h.Name).ToList();
		}

		private void LoadBalanceData(string grouped, Client client)
		{
			var payments =
				DbSession.Query<Payment>().Where(p => p.Client.Id == client.Id).Where(p => p.Sum > 0).OrderByDescending(t => t.PaidOn).ToList();
			var writeoffSum = DbSession.Query<WriteOff>().Where(p => p.Client.Id == client.Id).ToList().Sum(s => s.WriteOffSum);
			var userWriteoffSum = DbSession.Query<UserWriteOff>().Where(w => w.Client.Id == client.Id).ToList().Sum(w => w.Sum);
			if (InitializeContent.Partner.IsDiller())
				payments = payments.Where(p => p.Agent != null && p.Agent.Partner == InitializeContent.Partner).OrderByDescending(t => t.PaidOn).Take(5).OrderBy(t => t.PaidOn).ToList();
			PropertyBag["Payments"] = payments;
			PropertyBag["paymentsSum"] = payments.Sum(p => p.Sum);
			PropertyBag["writeOffSum"] = writeoffSum + userWriteoffSum;
			PropertyBag["action"] = new PaymentMoveAction();

			PropertyBag["WriteOffs"] = client.GetWriteOffs(DbSession, grouped).OrderByDescending(w => w.WriteOffDate).ToList();
		}

		public void SendConnectInfo(Client client)
		{
			if (client.LawyerPerson != null) {
				var orderInfo = client.GetOrderInfo(DbSession);
				if (orderInfo.Count == 0) {
					var connectSum = client.IsPhysical() ? client.PhysicalClient.ConnectSum : 0;
					orderInfo.Add(new ClientOrderInfo {
						Order = new Order() { Number = Order.GetNextNumber(DbSession, client.Id) },
						ClientConnectInfo = new ClientConnectInfo { ConnectSum = connectSum }
					});
				}
				PropertyBag["ClientOrdersInfo"] = orderInfo;
			}
			else {
				var connectInfo = client.GetConnectInfo(DbSession);
				if (connectInfo.Count == 0) {
					var connectSum = client.IsPhysical() ? client.PhysicalClient.ConnectSum : 0;
					connectInfo.Add(new ClientConnectInfo { ConnectSum = connectSum });
				}
				PropertyBag["ClientConnectInf"] = connectInfo;
			}
		}

		public void ConnectPropertyBag(uint clientId)
		{
			var client = DbSession.Load<Client>(clientId);
			var brigads = Brigad.All(DbSession);
			PropertyBag["_client"] = client;
			PropertyBag["ClientCode"] = clientId;
			PropertyBag["Switches"] = NetworkSwitch.All(DbSession, client.GetRegion());
			PropertyBag["Brigads"] = brigads;
			var endPoint = client.Endpoints.FirstOrDefault();
			if (endPoint != null && endPoint.WhoConnected != null)
				PropertyBag["ChBrigad"] = endPoint.WhoConnected.Id;
			else {
				var brigad = brigads.FirstOrDefault();
				if (brigad != null)
					PropertyBag["ChBrigad"] = brigad.Id;
			}
			List<PackageSpeed> speeds;
			var tariffs = DbSession.Query<Tariff>().Select(t => t.PackageId).ToList();
			var clientEndPointId = 0u;
			var eConnect = Convert.ToUInt32(PropertyBag["EConnect"]);
			if (client.GetClientType() == ClientType.Phisical) {
				speeds = DbSession.Query<PackageSpeed>().Where(p => tariffs.Contains(p.PackageId)).ToList();
				clientEndPointId = eConnect;
			}
			else {
				speeds = DbSession.Query<PackageSpeed>().OrderBy(s => s.Speed).ToList();
				var order = DbSession.Get<Order>(eConnect);
				if (order != null && order.EndPoint != null)
					clientEndPointId = order.EndPoint.Id;
			}

			int? packageId;
			if (clientEndPointId > 0)
				packageId = DbSession.Load<ClientEndpoint>(clientEndPointId).PackageId;
			else
				packageId = client.Endpoints.Select(e => e.PackageId).FirstOrDefault();

			PropertyBag["ChSpeed"] = packageId;
			PropertyBag["Speeds"] = speeds;
		}

		private void SendUserWriteOff()
		{
			if (Flash["userWO"] != null) {
				var userVriteOffs = Flash["userWO"];
				Validator.IsValid(userVriteOffs);
				PropertyBag["userWO"] = userVriteOffs;
			}
			else
				PropertyBag["userWO"] = new UserWriteOff();
		}

		[AccessibleThrough(Verb.Post)]
		public void ChangeBalance(uint clientId, string balanceText, bool virtualPayment, string CommentText)
		{
			if (Partner.IsDiller())
				virtualPayment = false;

			Payment payment = null;
			var client = DbSession.Load<Client>(clientId);
			decimal tryBalance;
			if (decimal.TryParse(balanceText, out tryBalance) && tryBalance > 0) {
				if (client.LawyerPerson == null) {
					payment = new Payment {
						Client = client,
						Agent = Agent.GetByInitPartner(),
						PaidOn = DateTime.Now,
						RecievedOn = DateTime.Now,
						Sum = tryBalance,
						BillingAccount = false,
						Virtual = virtualPayment,
						Comment = CommentText
					};
					DbSession.Save(payment);
					Notify("Платеж ожидает обработки");
					Flash["sleepButton"] = true;
				}
				else {
					Error("Юридические лица не могут оплачивать наличностью");
				}
			}
			else {
				Error("Введена неверная сумма, должно быть положительное число.");
			}
			if (payment != null && Partner.ShowContractOfAgency && client.IsPhysical() && !payment.Virtual)
				Redirect("Payments", "ContractOfAgency", new { id = payment.Id });
			else
				RedirectToUrl(client.Redirect());
		}

		[AccessibleThrough(Verb.Post)]
		public void UserWriteOff(uint ClientID)
		{
			var client = DbSession.Load<Client>(ClientID);
			var writeOff = new UserWriteOff(client);
			BindObjectInstance(writeOff, "userWO");
			if (!HasValidationError(writeOff)) {
				DbSession.Save(writeOff);
				Notify("Списание ожидает обработки");
			}
			else
				Flash["userWO"] = writeOff;

			RedirectToUrl(client.Redirect());
		}

		public void AddInfo(uint ClientCode)
		{
			PropertyBag["ClientCode"] = ClientCode;
			LayoutName = "NoMap";
		}

		public void Refused(uint ClientID, string prichina, string Appeal)
		{
			var client = DbSession.Load<Client>(ClientID);
			client.AdditionalStatus = DbSession.Load<AdditionalStatus>((uint)AdditionalStatusType.Refused);
			client.Endpoints.Clear();
			client.PhysicalClient.HouseObj = null;
			DbSession.Save(client);
			foreach (var graph in DbSession.Query<ConnectGraph>().Where(c => c.Client == client)) {
				DbSession.Delete(graph);
			}
			CreateAppeal("Причина отказа:  " + prichina + " \r\n Комментарий: \r\n " + Appeal, ClientID);
			LayoutName = "NoMap";
		}

		public void NoPhoned(uint ClientCode)
		{
			PropertyBag["ClientCode"] = ClientCode;
			if (DateTime.Now.Hour <= 12)
				PropertyBag["StartDate"] = DateTime.Now.ToShortDateString();
			else
				PropertyBag["StartDate"] = DateTime.Now.AddDays(1).ToShortDateString();
			LayoutName = "NoMap";
		}

		public void NoPhoned(uint ClientID, string NoPhoneDate, string Appeal, string prichina)
		{
			var client = DbSession.Load<Client>(ClientID);
			client.AdditionalStatus = DbSession.Load<AdditionalStatus>((uint)AdditionalStatusType.NotPhoned);
			DateTime noPhoneDate;
			if (DateTime.TryParse(NoPhoneDate, out noPhoneDate)) {
				DbSession.Save(new Appeals {
					Appeal =
						"Причина недозвона:  " + prichina + " \r\n Дата: " + noPhoneDate.ToShortDateString() +
							" \r\n Комментарий: \r\n " + Appeal,
					Date = DateTime.Now,
					Partner = InitializeContent.Partner,
					Client = DbSession.Load<Client>(ClientID),
					AppealType = AppealType.User
				});
			}
			RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID);
		}

		public void AppointedToTheGraph(uint clientCode)
		{
			var client = DbSession.Load<Client>(clientCode);
			PropertyBag["client"] = client;
			PropertyBag["StartDate"] = DateTime.Now.AddDays(1).ToShortDateString();
		}

		[return: JSONReturnBinder]
		public object GetGraph(DateTime date, uint clientId)
		{
			var client = DbSession.Load<Client>(clientId);
			var brigads = Brigad.All(DbSession, client.GetRegion());
			var connectGraphs = DbSession.Query<ConnectGraph>().Where(g => brigads.Contains(g.Brigad) && g.Day.Date == date);
			return new {
				brigads = brigads.Select(b => new { b.Id, b.Name }).ToArray(),
				graphs = connectGraphs
					.Select(g => new { brigadId = g.Brigad.Id, clientId = g.Client != null ? g.Client.Id : 0, g.IntervalId })
					.ToArray(),
				intervals = ConnectGraph.GetIntervals()
			};
		}

		[return: JSONReturnBinder]
		public bool SaveGraph()
		{
			if (Request.Form["graph_button"] != null) {
				var client = DbSession.Load<Client>(Convert.ToUInt32(Request.Form["clientId"]));
				var but_id = Request.Form["graph_button"].Split('_');
				if (client.BeginWork == null)
					foreach (var graph in DbSession.Query<ConnectGraph>().Where(c => c.Client == client).ToList()) {
						DbSession.Delete(graph);
					}
				var briad = DbSession.Load<Brigad>(Convert.ToUInt32(but_id[1]));
				var interval = Convert.ToUInt32(but_id[0]);
				DbSession.Save(new ConnectGraph {
					IntervalId = interval,
					Brigad = briad,
					Client = client,
					Day = DateTime.Parse(Request.Form["graph_date"]),
				});
				client.AdditionalStatus = DbSession.Load<AdditionalStatus>((uint)AdditionalStatusType.AppointedToTheGraph);
				foreach (var clientEndpoint in client.Endpoints) {
					clientEndpoint.WhoConnected = briad;
					DbSession.Save(clientEndpoint);
				}
				DbSession.Save(client);
				var message = string.Format("Назначен в график, \r\n Бригада: {0} \r\n Дата: {1} \r\n Время: {2}",
					briad.Name,
					DateTime.Parse(Request.Form["graph_date"]).ToShortDateString(),
					ConnectGraph.GetIntervals()[(int)interval]);

				DbSession.Save(new Appeals(message, client, AppealType.User));
				return true;
			}
			else {
				return false;
			}
		}

		[return: JSONReturnBinder]
		public string ReservGraph()
		{
			var but_id = Request.Form["graph_button"].Split('_');
			var briad = DbSession.Load<Brigad>(Convert.ToUInt32(but_id[1]));
			var interval = Convert.ToUInt32(but_id[0]);
			DbSession.Save(new ConnectGraph {
				Brigad = briad,
				IntervalId = interval,
				Day = DateTime.Parse(Request.Form["graph_date"])
			});
			return "Время зарезервировано";
		}

		[return: JSONReturnBinder]
		public bool DeleteGraph()
		{
			var date = DateTime.Parse(Request.Form["date"]);
			var client = DbSession.Load<Client>(Convert.ToUInt32(Request.Form["clientId"]));
			var briad = DbSession.Load<Brigad>(Convert.ToUInt32(Request.Form["brigad"]));
			var interval = uint.Parse(Request.Form["interval"]);
			var graph = DbSession.QueryOver<ConnectGraph>().Where(c => c.Client == client && c.Day == date && c.IntervalId == interval && c.Brigad == briad).List().FirstOrDefault();
			if (graph != null) {
				DbSession.Delete(graph);
				var appeal = client.CreareAppeal(string.Format("Удалено назначение в график, \r\n Бригада: {0} \r\n Дата: {1} \r\n Время: {2}",
					briad.Name,
					date.ToShortDateString(),
					ConnectGraph.GetIntervals()[(int)interval]), AppealType.User);
				DbSession.Save(appeal);
				return true;
			}
			return false;
		}

		public void Administration()
		{
		}

		public void RequestGraph(DateTime selectDate, uint brig)
		{
			var brigads = Brigad.All(DbSession);
			PropertyBag["selectDate"] = selectDate != DateTime.MinValue ? selectDate : DateTime.Now;
			PropertyBag["Brigad"] = DbSession.Get<Brigad>(brig) ?? brigads.FirstOrDefault();
			PropertyBag["Brigads"] = brigads;
			PropertyBag["Intervals"] = ConnectGraph.GetIntervals();
		}

		public void CreateAndPrintGraph(uint Brig, DateTime selectDate)
		{
			PropertyBag["Clients"] =
				DbSession.Query<ConnectGraph>().Where(c => c.Brigad.Id == Brig && c.Day.Date == selectDate.Date).Select(
					s => s.Client).Where(c => c != null).ToList();
			PropertyBag["selectDate"] = selectDate;
			PropertyBag["Brigad"] = DbSession.Load<Brigad>(Brig);
			PropertyBag["Intervals"] = ConnectGraph.GetIntervals();
		}

		public void ShowAppeals([DataBind("filter")] AppealFilter filter)
		{
			PropertyBag["appeals"] = filter.Find(DbSession);
			PropertyBag["filter"] = filter;
		}

		[AccessibleThrough(Verb.Post)]
		public void AddEndPoint(uint clientId)
		{
			RedirectToAction("AddPoint", new Dictionary<string, string> { { "clientId", clientId.ToString() } });
		}

		public void AddPoint(uint clientId)
		{
			PropertyBag["OrderInfo"] = new ClientOrderInfo {
				Order = new Order { Number = Order.GetNextNumber(DbSession, clientId) },
				ClientConnectInfo = new ClientConnectInfo()
			};
			ConnectPropertyBag(clientId);
		}

		[AccessibleThrough(Verb.Post)]
		public void DeleteEndPoint(uint endPointForDelete)
		{
			var endPoint = DbSession.Get<ClientEndpoint>(endPointForDelete);
			if (endPoint != null) {
				var client = endPoint.Client;
				if (client.RemoveEndpoint(endPoint))
					DbSession.Save(endPoint);
				else
					Error("Последняя точка подключения не может быть удалена!");
			}
			RedirectToReferrer();
		}

		[AccessibleThrough(Verb.Post)]
		public void RemakeVirginityClient(uint clientId)
		{
			var client = DbSession.Get<Client>(clientId);
			client.Status = Status.Get(StatusType.BlockedAndConnected, DbSession);
			client.BeginWork = null;
			client.RatedPeriodDate = null;
			if (client.ConnectGraph != null)
				DbSession.Delete(client.ConnectGraph);
			DbSession.Save(client);
			RedirectToReferrer();
		}

		public void ShowRegions()
		{
			PropertyBag["Regions"] = DbSession.Query<RegionHouse>().ToList();
		}

		public void EditRegion(uint id)
		{
			var region = DbSession.Load<RegionHouse>(id);
			PropertyBag["Region"] = region;
			if (IsPost) {
				BindObjectInstance(region, "Region");
				if (IsValid(region)) {
					DbSession.SaveOrUpdate(region);
					Notify("Сохранено");
					RedirectToAction("ShowRegions");
				}
			}
		}

		[return: JSONReturnBinder]
		public object RegisterRegion()
		{
			var regionName = Request.Form["RegionName"];
			var region = new RegionHouse();
			region.Name = regionName;
			DbSession.Save(region);
			return new { region.Id, regionName };
		}

		public void AddOrderService(uint orderId)
		{
			var order = DbSession.Load<Order>(orderId);
			PropertyBag["OrderService"] = new OrderService();
			PropertyBag["orderId"] = orderId;
			PropertyBag["PageDescription"] = "Добавление новой услуги";
			RenderView("EditOrderService");
		}

		public void SaveOrderService(uint orderId, [DataBind("OrderService")] OrderService orderService)
		{
			if(orderService.Order == null) {
				orderService.Order = DbSession.Load<Order>(orderId);
			}
			DbSession.SaveOrUpdate(orderService);
			RedirectToUrl("../Search/Redirect?filter.ClientCode=" + orderService.Order.Client.Id);
		}

		public void EditOrderService(uint orderServiceId)
		{
			var orderService = DbSession.Load<OrderService>(orderServiceId);
			PropertyBag["OrderService"] = orderService;
			PropertyBag["orderId"] = orderService.Order.Id;
			PropertyBag["PageDescription"] = "Редактирование услуги";
			RenderView("EditOrderService");
		}

		public void CloseOrder(uint orderId, DateTime? orderCloseDate)
		{
			var order = DbSession.Load<Order>(orderId);
			if (orderCloseDate != null)
				order.EndDate = orderCloseDate;
			else {
				order.Disabled = true;
				order.EndDate = DateTime.Today;
			}
			DbSession.Save(order);
			RedirectToUrl(order.Client.Redirect());
			var message = MessageOrderHelper.GenerateText(order, "закрытие");
			message += MessageOrderHelper.GenerateTextService(order);
			MessageOrderHelper.SendMail("Уведомление о закрытии заказа", message);
			var appeal = new Appeals(message, order.Client, AppealType.System);
			DbSession.Save(appeal);
		}

		public void OrdersArchive(uint clientCode)
		{
			var client = DbSession.Load<Client>(clientCode);
			var ordersInfo = client.GetOrderInfo(DbSession, true);
			PropertyBag["OrdersInfo"] = ordersInfo;
		}

		public void DeleteWriteOff(uint id, bool userWriteOff)
		{
			IWriteOff writeOff;
			if (!userWriteOff)
				writeOff = DbSession.Get<WriteOff>(id);
			else
				writeOff = DbSession.Get<UserWriteOff>(id);
			var message = writeOff.Cancel();
			DbSession.Delete(writeOff);
			DbSession.Save(message);
			Notify("Удалено");
			var mailer = this.Mailer<Mailer>();
			mailer.SendText("internet@ivrn.net", "internet@ivrn.net", "Уведомление об удалении списания", string.Format(@"
Отменено списание №{0}
Клиент: №{1} - {2}
Сумма: {3}
Оператор: {4}
", writeOff.Id, writeOff.Client.Id, writeOff.Client.Name, writeOff.Sum.ToString("#.00"), InitializeContent.Partner.Name));
			RedirectToReferrer();
		}

		public void Statistic(uint region)
		{
			PropertyBag["regionSet"] = region;
			PropertyBag["Regions"] = DbSession.Query<RegionHouse>().ToList();
			PropertyBag["Stat"] = new Statistic(DbSession, region).GetStatistic();
		}
	}
}