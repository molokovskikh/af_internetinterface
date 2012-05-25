using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using InternetInterface.AllLogic;
using InternetInterface.Controllers.Filter;
using InternetInterface.Filters;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Services;

namespace InternetInterface.Controllers
{
	public class ClientFilter
	{
		public uint ClientCode { get; set; }
		public AppealType appealType  { get; set; }
		public string grouped  { get; set; }
		public bool Editing  { get; set; }
		public bool EditConnectInfoFlag  { get; set; }
		public int EditingConnect { get; set; }
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

		public List<Internetsessionslog> Find()
		{
			var thisD = DateTime.Now;
			if (beginDate == null)
				beginDate = new DateTime(thisD.Year, thisD.Month, 1);
			if (endDate == null)
				endDate = DateTime.Now;
			Expression<Func<Internetsessionslog, bool>> predicate = i =>
				i.EndpointId.Client.Id == ClientCode && i.LeaseBegin.Date >= beginDate.Value.Date
				&& (i.LeaseEnd.Value.Date <= endDate.Value.Date
					|| i.LeaseEnd == null);

			_lastRowsCount = Internetsessionslog.Queryable.Where(predicate).Count();
			if (_lastRowsCount > 0)
			{
				var getCount = _lastRowsCount - PageSize*CurrentPage < PageSize ? _lastRowsCount - PageSize*CurrentPage : PageSize;
				return Internetsessionslog.Queryable.Where(predicate)
					.Skip(PageSize * CurrentPage)
					.Take(getCount)
					.ToList();
			}
			return new List<Internetsessionslog>();
		}
	}


	public class RequestFilter : IPaginable, ISortableContributor, SortableContributor
	{
		public string query { get; set; }
		public DateTime? beginDate { get; set; }
		public DateTime? endDate { get; set; }
		public string SortBy { get; set; }
		public string Direction { get; set; }
		public uint? labelId { get; set; }
		public bool Archive { get; set; }

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
			return
				GetParameters().Where(p => p.Key != "CurrentPage").Select(p => string.Format("{0}={1}", p.Key, p.Value))
					.ToArray();
		}

		public Dictionary<string,object> GetParameters()
		{
			return new Dictionary<string, object> {
													{"filter.query", query},
													{"filter.beginDate", beginDate},
													{"filter.endDate", endDate},
													{"filter.labelId", labelId},
													{"filter.Archive", Archive},
													{"filter.SortBy", SortBy},
													{"filter.Direction", Direction},
													{"CurrentPage", CurrentPage}
												  };
		}

		public string ToUrlQuery()
		{
			return string.Join("&", ToUrl());
		}

		public string GetUriToArchive()
		{
			Archive = !Archive;
			var label = Archive ? "Показать архив" : "Показать заявки";
			var a = string.Format("<a href=\"../UserInfo/RequestView?{0}\">{1}</a>", GetUri(), label);
			Archive = ! Archive;
			return a;
		}

		public string GetUri()
		{
			return ToUrlQuery();
		}

