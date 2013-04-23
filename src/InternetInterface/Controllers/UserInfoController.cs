using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Helpers;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.AllLogic;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Queries;
using InternetInterface.Services;
using NHibernate.Linq;
using TextHelper = InternetInterface.Helpers.TextHelper;
using NHibernate;

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

	public class SessionFilter : IPaginable
	{
		public uint ClientCode { get; set; }
		public DateTime? beginDate { get; set; }
		public DateTime? endDate { get; set; }

		private int _lastRowsCount;

		public int RowsCount
		{
			get { return _lastRowsCount; }
		}

		public int PageSize
		{
			get { return 20; }
		}

		public int CurrentPage { get; set; }

		public string[] ToUrl()
		{
			return new[] {
				String.Format("filter.ClientCode={0}", ClientCode),
				String.Format("filter.beginDate={0}", beginDate),
				String.Format("filter.endDate={0}", endDate)
			};
		}

		public string ToUrlQuery()
		{
			return string.Join("&", ToUrl());
		}

		public string GetUri()
		{
			return ToUrlQuery();
		}

		public List<SessionResult> Find(ISession session)
		{
			var result = new List<SessionResult>();
			var thisD = DateTime.Now;
			if (beginDate == null)
				beginDate = new DateTime(thisD.Year, thisD.Month, 1);
			if (endDate == null)
				endDate = DateTime.Now;
			Expression<Func<Internetsessionslog, bool>> predicate = i =>
				i.EndpointId.Client.Id == ClientCode && i.LeaseBegin.Date >= beginDate.Value.Date
					&& (i.LeaseEnd.Value.Date <= endDate.Value.Date
						|| i.LeaseEnd == null);

			var appeal = session.Query<Appeals>().Where(a =>
				a.Client.Id == ClientCode &&
					a.AppealType == AppealType.Statistic &&
					a.Date.Date >= beginDate.Value.Date &&
					a.Date.Date <= endDate.Value.Date)
				.ToList().Select(a => new SessionResult(a)).ToList();
			_lastRowsCount = Internetsessionslog.Queryable.Where(predicate).Count();
			_lastRowsCount += appeal.Count;
			int getCount = 0;
			if (_lastRowsCount > 0) {
				getCount = _lastRowsCount - PageSize * CurrentPage < PageSize ? _lastRowsCount - PageSize * CurrentPage : PageSize;
				result = Internetsessionslog.Queryable.Where(predicate)
					.ToList().Select(i => new SessionResult(i)).ToList();
			}
			result.AddRange(appeal);
			return result.OrderBy(r => r.Date).Skip(PageSize * CurrentPage)
				.Take(getCount).ToList();
		}
	}


	[Helper(typeof(PaginatorHelper))]
	[Helper(typeof(TextHelper))]
	[Helper(typeof(FormHelper))]
	[Helper(typeof(BindingHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class UserInfoController : BaseController
	{
		public void SearchUserInfo([DataBind("filter")] ClientFilter filter, [DataBind("userWO")] UserWriteOff writeOff)
		{
			var client = Client.Find(filter.ClientCode);
			PropertyBag["filter"] = filter;
			SendParam(filter, filter.grouped, filter.appealType);
			PropertyBag["Editing"] = filter.Editing;
			PropertyBag["appealType"] = filter.appealType;
			PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClient>(new PhysicalClient());
			PropertyBag["Switches"] = NetworkSwitch.All(DbSession);
			PropertyBag["RegionList"] = DbSession.Query<RegionHouse>().ToList();
		}

		public void Leases([DataBind("filter")] SessionFilter filter)
		{
			PropertyBag["_client"] = Client.Find(filter.ClientCode);
			PropertyBag["filter"] = filter;
			PropertyBag["Leases"] = filter.Find(DbSession);
		}

		public void LawyerPersonInfo([DataBind("filter")] ClientFilter filter)
		{
			var client = Client.Find(filter.ClientCode);

			if (client.Status != null)
				PropertyBag["ChStatus"] = client.Status.Id;
			else
				PropertyBag["ChStatus"] = Status.FindFirst().Id;
			PropertyBag["grouped"] = filter.grouped;
			PropertyBag["filter"] = filter;
			PropertyBag["appealType"] = filter.appealType == 0 ? AppealType.User : filter.appealType;
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["services"] = Service.FindAll();

			var packagesForTariff = Tariff.Queryable.Select(t => t.PackageId).ToList();
			PropertyBag["Speeds"] =
				PackageSpeed.Queryable.Where(p => !packagesForTariff.Contains(p.PackageId)).OrderBy(p => p.PackageId);

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

			if((client.Orders == null || client.Orders.All(o => o.OrderServices == null)) && client.LawyerPerson.Tariff == null)
				PropertyBag["Message"] = Message.Error("Не задана абонентская плата для клиента ! Клиент отключен !");

			PropertyBag["CallLogs"] = UnresolvedCall.LastCalls;
			PropertyBag["Contacts"] = client.Contacts.OrderBy(c => c.Type).ToList();
			PropertyBag["EditConnectInfoFlag"] = filter.EditConnectInfoFlag;
			PropertyBag["RegionList"] = RegionHouse.All();
			SendConnectInfo(client);
			ConnectPropertyBag(filter.ClientCode);
			SendUserWriteOff();
		}

		public void PostponedPayment(uint ClientID)
		{
			var client = Client.Find(ClientID);
			var pclient = client.PhysicalClient;
			var message = string.Empty;
			if (client.ClientServices.Select(c => c.Service).Contains(Service.GetByType(typeof(DebtWork))))
				message += "Повторное использование услуги \"Обещаный платеж невозможно\"";
			if (pclient.Balance > 0 && string.IsNullOrEmpty(message))
				message += "Воспользоваться устугой возможно только при отрицательном балансе";
			if ((!client.Disabled || !client.AutoUnblocked) && string.IsNullOrEmpty(message))
				message += "Услуга \"Обещанный платеж\" недоступна";
			if (!client.PaymentForTariff())
				message +=
					"Воспользоваться услугой возможно если все платежи клиента первышают его абонентскую плату за месяц";
			if (client.CanUsedPostponedPayment()) {
				client.Disabled = false;
				client.Update();
				message += "Услуга \"Обещанный платеж активирована\"";
				new Appeals {
					Appeal = "Услуга \"Обещанный платеж активирована\"",
					AppealType = AppealType.Statistic,
					Client = client,
					Date = DateTime.Now,
					Partner = InitializeContent.Partner
				}.Save();
			}
			Flash["Applying"] = message;
			RedirectToUrl("../UserInfo/SearchUserInfo.rails?filter.ClientCode=" + ClientID);
		}

		[AccessibleThrough(Verb.Post)]
		public void BindPhone(uint clientCode, ulong phoneId)
		{
			var client = Client.Find(clientCode);
			var phone = UnresolvedCall.Find(phoneId);
			if (phone != null && client != null) {
				var number = phone.PhoneNumber;
				var registrator = InitializeContent.Partner;
				var contact = new Contact(registrator, client, ContactType.ConnectedPhone, number);
				DbSession.Save(contact);
				phone.Delete();
				var appeal = new Appeals {
					Client = client,
					Date = DateTime.Now,
					AppealType = AppealType.System,
					Partner = registrator,
					Appeal = string.Format("Номер {0} был привязян к данному клиенту", number)
				};
				DbSession.Save(appeal);
			}
			RedirectToReferrer();
		}

		[AccessibleThrough(Verb.Post)]
		public void SaveContacts([ARDataBind("Contacts", AutoLoad = AutoLoadBehavior.NewInstanceIfInvalidKey)] Contact[] contacts, uint ClientID)
		{
			var client = Client.Find(ClientID);
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
			var servise = Service.Find(serviceId);
			var client = Client.Find(clientId);
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

				try {
					client.Activate(clientService);
					DbSession.Save(clientService);
					var appeal = Appeals.CreareAppeal(
						string.Format("Услуга \"{0}\" активирована на период с {1} по {2}", servise.HumanName,
							clientService.BeginWorkDate != null
								? clientService.BeginWorkDate.Value.ToShortDateString()
								: DateTime.Now.ToShortDateString(),
							clientService.EndWorkDate != null
								? clientService.EndWorkDate.Value.ToShortDateString()
								: string.Empty),
						client,
						AppealType.Statistic);
					DbSession.SaveOrUpdate(appeal);
					Flash["Message"] = Message.Notify(appeal.Appeal);
				}
				catch (ServiceActivationException e) {
					Flash["errorMessage"] = e.Message;
				}
			}
			RedirectToUrl(client.Redirect());
		}

		public void DiactivateService(uint clientId, uint serviceId)
		{
			var servise = Service.Find(serviceId);
			var client = Client.Find(clientId);
			if (client.ClientServices != null) {
				var cservice =
					client.ClientServices.FirstOrDefault(c => c.Service.Id == serviceId && c.Activated);
				if (cservice != null) {
					cservice.CompulsoryDeactivate();
					var appeal = Appeals.CreareAppeal(string.Format("Услуга \"{0}\" деактивирована", servise.HumanName), client, AppealType.Statistic);
					ActiveRecordMediator.Save(appeal);
				}
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
			var lease = Lease.Queryable.FirstOrDefault(l => l.Endpoint.Id == endPontId);
			if (lease != null)
				return NetworkSwitch.GetNormalIp(Convert.ToUInt32(lease.Ip));
			return string.Empty;
		}

		public void SaveSwitchForClient(uint ClientID, [DataBind("ConnectInfo")] ConnectInfo ConnectInfo,
			uint BrigadForConnect,
			[ARDataBind("staticAdress", AutoLoad = AutoLoadBehavior.NewInstanceIfInvalidKey)] StaticIp[] staticAdress,
			uint EditConnect, string ConnectSum,
			[DataBind("order")] Orders Order, bool withoutEndPoint)
		{
			var client = Client.Find(ClientID);
			bool brigadChangeFlag = client.WhoConnected == null;
			var newFlag = false;
			var clientEntPoint = new ClientEndpoint();
			var existingOrder = DbSession.Query<Orders>().FirstOrDefault(o => o.Id == EditConnect);
			if (!client.IsPhysical()) {
				if (existingOrder != null) {
					if (existingOrder.EndPoint != null)
						clientEntPoint = existingOrder.EndPoint;
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
			else {
				ConnectInfo.static_IP = NetworkSwitch.SetProgramIp(ConnectInfo.static_IP);
			}
			var errorMessage = Validation.ValidationConnectInfo(ConnectInfo, false);
			decimal _connectSum;
			var validateSum =
				!(!string.IsNullOrEmpty(ConnectSum) && (!decimal.TryParse(ConnectSum, out _connectSum) || (_connectSum <= 0 && !client.IsPhysical())));
			if (!validateSum)
				errorMessage = "Введена невалидная сумма";

			bool savedEndpoint = false;
			if(!withoutEndPoint) {
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
						if (string.IsNullOrEmpty(clientEntPoint.Ip) && !string.IsNullOrEmpty(ConnectInfo.static_IP))
							new UserWriteOff {
								Client = client,
								Date = DateTime.Now,
								Sum = 200,
								Comment = string.Format("Плата за фиксированный Ip адрес ({0})", NetworkSwitch.GetNormalIp(UInt32.Parse(ConnectInfo.static_IP))),
								Registrator = InitializeContent.Partner
							}.Save();
						clientEntPoint.Client = client;
						clientEntPoint.Ip = ConnectInfo.static_IP;
						clientEntPoint.Port = Int32.Parse(ConnectInfo.Port);
						clientEntPoint.Switch = DbSession.Load<NetworkSwitch>(ConnectInfo.Switch);
						clientEntPoint.Monitoring = ConnectInfo.Monitoring;
						if (!newFlag) {
							DbSession.SaveOrUpdate(clientEntPoint);
						}
						else {
							clientEntPoint.SaveAndFlush();
							if (client.AdditionalStatus != null && client.AdditionalStatus.ShortName == "Refused") {
								client.AdditionalStatus = null;
								client.Update();
							}
							Order.EndPoint = clientEntPoint;
						}
						if (brigadChangeFlag) {
							var brigad = Brigad.Find(BrigadForConnect);
							client.WhoConnected = brigad;
							client.WhoConnectedName = brigad.Name;
						}
						client.ConnectedDate = DateTime.Now;
						if (client.Status.Id == (uint)StatusType.BlockedAndNoConnected)
							client.Status = Status.Find((uint)StatusType.BlockedAndConnected);
						client.Update();

						StaticIp.Queryable.Where(s => s.EndPoint == clientEntPoint).ToList().Where(
							s => !staticAdress.Select(f => f.Id).Contains(s.Id)).ToList()
							.ForEach(s => s.Delete());

						foreach (var s in staticAdress) {
							if (!string.IsNullOrEmpty(s.Ip))
								if (Regex.IsMatch(s.Ip, NetworkSwitch.IPRegExp)) {
									s.EndPoint = clientEntPoint;
									s.Save();
								}
						}

						var connectSum = 0m;
						if (!string.IsNullOrEmpty(ConnectSum) && decimal.TryParse(ConnectSum, out connectSum) && connectSum > 0) {
							var payments = PaymentForConnect.Queryable.Where(p => p.EndPoint == clientEntPoint).ToList();
							if (payments.Count() == 0)
								new PaymentForConnect {
									Sum = connectSum,
									Partner = InitializeContent.Partner,
									EndPoint = clientEntPoint,
									RegDate = DateTime.Now
								}.Save();
							else {
								var payment = payments.First();
								payment.Sum = connectSum;
								payment.Update();
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

			if (savedEndpoint || withoutEndPoint) {
				if (existingOrder == null) {
					existingOrder = Order;
				}
				else {
					existingOrder.BeginDate = Order.BeginDate;
					existingOrder.EndDate = Order.EndDate;
					existingOrder.Number = Order.Number;
				}
				if (existingOrder.Client == null)
					existingOrder.Client = client;
				if(existingOrder.OrderServices == null)
					existingOrder.OrderServices = new List<OrderService>();
				DbSession.SaveOrUpdate(existingOrder);

				if(Order.OrderServices == null)
					Order.OrderServices = new List<OrderService>();

				foreach (var orderService in existingOrder.OrderServices) {
					if(Order.OrderServices.All(s => s.Id != orderService.Id)) {
						DbSession.Delete(orderService);
					}
				}

				// создаем новый акт
				Act act = null;
				if(Order.OrderServices.Any(o => !o.IsPeriodic)) {
					act = new Act(client /*, existingOrder*/);
					DbSession.Save(act);
				}
				// создаем новый счет
				Invoice invoice = null;
				if(Order.OrderServices.Any(o => o.IsPeriodic)) {
					invoice = new Invoice(client);
					DbSession.Save(invoice);
				}
				// создаем новый договор
				var contract = new Contract(existingOrder);
				DbSession.Save(contract);

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
						DbSession.Save(orderService);
					}
					// при сохранении заказа сохраняем начисления для разовых услуг в акте и для периодических в счете
					if(!orderService.IsPeriodic) {
						var partAct = new ActPart(act) {
							Count = 1,
							Cost = orderService.Cost,
							Name = orderService.Description + " по заказу №" + orderService.Order.Number,
							OrderService = orderService
						};
						DbSession.Save(partAct);
					}
					else {
						var daysInMonth = DateTime.DaysInMonth(Order.BeginDate.Value.Year, Order.BeginDate.Value.Month);
						var partInvoice = new InvoicePart(invoice,
							1,
							orderService.Cost / daysInMonth * (daysInMonth - Order.BeginDate.Value.Day),
							orderService.Description + " по заказу №" + orderService.Order.Number);
						partInvoice.OrderService = orderService;
						DbSession.Save(partInvoice);
					}
				}
				if(act != null) {
					DbSession.Refresh(act);
					act.CalculateSum();
					DbSession.Save(act);
				}
				if(invoice != null) {
					DbSession.Refresh(invoice);
					invoice.CalculateSum();
					DbSession.Save(invoice);
				}
				RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID);
				return;
			}

			PropertyBag["Editing"] = true;
			PropertyBag["ChBrigad"] = BrigadForConnect;
			Flash["errorMessage"] = errorMessage;
			RedirectToReferrer();
		}

		public void CreateAppeal(string Appeal, uint ClientID)
		{
			if (!string.IsNullOrEmpty(Appeal))
				new Appeals {
					Appeal = Appeal,
					Date = DateTime.Now,
					Partner = InitializeContent.Partner,
					Client = Client.Find(ClientID),
					AppealType = AppealType.User
				}.SaveAndFlush();
			RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID);
		}

		public void PassAndShowCard(uint ClientID)
		{
			if (CategorieAccessSet.AccesPartner("SSI")) {
				var _client = Client.Find(ClientID);
				var client = _client.PhysicalClient;
				var Password = CryptoPass.GeneratePassword(ClientID);
				client.Password = CryptoPass.GetHashString(Password);
				client.UpdateAndFlush();
				PropertyBag["WhoConnected"] = _client.WhoConnected;
				PropertyBag["Client"] = client;
				PropertyBag["_client"] = _client;
				PropertyBag["Password"] = Password;
				PropertyBag["AccountNumber"] = _client.Id.ToString("00000");
				PropertyBag["ConnectInfo"] = _client.GetConnectInfo(DbSession).FirstOrDefault();
				RenderView("ClientRegisteredInfo");
			}
		}

		public void LoadEditConnectMudule(uint ClientID, int EditConnectFlag)
		{
			RedirectToUrl("../Search/Redirect.rails?filter.ClientCode=" + ClientID + "&filter.EditingConnect=" + EditConnectFlag);
		}

		private void SendRequestEditParameter()
		{
			PropertyBag["labelColors"] = ColorWork.GetColorSet();
			PropertyBag["LabelName"] = string.Empty;
			PropertyBag["Labels"] = Label.FindAll();
		}

		public void EditLabel(uint deletelabelch, string LabelName, string labelcolor)
		{
			var labelForEdit = Label.Find(deletelabelch);
			if (labelForEdit != null && labelForEdit.Deleted) {
				if (LabelName != null)
					labelForEdit.Name = LabelName;
				if (labelcolor != "#000000") {
					labelForEdit.Color = labelcolor;
				}
				labelForEdit.UpdateAndFlush();
			}
			RedirectToUrl("../UserInfo/RequestView.rails");
		}

		public void DeleteLabel(uint deletelabelch)
		{
			var labelForDel = Label.Find(deletelabelch);
			if (labelForDel != null && labelForDel.Deleted) {
				labelForDel.DeleteAndFlush();
				DbSession.CreateSQLQuery(
					@"update internet.Requests R
set r.`Label` = null,
r.`ActionDate` = :ActDate,
r.`Operator` = :Oper
where r.`Label`= :LabelIndex;")
					.SetParameter("LabelIndex", deletelabelch)
					.SetParameter("ActDate", DateTime.Now)
					.SetParameter("Oper", InitializeContent.Partner.Id)
					.ExecuteUpdate();
			}
			RedirectToUrl("../UserInfo/RequestView.rails");
		}

		public void RequestView([DataBind("filter")] RequestFilter filter)
		{
			var requests = filter.Find();
			PropertyBag["Clients"] = InitializeContent.Partner.Categorie.ReductionName == "Agent"
				? requests.Where(r => r.Registrator == InitializeContent.Partner).ToList()
				: requests.ToList();
			PropertyBag["filter"] = filter;
			PropertyBag["Direction"] = filter.Direction;
			PropertyBag["SortBy"] = filter.SortBy;
			SendRequestEditParameter();
		}

		public void RequestOne(uint id)
		{
			PropertyBag["Request"] = Models.Request.Find(id);
			PropertyBag["Messages"] = RequestMessage.Queryable.Where(r => r.Request.Id == id).ToList();
		}

		public void CreateRequestComment(uint requestId, string comment)
		{
			if (!string.IsNullOrEmpty(comment))
				new RequestMessage {
					Date = DateTime.Now,
					Registrator = InitializeContent.Partner,
					Comment = comment,
					Request = Models.Request.Find(requestId)
				}.Save();
			RedirectToReferrer();
		}

		public void RequestInArchive(uint id, bool action)
		{
			var request = Models.Request.Find(id);
			request.Archive = action;
			request.Update();
			RedirectToReferrer();
		}

		/// <summary>
		/// Создать новую метку
		/// </summary>
		/// <param name="LabelName"></param>
		/// <param name="labelcolor"></param>
		public void CreateLabel(string LabelName, string labelcolor)
		{
			if (!string.IsNullOrEmpty(LabelName)) {
				new Label {
					Color = labelcolor,
					Name = LabelName,
					Deleted = true
				}.SaveAndFlush();
			}
			Flash["Message"] = Message.Error("Нельзя создать метку без имени");
			RedirectToAction("RequestView");
		}

		/// <summary>
		/// Устанавливает метки на клиентов
		/// </summary>
		/// <param name="labelList"></param>
		/// <param name="labelch"></param>
		[AccessibleThrough(Verb.Post)]
		public void SetLabel([DataBind("LabelList")] List<uint> labelList, uint labelch)
		{
			var _label = Label.Find(labelch);
			foreach (var label in labelList) {
				var request = Models.Request.Find(label);
				if ((request.Label == null) ||
					(request.Label.ShortComment != "Refused" && request.Label.ShortComment != "Registered")) {
					request.Label = _label;
					request.ActionDate = DateTime.Now;
					request.Operator = InitializeContent.Partner;
					if (_label.ShortComment == "Refused" || _label.ShortComment == "Deleted" || _label.ShortComment == "Registered") {
						request.Archive = true;
					}
					request.UpdateAndFlush();
				}
			}
			RedirectToReferrer();
		}

		public void InforoomUsersPreview()
		{
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

		public void PartnerRegisteredInfo(int hiddenPartnerId, string hiddenPass)
		{
			if (Flash["Partner"] == null) {
				RedirectToUrl("../Register/RegisterPartner.rails");
			}
		}

		public void PartnersPreview(uint catType)
		{
			PropertyBag["Partners"] = Partner.FindAllSort().Where(p => p.Categorie.Id == catType).ToList();
		}

		[AccessibleThrough(Verb.Post)]
		public void EditLawyerPerson(uint ClientID, int Speed, string grouped, AppealType appealType, string comment)
		{
			SetBinder(new DecimalValidateBinder { Validator = Validator });
			var _client = Client.Queryable.First(c => c.Id == ClientID);
			var updateClient = _client.LawyerPerson;

			BindObjectInstance(updateClient, ParamStore.Form, "LegalPerson");
			BindObjectInstance(_client, ParamStore.Form, "_client");

			if (IsValid(updateClient)) {
				if (!string.IsNullOrEmpty(comment)) {
					_client.LogComment = comment;
					updateClient.LogComment = comment;
				}

				if (updateClient.IsChanged(c => c.Tariff)) {
					_client.Disabled = updateClient.Tariff == null;
					if (_client.IsChanged(c => c.Disabled)) {
						var message = !_client.Disabled ? "Клиент включен оператором, тариф назначен" : "Клиент отключен оператором, тариф удален";
						Appeals.CreareAppeal(message, _client, AppealType.Statistic);
					}
				}
				_client.Name = updateClient.ShortName;
				_client.Update();
				updateClient.Update();

				RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID);
			}
			else {
				updateClient.SetValidationErrors(Validator.GetErrorSummary(updateClient));
				PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(updateClient);
				DbSession.Evict(updateClient);
				RenderView("LawyerPersonInfo");
				PropertyBag["LegalPerson"] = updateClient;
				PropertyBag["grouped"] = grouped;
				var fil = new ClientFilter {
					ClientCode = ClientID,
					grouped = grouped,
					appealType = appealType,
					Editing = true,
					EditingConnect = 0
				};
				LawyerPersonInfo(fil);
			}
		}


		[AccessibleThrough(Verb.Post)]
		public void EditInformation(uint ClientID, uint status,
			string group, uint house_id, AppealType appealType, string comment,
			[DataBind("filter")] ClientFilter filter)
		{
			var client = DbSession.Load<Client>(ClientID);
			var statusEntity = Status.Find(status);
			var updateClient = client.PhysicalClient;
			var oldStatus = client.Status;

			var iptv = client.Iptv;
			var internet = client.Internet;

			SetARDataBinder();
			BindObjectInstance(iptv, "iptv", AutoLoadBehavior.NullIfInvalidKey);
			BindObjectInstance(internet, "internet", AutoLoadBehavior.NullIfInvalidKey);
			BindObjectInstance(updateClient, ParamStore.Form, "Client", AutoLoadBehavior.NullIfInvalidKey);
			BindObjectInstance(client, ParamStore.Form, "_client");

			if (oldStatus.ManualSet)
				client.Status = statusEntity;

			if (Validator.IsValid(updateClient)) {
				var house = DbSession.Get<House>(house_id);
				if (house != null) {
					updateClient.UpdateHouse(house);
				}

				if (!string.IsNullOrEmpty(comment)) {
					client.LogComment = comment;
					updateClient.LogComment = comment;
				}

				client.Name = string.Format("{0} {1} {2}", updateClient.Surname, updateClient.Name, updateClient.Patronymic);
				updateClient.UpdatePackageId();

				if (client.IsChanged(s => s.Status)) {
					if (client.Status.Type == StatusType.NoWorked) {
						client.AutoUnblocked = false;
						client.Disabled = true;
						client.StartNoBlock = null;
						client.Sale = 0;
						if (client.IsChanged(c => c.Disabled))
							Appeals.CreareAppeal("Оператором клиент был заблокирован", client, AppealType.Statistic);
					}
					else if (client.Status.Type != StatusType.Dissolved) {
						client.AutoUnblocked = true;
						client.Disabled = false;
						client.ShowBalanceWarningPage = false;
						if (client.IsChanged(c => c.Disabled))
							Appeals.CreareAppeal("Оператором клиент был разблокирован", client, AppealType.Statistic);
						if (client.IsChanged(c => c.ShowBalanceWarningPage))
							Appeals.CreareAppeal("Оператором отключена страница Warning", client, AppealType.Statistic);
					}
					if (client.Status.Type == StatusType.Dissolved) {
						client.Endpoints.Clear();
						client.PhysicalClient.HouseObj = null;
						client.Disabled = true;
						client.AutoUnblocked = false;
					}
				}

				DbSession.SaveOrUpdate(updateClient);
				DbSession.SaveOrUpdate(client);
				Flash["EditFlag"] = "Данные изменены";
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
			var client = Client.Find(filter.ClientCode);

			LoadBalanceData(grouped, client);
			PropertyBag["iptv"] = client.Iptv;
			PropertyBag["internet"] = client.Internet;
			PropertyBag["grouped"] = grouped;
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["services"] = Service.FindAll();
			PropertyBag["Appeals"] = Appeals.GetAllAppeal(DbSession, client, appealType);
			PropertyBag["Client"] = client.PhysicalClient;
			PropertyBag["EditAddress"] = client.AdditionalStatus == null ? false : client.AdditionalStatus.ShortName == "Refused";

			PropertyBag["Regions"] = DbSession.Query<RegionHouse>().ToList();
			PropertyBag["ChHouse"] = client.PhysicalClient.HouseObj ?? new House();
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["channels"] = ChannelGroup.All(DbSession);
			PropertyBag["ChStatus"] = client.Status != null ? client.Status.Id : Status.FindFirst().Id;
			PropertyBag["naznach_text"] = ConnectGraph.Queryable.Count(c => c.Client.Id == filter.ClientCode) != 0
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
			PropertyBag["SendSmsNotifocation"] = client.SendSmsNotifocation;
			PropertyBag["isService"] = false;
			PropertyBag["RegionList"] = DbSession.Query<RegionHouse>().ToList();
			ConnectPropertyBag(filter.ClientCode);
			SendConnectInfo(client);
			SendUserWriteOff();
		}

		private void LoadBalanceData(string grouped, Client client)
		{
			var payments =
				Payment.Queryable.Where(p => p.Client.Id == client.Id).Where(p => p.Sum > 0).OrderBy(t => t.PaidOn).ToList();
			var writeoffSum = WriteOff.Queryable.Where(p => p.Client.Id == client.Id).ToList().Sum(s => s.WriteOffSum);
			var userWriteoffSum = Models.UserWriteOff.Queryable.Where(w => w.Client.Id == client.Id).ToList().Sum(w => w.Sum);
			if (InitializeContent.Partner.IsDiller())
				payments = payments.Where(p => p.Agent != null && p.Agent.Partner == InitializeContent.Partner).OrderByDescending(t => t.PaidOn).Take(5).OrderBy(t => t.PaidOn).ToList();
			PropertyBag["Payments"] = payments;
			PropertyBag["paymentsSum"] = payments.Sum(p => p.Sum);
			PropertyBag["writeOffSum"] = writeoffSum + userWriteoffSum;

			PropertyBag["WriteOffs"] = client.GetWriteOffs(DbSession, grouped).OrderByDescending(w => w.WriteOffDate).ToList();
		}

		public void SendConnectInfo(Client client)
		{
			if (client.LawyerPerson != null) {
				var orderInfo = client.GetOrderInfo(DbSession);
				if (orderInfo.Count == 0) {
					var connectSum = client.IsPhysical() ? client.PhysicalClient.ConnectSum : 0;
					orderInfo.Add(new ClientOrderInfo {
						Order = new Orders() { Number = Orders.GetNextNumber(DbSession) },
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
			PropertyBag["_client"] = client;
			PropertyBag["ClientCode"] = clientId;
			PropertyBag["Switches"] = NetworkSwitch.All(DbSession);
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			if (client.WhoConnected != null)
				PropertyBag["ChBrigad"] = client.WhoConnected.Id;
			else {
				var brigad = Brigad.FindFirst();
				if (brigad != null)
					PropertyBag["ChBrigad"] = Brigad.FindFirst().Id;
			}
			List<PackageSpeed> speeds;
			var tariffs = Tariff.FindAll().Select(t => t.PackageId).ToList();
			if (client.GetClientType() == ClientType.Phisical) {
				speeds = PackageSpeed.Queryable.Where(p => tariffs.Contains(p.PackageId)).ToList();
			}
			else {
				speeds = PackageSpeed.Queryable.Where(p => !tariffs.Contains(p.PackageId)).OrderBy(s => s.Speed).ToList();
			}
			var clientEndPointId = Convert.ToUInt32(PropertyBag["EConnect"]);

			int? packageId;
			if (clientEndPointId > 0)
				packageId = ClientEndpoint.Queryable.Where(c => c.Id == clientEndPointId).Select(c => c.PackageId).FirstOrDefault();
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
			if (InitializeContent.Partner.IsDiller())
				virtualPayment = false;

			var clientToch = Client.Find(clientId);
			decimal tryBalance;
			if (decimal.TryParse(balanceText, out tryBalance) && tryBalance > 0) {
				if (clientToch.LawyerPerson == null) {
					new Payment {
						Client = clientToch,
						Agent = Agent.GetByInitPartner(),
						PaidOn = DateTime.Now,
						RecievedOn = DateTime.Now,
						Sum = tryBalance,
						BillingAccount = false,
						Virtual = virtualPayment,
						Comment = CommentText
					}.Save();
					Flash["Message"] = Message.Notify("Платеж ожидает обработки");
					Flash["sleepButton"] = true;
				}
				else {
					Flash["Message"] = Message.Error("Юридические лица не могут оплачивать наличностью");
				}
			}
			else {
				Flash["Message"] = Message.Error("Введена неверная сумма, должно быть положительное число.");
			}
			RedirectToUrl(clientToch.Redirect());
		}

		[AccessibleThrough(Verb.Post)]
		public void UserWriteOff(uint ClientID)
		{
			var client = Client.Find(ClientID);
			var writeOff = new UserWriteOff(client);
			BindObjectInstance(writeOff, "userWO");
			if (!HasValidationError(writeOff)) {
				writeOff.Save();
				Flash["Message"] = Message.Notify("Списание ожидает обработки");
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
			var client = Client.Find(ClientID);
			client.AdditionalStatus = AdditionalStatus.Find((uint)AdditionalStatusType.Refused);
			client.Endpoints.Clear();
			client.PhysicalClient.HouseObj = null;
			client.Update();
			foreach (var graph in ConnectGraph.Queryable.Where(c => c.Client == client)) {
				graph.Delete();
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
			var client = Client.Find(ClientID);
			client.AdditionalStatus = AdditionalStatus.Find((uint)AdditionalStatusType.NotPhoned);
			DateTime _noPhoneDate;
			if (DateTime.TryParse(NoPhoneDate, out _noPhoneDate)) {
				new Appeals {
					Appeal =
						"Причина недозвона:  " + prichina + " \r\n Дата: " + _noPhoneDate.ToShortDateString() +
							" \r\n Комментарий: \r\n " + Appeal,
					Date = DateTime.Now,
					Partner = InitializeContent.Partner,
					Client = Client.Find(ClientID),
					AppealType = AppealType.User
				}.SaveAndFlush();
			}
			RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID);
		}

		public void AppointedToTheGraph(uint ClientCode)
		{
			PropertyBag["ClientCode"] = ClientCode;
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["StartDate"] = DateTime.Now.AddDays(1).ToShortDateString();
			LayoutName = "NoMap";
		}

		[return: JSONReturnBinder]
		public object GetGraph()
		{
			var selDate = DateTime.Parse(Request.Form["graph_date"]);
			return new {
				brigads = Brigad.FindAll().Select(b => new { b.Id, b.Name }).ToArray(),
				graphs =
					ConnectGraph.Queryable.Where(c => c.Day.Date == selDate).Select(
						g => new { brigadId = g.Brigad.Id, clientId = g.Client != null ? g.Client.Id : 0, g.IntervalId }).ToArray(),
				intervals = Intervals.GetIntervals()
			};
		}

		[return: JSONReturnBinder]
		public bool SaveGraph()
		{
			if (Request.Form["graph_button"] != null) {
				var client = Client.Find(Convert.ToUInt32(Request.Form["clientId"]));
				var but_id = Request.Form["graph_button"].Split('_');
				if (client.BeginWork == null)
					foreach (var graph in ConnectGraph.Queryable.Where(c => c.Client == client).ToList()) {
						graph.Delete();
					}
				var briad = Brigad.Find(Convert.ToUInt32(but_id[1]));
				var interval = Convert.ToUInt32(but_id[0]);
				new ConnectGraph {
					IntervalId = interval,
					Brigad = briad,
					Client = client,
					Day = DateTime.Parse(Request.Form["graph_date"]),
				}.Save();
				client.AdditionalStatus = AdditionalStatus.Find((uint)AdditionalStatusType.AppointedToTheGraph);
				client.Update();
				new Appeals {
					Client = client,
					Date = DateTime.Now,
					Partner = InitializeContent.Partner,
					Appeal =
						string.Format("Назначен в график, \r\n Брагада: {0} \r\n Дата: {1} \r\n Время: {2}",
							briad.Name,
							DateTime.Parse(Request.Form["graph_date"]).ToShortDateString(),
							Intervals.GetIntervals()[(int)interval]),
					AppealType = AppealType.User
				}.Save();
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
			var briad = Brigad.Find(Convert.ToUInt32(but_id[1]));
			var interval = Convert.ToUInt32(but_id[0]);
			new ConnectGraph {
				Brigad = briad,
				IntervalId = interval,
				Day = DateTime.Parse(Request.Form["graph_date"])
			}.Save();
			return "Время зарезервировано";
		}

		[return: JSONReturnBinder]
		public bool DeleteGraph()
		{
			var date = DateTime.Parse(Request.Form["date"]);
			var client = Client.Find(Convert.ToUInt32(Request.Form["clientId"]));
			var briad = Brigad.Find(Convert.ToUInt32(Request.Form["brigad"]));
			var interval = uint.Parse(Request.Form["interval"]);
			var graph = DbSession.QueryOver<ConnectGraph>().Where(c => c.Client == client && c.Day == date && c.IntervalId == interval && c.Brigad == briad).List().FirstOrDefault();
			if (graph != null) {
				DbSession.Delete(graph);
				var appeal = Appeals.CreareAppeal(string.Format("Удалено назначение в граффик, \r\n Брагада: {0} \r\n Дата: {1} \r\n Время: {2}",
					briad.Name,
					date.ToShortDateString(),
					Intervals.GetIntervals()[(int)interval]), client, AppealType.User);
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
			PropertyBag["selectDate"] = selectDate != DateTime.MinValue ? selectDate : DateTime.Now;
			PropertyBag["Brigad"] = brig != 0 ? Brigad.Find(brig) : Brigad.FindFirst();
			PropertyBag["Brigads"] = Brigad.FindAll();
			PropertyBag["Intervals"] = Intervals.GetIntervals();
		}

		public void CreateAndPrintGraph(uint Brig, DateTime selectDate)
		{
			PropertyBag["Clients"] =
				ConnectGraph.Queryable.Where(c => c.Brigad.Id == Brig && c.Day.Date == selectDate.Date).Select(
					s => s.Client).Where(c => c != null).ToList();
			PropertyBag["selectDate"] = selectDate;
			PropertyBag["Brigad"] = Brigad.Find(Brig);
			PropertyBag["Intervals"] = Intervals.GetIntervals();
		}

		public void ShowAppeals([DataBind("filter")] AppealFilter filter)
		{
			PropertyBag["appeals"] = filter.Find();
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
				Order = new Orders() { Number = Orders.GetNextNumber(DbSession) },
				ClientConnectInfo = new ClientConnectInfo()
			};
			ConnectPropertyBag(clientId);
		}

		[AccessibleThrough(Verb.Post)]
		public void DeleteEndPoint(uint endPointForDelete)
		{
			var endPoint = ClientEndpoint.FirstOrDefault(endPointForDelete);

			if (endPoint != null) {
				var client = endPoint.Client;
				var endPointsForClient = ClientEndpoint.Queryable.Where(c => c.Client == client).ToList();
				if (endPointsForClient.Count > 1 || client.LawyerPerson != null)
					endPoint.Delete();
				else
					Flash["Message"] = Message.Error("Последняя точка подключения не может быть удалена!");
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
			DbSession.SaveOrUpdate(client);
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
			var order = DbSession.Load<Orders>(orderId);
			PropertyBag["OrderService"] = new OrderService();
			PropertyBag["orderId"] = orderId;
			PropertyBag["PageDescription"] = "Добавление новой услуги";
			RenderView("EditOrderService");
		}

		public void SaveOrderService(uint orderId, [DataBind("OrderService")] OrderService orderService)
		{
			if(orderService.Order == null) {
				orderService.Order = DbSession.Load<Orders>(orderId);
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

		public void CloseOrder(uint orderId)
		{
			var order = DbSession.Load<Orders>(orderId);
			order.Disabled = true;
			DbSession.Save(order);
			RedirectToUrl(order.Client.Redirect());
		}

		public void OrdersArchive(uint clientCode)
		{
			var client = DbSession.Load<Client>(clientCode);
			var ordersInfo = client.GetOrderInfo(DbSession, true);
			PropertyBag["OrdersInfo"] = ordersInfo;
		}
	}
}