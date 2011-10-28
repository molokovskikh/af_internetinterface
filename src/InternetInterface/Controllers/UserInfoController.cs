using System;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Linq;
using System.Collections.Generic;
using Castle.ActiveRecord.Framework;
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
using NHibernate;
using NHibernate.Criterion;

namespace InternetInterface.Controllers
{
	public class ClientFilter
	{
		public uint ClientCode { get; set; }
		public int appealType  { get; set; }
		public string grouped  { get; set; }
		public bool Editing  { get; set; }
		//public List<bool> EditingConnect  { get; set; }
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

	//[Layout("Main")]
	[Helper(typeof (PaginatorHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof (AuthenticationFilter))]
	public class UserInfoController : BaseController
	{
		public void SearchUserInfo([DataBind("filter")] ClientFilter filter, [DataBind("userWO")] UserWriteOff writeOff)
		{
			var client = Client.Find(filter.ClientCode);
			PropertyBag["filter"] = filter;
			if (filter.appealType == 0)
				filter.appealType = (int) AppealType.User;
			if (client.Status.Id != (uint)StatusType.BlockedAndNoConnected)
				PropertyBag["EConnect"] = filter.EditingConnect;
			else
			{
				PropertyBag["EConnect"] = 0;
			}
			SendParam(filter.ClientCode, filter.grouped, filter.appealType);
			PropertyBag["Editing"] = filter.Editing;
			PropertyBag["appealType"] = filter.appealType;
			PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClients>(new PhysicalClients());
			PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(t => t.Name != null);
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
			PropertyBag["appealType"] = filter.appealType == 0 ? (int) AppealType.User : filter.appealType;
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["services"] = Service.FindAll();
			PropertyBag["Speeds"] = PackageSpeed.FindAll();

			PropertyBag["Client"] = client.LawyerPerson;
			PropertyBag["UserInfo"] = false;
			PropertyBag["LegalPerson"] = client.LawyerPerson;
			if (PropertyBag["VB"] == null)
				PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(new LawyerPerson());

			PropertyBag["Editing"] = filter.Editing;
			PropertyBag["ConnectInfo"] = client.GetConnectInfo();
			PropertyBag["Payments"] = client.Payments.OrderBy(c => c.PaidOn).ToList();
			PropertyBag["WriteOffs"] = client.GetWriteOffs(filter.grouped).OrderBy(w => w.WriteOffDate);
			PropertyBag["writeOffSum"] = WriteOff.FindAllByProperty("Client", client).Sum(s => s.WriteOffSum);
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["Appeals"] =
				Appeals.Queryable.Where(
					a => a.Client == client && a.AppealType == (filter.appealType == 0 ? (int) AppealType.User : filter.appealType)).
					OrderByDescending(
						a => a.Date).ToArray();

			if (client.Status.Connected)
				PropertyBag["EConnect"] = filter.EditingConnect;
			else
			{
				PropertyBag["EConnect"] = 0;
			}
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
			if (client.CanUsedPostponedPayment())
			{
				client.Disabled = false;
				client.Update();
				message += "Услуга \"Обещанный платеж активирована\"";
				new Appeals {
				            	Appeal = "Услуга \"Обещанный платеж активирована\"",
				            	AppealType = (int) AppealType.System,
				            	Client = client,
				            	Date = DateTime.Now,
				            	Partner = InitializeContent.partner
				            }.Save();
			}
			Flash["Applying"] = message;
			RedirectToUrl("../UserInfo/SearchUserInfo.rails?filter.ClientCode=" + ClientID);
		}

		public void ActivateService(uint clientId, uint serviceId, DateTime? startDate, DateTime? endDate)
		{
			var servise = Service.Find(serviceId);
			var client = Client.Find(clientId);
			var dtn = DateTime.Now;
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
				Activator = InitializeContent.partner
			};
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
			//}
			RedirectToUrl("../UserInfo/SearchUserInfo.rails?filter.ClientCode=" + clientId);
		}

		public void DiactivateService(uint clientId, uint serviceId)
		{
			var servise = Service.Find(serviceId);
			var client = Client.Find(clientId);
			if (client.ClientServices != null)
			{
				var cservice =
					client.ClientServices.Where(c => c.Service.Id == serviceId && c.Activated).FirstOrDefault();
				if (cservice != null)
				{
					cservice.CompulsoryDiactivate();
					Appeals.CreareAppeal(string.Format("Услуга \"{0}\" деактивирована", servise.HumanName), client, AppealType.User);
				}
			}
			RedirectToUrl("../UserInfo/SearchUserInfo.rails?filter.ClientCode=" + clientId);
		}

		[return: JSONReturnBinder]
		public string GetSubnet()
		{
			var mask = Int32.Parse(Request.Form["mask"]);
			return SubnetMask.CreateByNetBitLength(mask).ToString();
		}

		public void SaveSwitchForClient(uint ClientID, [DataBind("ConnectInfo")] ConnectInfo ConnectInfo,
		                                uint BrigadForConnect,
		                                [ARDataBind("staticAdress", AutoLoad = AutoLoadBehavior.NewInstanceIfInvalidKey)] StaticIp[] staticAdress,
			uint EditConnect, string ConnectSum)
		{
			var client = Client.Find(ClientID);
			var brigadChangeFlag = true;
			if (client.WhoConnected != null)
				brigadChangeFlag = false;
			var newFlag = false;
			var clientEntPoint = new ClientEndpoints();
			var clientsEndPoint = ClientEndpoints.Queryable.Where(c => c.Client == client && c.Id == EditConnect).ToArray();
			if (clientsEndPoint.Length != 0) {
				clientEntPoint = clientsEndPoint[0];
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

					clientEntPoint.Client = client;
					clientEntPoint.Ip = ConnectInfo.static_IP;
					clientEntPoint.Port = Int32.Parse(ConnectInfo.Port);
					clientEntPoint.Switch = NetworkSwitches.Find(Convert.ToUInt32(ConnectInfo.Switch));
					clientEntPoint.Monitoring = ConnectInfo.Monitoring;
					if (!newFlag)
						clientEntPoint.UpdateAndFlush();
					else {
						clientEntPoint.SaveAndFlush();
						if (client.PhysicalClient != null && client.PhysicalClient.Request != null &&
						    client.PhysicalClient.Request.Registrator != null) {
							var request = client.PhysicalClient.Request;
							var registrator = request.Registrator;
							PaymentsForAgent.CreatePayment(AgentActions.ConnectClient,
							                               string.Format("Зачисление за подключение клиента #{0}", client.Id), registrator);
							PaymentsForAgent.CreatePayment(registrator,
							                               string.Format("Зачисление бонусов по заявке #{0} за поключенного клиента #{1}",
							                                             request.Id, client.Id), request.VirtualBonus);
							request.PaidBonus = true;
							request.Update();
							if (client.AdditionalStatus != null && client.AdditionalStatus.ShortName == "Refused") {
								PaymentsForAgent.CreatePayment(request.Registrator,
								                               string.Format("Снятие штрафа за отказ клиента заявки #{0}", request.Id),
								                               -AgentTariff.GetPriceForAction(AgentActions.RefusedClient));
								client.AdditionalStatus = null;
								client.Update();
								request.Label = null;
								request.Update();
							}
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
					client.UpdateAndFlush();

					StaticIp.Queryable.Where(s => s.Client == client).ToList().Where(
						s => !staticAdress.Select(f => f.Id).Contains(s.Id)).ToList().
						ForEach(s => s.Delete());

					foreach (var s in staticAdress) {
						if (!string.IsNullOrEmpty(s.Ip))
							if (Regex.IsMatch(s.Ip, NetworkSwitches.IPRegExp)) {
								s.Client = client;
								s.Save();
							}
					}

					var connectSum = 0m;
					if (!string.IsNullOrEmpty(ConnectSum) && decimal.TryParse(ConnectSum, out connectSum) && connectSum > 0) {
						var payments = PaymentForConnect.Queryable.Where(p => p.EndPoint == clientEntPoint).ToList();
						if (payments.Count() == 0)
							new PaymentForConnect {
								Sum = connectSum,
								Partner = InitializeContent.partner,
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
				            	Partner = InitializeContent.partner,
				            	Client = Client.Find(ClientID),
				            	AppealType = (int) AppealType.User
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
				PropertyBag["Password"] = Password;
				PropertyBag["AccountNumber"] = _client.Id.ToString("00000");
				PropertyBag["ConnectInfo"] = _client.GetConnectInfo().FirstOrDefault();
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
			if (labelForDel != null && labelForDel.Deleted)
			{
				labelForDel.DeleteAndFlush();
				ARSesssionHelper<Label>.QueryWithSession(session => {
					var query =
						session.CreateSQLQuery(
							@"update internet.Requests R set r.`Label` = 0, r.`ActionDate` = :ActDate, r.`Operator` = :Oper
																where r.`Label`= :LabelIndex ;")
							.AddEntity(
								typeof (Label));
					query.SetParameter("LabelIndex", deletelabelch);
					query.SetParameter("ActDate", DateTime.Now);
					query.SetParameter("Oper", InitializeContent.partner.Id);
					query.ExecuteUpdate();
					return new List<Label>();
				});
			}
			RedirectToUrl("../UserInfo/RequestView.rails");
		}

		public void RequestView()
		{
			var requests = Requests.FindAll().OrderByDescending(f => f.ActionDate);
			PropertyBag["Clients"] = InitializeContent.partner.Categorie.ReductionName == "Agent"
			                         	? requests.Where(r => r.Registrator == InitializeContent.partner).ToList()
			                         	: requests.ToList();
			SendRequestEditParameter();
		}

		/// <summary>
		/// Создать новую метку
		/// </summary>
		/// <param name="LabelName"></param>
		/// <param name="labelcolor"></param>
		public void CreateLabel(string LabelName, string labelcolor)
		{
			var newlab = new Label {
				Color = labelcolor,
				Name = LabelName,
				Deleted = true
			};
			newlab.SaveAndFlush();
			RedirectToAction("RequestView");
			//RequestView();
		}

		/// <summary>
		/// Фильтр по меткам
		/// </summary>
		/// <param name="labelId"></param>
		public void RequestView(uint labelId)
		{
			var requests = Requests.FindAll().ToList();

			if (labelId == 0)
				requests = requests.Where(r => r.Label == null).ToList();
			else
				requests = requests.Where(r => r.Label != null && r.Label.Id == labelId).ToList();
			requests = requests.OrderByDescending(f => f.ActionDate).ToList();
			if (InitializeContent.partner.Categorie.ReductionName == "Agent")
				PropertyBag["Clients"] = requests.Where(r => r.Registrator == InitializeContent.partner).ToList();
			else
				PropertyBag["Clients"] = requests.ToList();
			SendRequestEditParameter();
		}

		/// <summary>
		/// Устанавливает метки на клиентов
		/// </summary>
		/// <param name="labelList"></param>
		/// <param name="labelch"></param>
		[AccessibleThrough(Verb.Post)]
		public void RequestView([DataBind("LabelList")] List<uint> labelList, uint labelch)
		{
			var _label = Label.Find(labelch);
			foreach (var label in labelList)
			{
				var request = Requests.Find(label);
				if ((request.Label == null) ||
				    (request.Label.ShortComment != "Refused" && request.Label.ShortComment != "Registered"))
				{
					request.Label = _label;
					request.ActionDate = DateTime.Now;
					request.Operator = InitializeContent.partner;
					request.UpdateAndFlush();
				}
				if (_label.ShortComment == "Refused" && request.Registrator != null &&
				    (request.Label == null || request.Label.ShortComment != "Registered"))
				{
					if (!DateHelper.IncludeDateInCurrentInterval(request.RegDate))
						PaymentsForAgent.CreatePayment(AgentActions.DeleteRequest,
						                               string.Format("Списание за отказ заявки #{0}", request.Id), request.Registrator);
					request.SetRequestBoduses();
				}
			}
			var requests = Requests.FindAll().OrderByDescending(f => f.ActionDate);
			if (InitializeContent.partner.Categorie.ReductionName == "Agent")
				PropertyBag["Clients"] = requests.Where(r => r.Registrator == InitializeContent.partner).ToList();
			else
				PropertyBag["Clients"] = requests.ToList();
			SendRequestEditParameter();
		}

		public void InforoomUsersPreview()
		{
		}

		[AccessibleThrough(Verb.Post)]
		public void LoadEditMudule(uint ClientID, int appealType)
		{
			Flash["Editing"] = true;
			RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID + "&filter.Editing=true&filter.appealType=" +
			              appealType);
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
		public void EditLawyerPerson(uint ClientID, int Speed, string grouped, int appealType, string comment)
		{
			var _client = Client.Queryable.First(c => c.Id == ClientID);
			var updateClient = _client.LawyerPerson;
			InitializeHelper.InitializeModel(_client);
			InitializeHelper.InitializeModel(updateClient);
			BindObjectInstance(updateClient, ParamStore.Form, "LegalPerson");

			if (Validator.IsValid(updateClient))
			{
				//updateClient.Speed = PackageSpeed.Find(Speed);

				InitializeHelper.InitializeModel(_client);
				InitializeHelper.InitializeModel(updateClient);
				DbLogHelper.SetupParametersForTriggerLogging();

				if (!string.IsNullOrEmpty(comment))
				{
					_client.LogComment = comment;
					updateClient.LogComment = comment;
				}

				updateClient.Update();

				_client.Name = updateClient.ShortName;
				_client.Update();

				RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID + "&filter.appealType=" + appealType);
			}
			else
			{
				updateClient.SetValidationErrors(Validator.GetErrorSummary(updateClient));
				PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(updateClient);
				ARSesssionHelper<LawyerPerson>.QueryWithSession(session => {
					session.Evict(updateClient);
					return new List<LawyerPerson>();
				});
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
		                            string group, uint house_id, int appealType, string comment,
		                            [DataBind("filter")] ClientFilter filter)
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
				DbLogHelper.SetupParametersForTriggerLogging();

				if (!string.IsNullOrEmpty(comment))
				{
					_client.LogComment = comment;
					updateClient.LogComment = comment;
				}

				updateClient.Update();
				_client.Name = string.Format("{0} {1} {2}", updateClient.Surname, updateClient.Name,
				                             updateClient.Patronymic);
				var endPoints = ClientEndpoints.Queryable.Where(p => p.Client == _client).ToList();
				foreach (var clientEndpointse in endPoints)
				{
					clientEndpointse.PackageId = updateClient.Tariff.PackageId;
				}
				if (_client.Status.Blocked)
				{
					//_client.AutoUnblocked = false;
					_client.Disabled = true;
				}
				else
				{
					_client.AutoUnblocked = true;
					_client.Disabled = false;
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
				ARSesssionHelper<PhysicalClients>.QueryWithSession(session => {
					session.Evict(updateClient);
					return new List<PhysicalClients>();
				});
				RenderView("SearchUserInfo");
				Flash["Editing"] = true;
				//Flash["EditingConnect"] = false;
				Flash["_client"] = _client;
				Flash["Client"] = updateClient;
				filter.ClientCode = _client.Id;
				PropertyBag["filter"] = filter;
				SendParam(ClientID, group, appealType);
			}
		}


		private void SendParam(UInt32 ClientCode, string grouped, int appealType)
		{
			var client = Client.Find(ClientCode);

			PropertyBag["Payments"] = Payment.Queryable.Where(p => p.Client.Id == client.Id).OrderBy(t => t.PaidOn).ToList();
			var abonentSum = WriteOff.Queryable.Where(p => p.Client.Id == client.Id).ToList().Sum(s => s.WriteOffSum);
			PropertyBag["writeOffSum"] = abonentSum +
			                             Models.UserWriteOff.Queryable.Where(w => w.Client.Id == client.Id).ToList().Sum(
			                             	w => w.Sum);
			PropertyBag["WriteOffs"] = client.GetWriteOffs(grouped).OrderBy(w => w.WriteOffDate);
			PropertyBag["grouped"] = grouped;
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["services"] = Service.FindAll();
			PropertyBag["Appeals"] =
				Appeals.Queryable.Where(a => a.Client.Id == client.Id && a.AppealType == appealType).ToList().OrderByDescending(
					a => a.Date);
			PropertyBag["Client"] = client.PhysicalClient;

			PropertyBag["Houses"] = House.FindAll();
			PropertyBag["ChHouse"] = client.PhysicalClient.HouseObj != null ? client.PhysicalClient.HouseObj.Id : 0;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["ChTariff"] = client.PhysicalClient.Tariff.Id;
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["ChStatus"] = client.Status != null ? client.Status.Id : Status.FindFirst().Id;
			PropertyBag["naznach_text"] = ConnectGraph.Queryable.Count(c => c.Client.Id == ClientCode) != 0
			                              	? "Переназначить в график"
			                              	: "Назначить в график";

			PropertyBag["ChangeBy"] = new ChangeBalaceProperties { ChangeType = TypeChangeBalance.OtherSumm };
			PropertyBag["UserInfo"] = true;

			ConnectPropertyBag(ClientCode);
			SendConnectInfo(client);
			SendUserWriteOff();
		}

		public void SendConnectInfo(Client client)
		{
			var connectInfo = client.GetConnectInfo();
			if (connectInfo.Count == 0)
				connectInfo.Add(new ClientConnectInfo());
			PropertyBag["ClientConnectInf"] = connectInfo;
		}

		public void ConnectPropertyBag(uint clientId)
		{
			var client = Client.FirstOrDefault(clientId);
			PropertyBag["_client"] = client;
			//PropertyBag["Client"] = client.PhysicalClient;
			PropertyBag["ClientCode"] = clientId;
			PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(t => !string.IsNullOrEmpty(t.Name));
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["ChBrigad"] = client.WhoConnected != null ? client.WhoConnected.Id : Brigad.FindFirst().Id;
			PropertyBag["staticIps"] = StaticIp.Queryable.Where(s => s.Client.Id == clientId).ToList();
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
		public void ChangeBalance([DataBind("ChangedBy")] ChangeBalaceProperties changeProperties, uint clientId,
		                          string balanceText)
		{
			var clientToch = Client.Find(clientId);
			var forChangeSumm = string.Empty;
			PropertyBag["ChangeBalance"] = true;
			if (changeProperties.IsForTariff())
			{
				forChangeSumm = Client.Find(clientId).PhysicalClient.Tariff.Price.ToString();
			}
			if (changeProperties.IsOtherSumm())
			{
				forChangeSumm = balanceText;
			}
			decimal tryBalance;
			if (decimal.TryParse(forChangeSumm, out tryBalance) && tryBalance > 0)
			{
				new Payment {
				            	Client = clientToch,
								Agent = Agent.GetByInitPartner(),
								PaidOn = DateTime.Now,
								RecievedOn = DateTime.Now,
								Sum = tryBalance,
								BillingAccount = false
				            }.Save();
				Flash["Message"] = Message.Notify("Платеж ожидает обработки");
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
			client.Update();
			foreach (var graph in ConnectGraph.Queryable.Where(c => c.Client == client))
			{
				graph.Delete();
			}
			if (client.PhysicalClient.Request != null && client.PhysicalClient.Request.Registrator != null)
				PaymentsForAgent.CreatePayment(AgentActions.RefusedClient,
				                               string.Format("Списание за отказ клиента #{0} от подключения", client.Id),
				                               client.PhysicalClient.Request.Registrator);
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
				            	Partner = InitializeContent.partner,
				            	Client = Client.Find(ClientID),
				            	AppealType = (int) AppealType.User
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
			           			g => new {brigadId = g.Brigad.Id, clientId = g.Client != null ? g.Client.Id : 0, g.IntervalId}).ToArray
			           			(),
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
				            	Partner = InitializeContent.partner,
				            	Appeal =
				            		string.Format("Назначен в график, \r\n Брагада: {0} \r\n Дата: {1} \r\n Время: {2}",
				            		              briad.Name,
				            		              DateTime.Parse(Request.Form["graph_date"]).ToShortDateString(),
				            		              Intervals.GetIntervals()[(int) interval]),
				            	AppealType = (int) AppealType.User
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
					s => s.Client);
			PropertyBag["selectDate"] = selectDate;
			PropertyBag["Brigad"] = Brigad.Find(Brig);
			PropertyBag["Intervals"] = Intervals.GetIntervals();
		}

		private string CreateAppealLink(string link, string name, int type)
		{
			return string.Format("<a href=\"{0}\"" +
			                     "<button type=\"button\" id=\"Button1\" class=\"button\">" +
			                     "<img alt=\"Save\" src=\"../Images/tick.png\">{1}</button></a>",
			                     link.Remove(link.IndexOf("appalType")) + "appealType" + type, name);
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