		public List<Request> Find()
		{
			var thisD = DateTime.Now;
			if (beginDate == null)
				beginDate = new DateTime(thisD.Year, thisD.Month, 1);
			if (endDate == null)
				endDate = DateTime.Now;

			Expression<Func<Request, bool>> predicate;
			if (labelId != 0)
				if (!string.IsNullOrEmpty(query))
					predicate = i => (i.Street.Contains(query) || i.ApplicantPhoneNumber.Contains(query) || i.ApplicantName.Contains(query)) && i.ActionDate.Date >= beginDate.Value.Date && i.ActionDate.Date <= endDate.Value.Date && i.Label.Id == labelId && i.Archive == Archive;
				else {
					predicate = i => i.ActionDate.Date >= beginDate.Value.Date && i.ActionDate.Date <= endDate.Value.Date && i.Label.Id == labelId && i.Archive == Archive;
				}
			else {
				if (!string.IsNullOrEmpty(query))
					predicate =
						i =>
						(i.Street.Contains(query) || i.ApplicantPhoneNumber.Contains(query) || i.ApplicantName.Contains(query)) && i.ActionDate.Date >= beginDate.Value.Date &&
						i.ActionDate.Date <= endDate.Value.Date &&
						i.Archive == Archive;
				else {
					predicate =
						i => i.ActionDate.Date >= beginDate.Value.Date && i.ActionDate.Date <= endDate.Value.Date && i.Archive == Archive;
				}
			}

			_lastRowsCount = Request.Queryable.Where(predicate).Count();
			if (_lastRowsCount > 0) {
				var getCount = _lastRowsCount - PageSize*CurrentPage < PageSize ? _lastRowsCount - PageSize*CurrentPage : PageSize;
				var result = Request.Queryable.Where(predicate).ToList();
				if (!string.IsNullOrEmpty(SortBy))
					result.Sort(new PropertyComparer<Request>(Direction == "asc" ? SortDirection.Asc : SortDirection.Desc, SortBy));
				return result.Skip(PageSize*CurrentPage)
					.Take(getCount)
					.ToList();
			}
			return new List<Request>();
		}
	}

	[Helper(typeof (PaginatorHelper))]
	[Helper(typeof (TextHelper))]
	[Helper(typeof (BindingHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof (AuthenticationFilter))]
	public class UserInfoController : BaseController
	{
		public void SearchUserInfo([DataBind("filter")] ClientFilter filter, [DataBind("userWO")] UserWriteOff writeOff)
		{
			var client = Client.Find(filter.ClientCode);
			PropertyBag["filter"] = filter;
			SendParam(filter, filter.grouped, filter.appealType);
			PropertyBag["Editing"] = filter.Editing;
			PropertyBag["appealType"] = filter.appealType;
			PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClients>(new PhysicalClients());
			PropertyBag["Switches"] = NetworkSwitches.All(DbSession);
		}

		public void Leases([DataBind("filter")] SessionFilter filter)
		{
			PropertyBag["_client"] = Client.Find(filter.ClientCode);
			PropertyBag["filter"] = filter;
			PropertyBag["Leases"] = filter.Find();	
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
			PropertyBag["ConnectInfo"] = client.GetConnectInfo(DbSession);
			PropertyBag["Payments"] = client.Payments.Where(p => p.Sum > 0).OrderBy(c => c.PaidOn).ToList();
			PropertyBag["WriteOffs"] = client.GetWriteOffs(DbSession, filter.grouped).OrderByDescending(w => w.WriteOffDate).ToList();
			PropertyBag["writeOffSum"] = WriteOff.FindAllByProperty("Client", client).Sum(s => s.WriteOffSum);
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["Appeals"] = Appeals.GetAllAppeal(DbSession, client, filter.appealType);

			if (client.Status.Connected)
				PropertyBag["EConnect"] = filter.EditingConnect;
			else
				PropertyBag["EConnect"] = 0;
			if (client.LawyerPerson.Tariff == null)
				PropertyBag["Message"] = Message.Error("Не задана абонентская плата для клиента ! Клиент отключен !");

			PropertyBag["CallLogs"] = UnresolvedCall.LastCalls;
			PropertyBag["Contacts"] =
				Contact.Queryable.Where(c => c.Client.Id == filter.ClientCode).OrderByDescending(c => c.Type).ToList();
			PropertyBag["EditConnectInfoFlag"] = filter.EditConnectInfoFlag;
			SendConnectInfo(client);
			ConnectPropertyBag(filter.ClientCode);
			SendUserWriteOff();
		}

		public void PostponedPayment(uint ClientID)
		{
			var client = Client.Find(ClientID);
			var pclient = client.PhysicalClient;
			var message = string.Empty;
			if (client.ClientServices.Select(c => c.Service).Contains(Service.GetByType(typeof (DebtWork))))
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
					AppealType = AppealType.System,
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
				new Contact {
					Client = client,
					Text = number,
					Type = ContactType.ConnectedPhone,
					Registrator = InitializeContent.Partner,
					Date = DateTime.Now
				}.Save();
				phone.Delete();
				new Appeals {
					Client = client,
					Date = DateTime.Now,
					AppealType = AppealType.System,
					Partner = InitializeContent.Partner,
					Appeal = string.Format("Номер {0} был привязян к данному клиенту", number)
				}.Save();
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
				contact.Save();
			}
			RedirectToUrl("../Search/Redirect.rails?filter.ClientCode=" + ClientID);
		}

		public void DeleteContact(uint contactId)
		{
			Contact.Find(contactId).Delete();
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
			clientService.Save();
			client.ClientServices.Add(clientService);
			clientService.Activate();
			if (string.IsNullOrEmpty(clientService.LogComment))
				Appeals.CreareAppeal(
					string.Format("Услуга \"{0}\" активирована на период с {1} по {2}", servise.HumanName,
					              clientService.BeginWorkDate != null
					              	? clientService.BeginWorkDate.Value.ToShortDateString()
					              	: DateTime.Now.ToShortDateString(),
					              clientService.EndWorkDate != null
					              	? clientService.EndWorkDate.Value.ToShortDateString()
					              	: string.Empty),
					client,
					AppealType.User);
			else
				PropertyBag["errorMessage"] = clientService.LogComment;
			}
			RedirectToUrl(client.Redirect());
		}

		public void DiactivateService(uint clientId, uint serviceId)
		{
			var servise = Service.Find(serviceId);
			var client = Client.Find(clientId);
			if (client.ClientServices != null)
			{
				var cservice =
					client.ClientServices.FirstOrDefault(c => c.Service.Id == serviceId && c.Activated);
				if (cservice != null)
				{
					cservice.CompulsoryDiactivate();
					Appeals.CreareAppeal(string.Format("Услуга \"{0}\" деактивирована", servise.HumanName), client, AppealType.User);
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
				return NetworkSwitches.GetNormalIp(Convert.ToUInt32(lease.Ip));
			return string.Empty;
		}

		public void SaveSwitchForClient(uint ClientID, [DataBind("ConnectInfo")] ConnectInfo ConnectInfo,
		                                uint BrigadForConnect,
		                                [ARDataBind("staticAdress", AutoLoad = AutoLoadBehavior.NewInstanceIfInvalidKey)] StaticIp[] staticAdress,
			uint EditConnect, string ConnectSum)
		{
			var client = Client.Find(ClientID);
			bool brigadChangeFlag = client.WhoConnected == null;
			var newFlag = false;
			var clientEntPoint = new ClientEndpoints();
			var clientsEndPoint = ClientEndpoints.Queryable.Where(c => c.Client == client && c.Id == EditConnect).ToArray();
			if (clientsEndPoint.Length != 0) {
				clientEntPoint = clientsEndPoint[0];
				InitializeHelper.InitializeModel(clientEntPoint);
			}
			else {
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
				ConnectInfo.static_IP = NetworkSwitches.SetProgramIp(ConnectInfo.static_IP);
			}
			var errorMessage = Validation.ValidationConnectInfo(ConnectInfo);
			decimal _connectSum;
			var validateSum =
				!(!string.IsNullOrEmpty(ConnectSum) && (!decimal.TryParse(ConnectSum, out _connectSum) || _connectSum <= 0));
			if (!validateSum)
				errorMessage = "Введена невалидная сумма";
			if ((ConnectInfo.static_IP != string.Empty) || (nullFlag)) {
				if (validateSum && string.IsNullOrEmpty(errorMessage) || validateSum &&
				    (oldSwitch != null && ConnectInfo.Switch == oldSwitch.Id && ConnectInfo.Port == olpPort.ToString())) {

					var packageSpeed =
						PackageSpeed.Queryable.Where(p => p.PackageId == ConnectInfo.PackageId).ToList().FirstOrDefault();
					clientEntPoint.PackageId = packageSpeed.PackageId;
					if (client.GetClientType() == ClientType.Phisical) {
						clientEntPoint.PackageId = client.PhysicalClient.Tariff.PackageId;
					}
					if (string.IsNullOrEmpty(clientEntPoint.Ip) && !string.IsNullOrEmpty(ConnectInfo.static_IP))
						new UserWriteOff {
							Client = client,
							Date = DateTime.Now,
							Sum = 200,
							Comment = string.Format("Плата за фиксированный Ip адрес ({0})", NetworkSwitches.GetNormalIp(UInt32.Parse(ConnectInfo.static_IP))),
							Registrator = InitializeContent.Partner
						}.Save();
					clientEntPoint.Client = client;
					clientEntPoint.Ip = ConnectInfo.static_IP;
					clientEntPoint.Port = Int32.Parse(ConnectInfo.Port);
					clientEntPoint.Switch = DbSession.Load<NetworkSwitches>(ConnectInfo.Switch);
					clientEntPoint.Monitoring = ConnectInfo.Monitoring;
					if (!newFlag) {
						InitializeHelper.InitializeModel(clientEntPoint);
						clientEntPoint.UpdateAndFlush();
					}
					else {
						clientEntPoint.SaveAndFlush();
							if (client.AdditionalStatus != null && client.AdditionalStatus.ShortName == "Refused") {
								client.AdditionalStatus = null;
								client.Update();
							}
					}
					if (brigadChangeFlag) {
						var brigad = Brigad.Find(BrigadForConnect);
						client.WhoConnected = brigad;
						client.WhoConnectedName = brigad.Name;
					}
					client.ConnectedDate = DateTime.Now;
					if (client.Status.Id == (uint) StatusType.BlockedAndNoConnected)
						client.Status = Status.Find((uint) StatusType.BlockedAndConnected);
					client.Update();

					StaticIp.Queryable.Where(s => s.EndPoint == clientEntPoint).ToList().Where(
						s => !staticAdress.Select(f => f.Id).Contains(s.Id)).ToList().
						ForEach(s => s.Delete());

					foreach (var s in staticAdress) {
						if (!string.IsNullOrEmpty(s.Ip))
							if (Regex.IsMatch(s.Ip, NetworkSwitches.IPRegExp)) {
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

					RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID);
					return;
				}
			}
			else {
				errorMessage = string.Empty;
				errorMessage += "Ошибка ввода IP адреса";
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
			if (CategorieAccessSet.AccesPartner("SSI"))
			{
				var _client = Client.Find(ClientID);
				var client = _client.PhysicalClient;
				var Password = CryptoPass.GeneratePassword();
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
			if (labelForEdit != null && labelForEdit.Deleted)
			{
				if (LabelName != null)
					labelForEdit.Name = LabelName;
				if (labelcolor != "#000000")
				{
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
@"update internet.Request R 
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

		public void RequestView([DataBind("filter")]RequestFilter filter)
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
			foreach (var label in labelList)
			{
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
			if (Flash["Partner"] == null)
			{
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
			SetBinder(new DecimalValidateBinder {Validator = Validator});
			var _client = Client.Queryable.First(c => c.Id == ClientID);
			var updateClient = _client.LawyerPerson;
			InitializeHelper.InitializeModel(_client);
			InitializeHelper.InitializeModel(updateClient);
			BindObjectInstance(updateClient, ParamStore.Form, "LegalPerson");

			if (IsValid(updateClient))
			{
				InitializeHelper.InitializeModel(_client);
				InitializeHelper.InitializeModel(updateClient);

				if (!string.IsNullOrEmpty(comment))
				{
					_client.LogComment = comment;
					updateClient.LogComment = comment;
				}

				updateClient.Update();

				_client.Disabled = updateClient.Tariff == null;
				_client.Name = updateClient.ShortName;
				_client.Update();

				RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID + "&filter.appealType=" + appealType);
			}
			else
			{
				updateClient.SetValidationErrors(Validator.GetErrorSummary(updateClient));
				PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(updateClient);
				DbSession.Evict(updateClient);
				RenderView("LawyerPersonInfo");
				PropertyBag["LegalPerson"] = updateClient;
				PropertyBag["grouped"] = grouped;
				var fil = new ClientFilter
				{
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
		public void EditInformation([DataBind("Client")] PhysicalClients client, uint ClientID, uint tariff, uint status,
		                            string group, uint house_id, AppealType appealType, string comment,
		                            [DataBind("filter")] ClientFilter filter, bool SendSmsNotifocation)
		{
			var _client = Client.Queryable.First(c => c.Id == ClientID);
			var _status = Status.Find(status);
			var updateClient = _client.PhysicalClient;
			var oldStatus = _client.Status;
			InitializeHelper.InitializeModel(updateClient);
			InitializeHelper.InitializeModel(_client);

			BindObjectInstance(updateClient, ParamStore.Form, "Client");

			if (oldStatus.ManualSet)
				_client.Status = _status;
			if (Validator.IsValid(updateClient))
			{
				if (updateClient.PassportDate != null)
					updateClient.PassportDate = updateClient.PassportDate;
				var _house = House.Find(house_id);
				updateClient.HouseObj = _house;
				updateClient.Street = _house.Street;
				updateClient.House = _house.Number;
				updateClient.CaseHouse = _house.Case;
				updateClient.Tariff = Tariff.Find(tariff);

				InitializeHelper.InitializeModel(updateClient);
				InitializeHelper.InitializeModel(_client);

				if (!string.IsNullOrEmpty(comment))
				{
					_client.LogComment = comment;
					updateClient.LogComment = comment;
				}

				updateClient.Update();
				_client.SendSmsNotifocation = SendSmsNotifocation;
				_client.Name = string.Format("{0} {1} {2}", updateClient.Surname, updateClient.Name,
				                             updateClient.Patronymic);
				var endPoints = ClientEndpoints.Queryable.Where(p => p.Client == _client).ToList();
				foreach (var clientEndpointse in endPoints)
				{
					clientEndpointse.PackageId = updateClient.Tariff.PackageId;
				}
				if (_client.Status.Blocked)
				{
					_client.Disabled = true;
				}
				else
				{
					_client.AutoUnblocked = true;
					_client.Disabled = false;
					_client.ShowBalanceWarningPage = false;
				}
				if (_client.Status.Type == StatusType.Dissolved) {
					_client.Endpoints.Clear();
					_client.PhysicalClient.HouseObj = null;
				}
				_client.Update();
				PropertyBag["Editing"] = false;

				Flash["EditFlag"] = "Данные изменены";
				RedirectToUrl("../UserInfo/SearchUserInfo?filter.ClientCode=" + ClientID + "&filter.appealType=" +
				              appealType);
			}
			else
			{
				updateClient.SetValidationErrors(Validator.GetErrorSummary(updateClient));
				PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClients>(updateClient);
				DbSession.Evict(updateClient);
				RenderView("SearchUserInfo");
				Flash["Editing"] = true;
				Flash["_client"] = _client;
				Flash["Client"] = updateClient;
				filter.ClientCode = _client.Id;
				PropertyBag["filter"] = filter;
				SendParam(filter, group, appealType);
			}
		}


		private void SendParam(ClientFilter filter, string grouped, AppealType appealType)
		{
			var client = Client.Find(filter.ClientCode);

			PropertyBag["Payments"] = Payment.Queryable.Where(p => p.Client.Id == client.Id).Where(p => p.Sum > 0).OrderBy(t => t.PaidOn).ToList();
			var abonentSum = WriteOff.Queryable.Where(p => p.Client.Id == client.Id).ToList().Sum(s => s.WriteOffSum);
			PropertyBag["writeOffSum"] = abonentSum +
			                             Models.UserWriteOff.Queryable.Where(w => w.Client.Id == client.Id).ToList().Sum(
			                             	w => w.Sum);
			PropertyBag["WriteOffs"] = client.GetWriteOffs(DbSession, grouped).OrderByDescending(w => w.WriteOffDate).ToList();
			PropertyBag["grouped"] = grouped;
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["services"] = Service.FindAll();
			PropertyBag["Appeals"] = Appeals.GetAllAppeal(DbSession, client, appealType);
			PropertyBag["Client"] = client.PhysicalClient;

			PropertyBag["Houses"] = House.AllSort;
			PropertyBag["ChHouse"] = client.PhysicalClient.HouseObj != null ? client.PhysicalClient.HouseObj.Id : 0;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["ChTariff"] = client.PhysicalClient.Tariff.Id;
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["ChStatus"] = client.Status != null ? client.Status.Id : Status.FindFirst().Id;
			PropertyBag["naznach_text"] = ConnectGraph.Queryable.Count(c => c.Client.Id == filter.ClientCode) != 0
			                              	? "Переназначить в график"
			                              	: "Назначить в график";

			PropertyBag["ChangeBy"] = new ChangeBalaceProperties { ChangeType = TypeChangeBalance.OtherSumm };
			PropertyBag["UserInfo"] = true;
			PropertyBag["CallLogs"] = UnresolvedCall.LastCalls;
			PropertyBag["Contacts"] =
				Contact.Queryable.Where(c => c.Client.Id == filter.ClientCode)
					.OrderByDescending(c => c.Type).ToList();

			if (client.Status.Id != (uint)StatusType.BlockedAndNoConnected)
				PropertyBag["EConnect"] = filter.EditingConnect;
			else
			{
				PropertyBag["EConnect"] = 0;
			}
			PropertyBag["EditConnectInfoFlag"] = filter.EditConnectInfoFlag;
			PropertyBag["SendSmsNotifocation"] = client.SendSmsNotifocation;
			PropertyBag["isService"] = false;
			ConnectPropertyBag(filter.ClientCode);
			SendConnectInfo(client);
			SendUserWriteOff();
		}

		public void SendConnectInfo(Client client)
		{
			var connectInfo = client.GetConnectInfo(DbSession);
			if (connectInfo.Count == 0)
				connectInfo.Add(new ClientConnectInfo());
			PropertyBag["ClientConnectInf"] = connectInfo;
		}

		public void ConnectPropertyBag(uint clientId)
		{
			var client = Client.FirstOrDefault(clientId);
			PropertyBag["_client"] = client;
			PropertyBag["ClientCode"] = clientId;
			PropertyBag["Switches"] = NetworkSwitches.All(DbSession);
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
			if (client.GetClientType() == ClientType.Phisical)
			{
				speeds = PackageSpeed.Queryable.Where(p => tariffs.Contains(p.PackageId)).ToList();
			}
			else
			{
				speeds = PackageSpeed.FindAll().OrderBy(s => s.Speed).ToList();
			}
			var clientEndPointId = Convert.ToUInt32(PropertyBag["EConnect"]);
			if (clientEndPointId > 0)
				PropertyBag["ChSpeed"] =
					ClientEndpoints.Queryable.Where(c => c.Id == clientEndPointId).Select(c => c.PackageId).FirstOrDefault();
			else {
				if (client.GetClientType() == ClientType.Phisical) {
					var tariff = client.PhysicalClient.Tariff;
					PropertyBag["ChSpeed"] = tariff.PackageId;
				}
				else {
					PropertyBag["ChSpeed"] = 0;
				}
			}
			PropertyBag["Speeds"] = speeds;
		}

		private void SendUserWriteOff()
		{
			if (Flash["userWO"] != null)
			{
				var userVriteOffs = Flash["userWO"];
				Validator.IsValid(userVriteOffs);
				PropertyBag["userWO"] = userVriteOffs;
			}
			else
				PropertyBag["userWO"] = new UserWriteOff();
		}

		[AccessibleThrough(Verb.Post)]
		public void ChangeBalance(uint clientId, string balanceText, bool virtualPayment)
		{
			var clientToch = Client.Find(clientId);
			decimal tryBalance;
			if (decimal.TryParse(balanceText, out tryBalance) && tryBalance > 0)
			{
				if (clientToch.LawyerPerson == null) {
					new Payment {
						Client = clientToch,
						Agent = Agent.GetByInitPartner(),
						PaidOn = DateTime.Now,
						RecievedOn = DateTime.Now,
						Sum = tryBalance,
						BillingAccount = false,
						Virtual = virtualPayment
					}.Save();
					Flash["Message"] = Message.Notify("Платеж ожидает обработки");
					Flash["sleepButton"] = true;
				}
				else {
					Flash["Message"] = Message.Error("Юридические лица не могут оплачивать наличностью");
				}
			}
			else
			{
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
			if (!HasValidationError(writeOff))
			{
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
			client.AdditionalStatus = AdditionalStatus.Find((uint) AdditionalStatusType.Refused);
			client.Endpoints.Clear();
			client.PhysicalClient.HouseObj = null;
			client.Update();
			foreach (var graph in ConnectGraph.Queryable.Where(c => c.Client == client))
			{
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
			client.AdditionalStatus = AdditionalStatus.Find((uint) AdditionalStatusType.NotPhoned);
			DateTime _noPhoneDate;
			if (DateTime.TryParse(NoPhoneDate, out _noPhoneDate))
			{
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
				brigads = Brigad.FindAll().Select(b => new {b.Id, b.Name}).ToArray(),
				graphs =
					ConnectGraph.Queryable.Where(c => c.Day.Date == selDate).Select(
						g => new {brigadId = g.Brigad.Id, clientId = g.Client != null ? g.Client.Id : 0, g.IntervalId}).ToArray(),
				intervals = Intervals.GetIntervals()
			};
		}

		[return: JSONReturnBinder]
		public bool SaveGraph()
		{
			if (Request.Form["graph_button"] != null)
			{
				var client = Client.Find(Convert.ToUInt32(Request.Form["clientId"]));
				var but_id = Request.Form["graph_button"].Split('_');
				foreach (var graph in ConnectGraph.Queryable.Where(c => c.Client == client).ToList())
				{
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
				client.AdditionalStatus = AdditionalStatus.Find((uint) AdditionalStatusType.AppointedToTheGraph);
				client.Update();
				new Appeals {
				            	Client = client,
				            	Date = DateTime.Now,
				            	Partner = InitializeContent.Partner,
				            	Appeal =
				            		string.Format("Назначен в график, \r\n Брагада: {0} \r\n Дата: {1} \r\n Время: {2}",
				            		              briad.Name,
				            		              DateTime.Parse(Request.Form["graph_date"]).ToShortDateString(),
				            		              Intervals.GetIntervals()[(int) interval]),
				            	AppealType = AppealType.User
				            }.Save();
				return true;
			}
			else
			{
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

		public void ShowAppeals([DataBind("filter")]AppealFilter filter)
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
			PropertyBag["connectInfo"] = new ClientConnectInfo();
			ConnectPropertyBag(clientId);
		}

		[AccessibleThrough(Verb.Post)]
		public void DeleteEndPoint(uint endPointForDelete)
		{
			var endPoint = ClientEndpoints.FirstOrDefault(endPointForDelete);

			if (endPoint != null)
			{
				var client = endPoint.Client;
				var endPointsForClient = ClientEndpoints.Queryable.Where(c => c.Client == client).ToList();
				if (endPointsForClient.Count > 1)
					endPoint.Delete();
				else
					Flash["Message"] = Message.Error("Последняя точка подключения не может быть удалена!");
			}
			RedirectToReferrer();
		}
	}
}