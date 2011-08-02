using System;
using System.Threading;
using System.Web;
using System.Linq;
using System.Collections.Generic;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Helpers;
using InternetInterface.AllLogic;
using InternetInterface.Controllers.Filter;
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
        public bool EditingConnect  { get; set; }
    }

    public class AppealFilter : IPaginable
    {
        public uint ClientCode { get; set; }
        public DateTime? beginDate { get; set; }
        public DateTime? endDate { get; set; }

        private int _lastRowsCount;

        public int RowsCount
        {
            get { return _lastRowsCount; }
        }

        public int PageSize { get { return 20; } }

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
            _lastRowsCount =
                Internetsessionslog.Queryable.Where(
                    i =>
                    i.EndpointId.Client.Id == ClientCode && i.LeaseBegin.Date >= beginDate.Value.Date &&
                    i.LeaseEnd.Date <= endDate.Value.Date).Count();
            if (_lastRowsCount > 0)
            {
                var getCount = _lastRowsCount - PageSize * CurrentPage < PageSize ? _lastRowsCount - PageSize * CurrentPage : PageSize;
                return
                    Internetsessionslog.Queryable.Where(
                        i =>
                        i.EndpointId.Client.Id == ClientCode && i.LeaseBegin.Date >= beginDate.Value.Date &&
                        i.LeaseEnd.Date <= endDate.Value.Date).ToList().GetRange(
                            PageSize * CurrentPage, getCount);
            }
            return new List<Internetsessionslog>();
        }
    }

    [Layout("Main")]
    [Helper(typeof(PaginatorHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
    public class UserInfoController : SmartDispatcherController
	{
        public void SearchUserInfo([DataBind("filter")]ClientFilter filter)
		{
            var client = Client.Find(filter.ClientCode);
            PropertyBag["filter"] = filter;
			PropertyBag["_client"] = client;
            PropertyBag["services"] = Service.FindAll();
			PropertyBag["Client"] = client.PhysicalClient;
            //PropertyBag["Leases"] = filter.Find();
            
            if (filter.appealType == 0)
                filter.appealType = (int) AppealType.User;
            SendParam(filter.ClientCode, filter.grouped, filter.appealType);
			PropertyBag["Editing"] = filter.Editing;
            PropertyBag["appealType"] = filter.appealType;//CreateAppealLink(Request.Url, "Отобразить", appealType);
			if (client.Status.Id != (uint)StatusType.BlockedAndNoConnected)
			PropertyBag["EditingConnect"] = filter.EditingConnect;
			else
			{
				PropertyBag["EditingConnect"] = true;
			}
			PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClients>(new PhysicalClients());
			PropertyBag["ConnectInfo"] = client.GetConnectInfo();
			PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(t => t.Name != null);
		}

        public void Leases([DataBind("filter")]AppealFilter filter)
        {
            PropertyBag["_client"] = Client.Find(filter.ClientCode);
            PropertyBag["filter"] = filter;
            PropertyBag["Leases"] = filter.Find();
        }

        public void LawyerPersonInfo([DataBind("filter")]ClientFilter filter)
		{
			var client = Client.Find(filter.ClientCode);


			if (client.Status != null)
				PropertyBag["ChStatus"] = client.Status.Id;
			else
				PropertyBag["ChStatus"] = Status.FindFirst().Id;
			if (client.WhoConnected != null)
				PropertyBag["ChBrigad"] = client.WhoConnected.Id;
			else
				PropertyBag["ChBrigad"] = Brigad.FindFirst().Id;
            PropertyBag["ClientCode"] = filter.ClientCode;
            PropertyBag["grouped"] = filter.grouped;
            PropertyBag["appealType"] = filter.appealType == 0 ? (int)AppealType.User : filter.appealType;
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(t => t.Name != null);
			PropertyBag["Brigads"] = Brigad.FindAllSort();

			PropertyBag["Client"] = client.LawyerPerson;

			PropertyBag["LegalPerson"] = client.LawyerPerson;
			PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(new LawyerPerson());

			PropertyBag["_client"] = client;
			PropertyBag["Editing"] = filter.Editing;
			PropertyBag["ConnectInfo"] = client.GetConnectInfo();
			PropertyBag["Payments"] = client.Payments.OrderBy(c => c.PaidOn).ToArray();
            PropertyBag["WriteOffs"] = client.GetWriteOffs(filter.grouped).OrderBy(w => w.WriteOffDate);
            PropertyBag["writeOffSum"] = WriteOff.FindAllByProperty("Client", client).Sum(s => s.WriteOffSum);
			PropertyBag["BalanceText"] = string.Empty;
            PropertyBag["Appeals"] = Appeals.Queryable.Where(a => a.Client == client && a.AppealType == (filter.appealType == 0 ? (int)AppealType.User : filter.appealType)).OrderByDescending(
		            a => a.Date).ToArray();
			if (client.Status.Connected)
				PropertyBag["EditingConnect"] = filter.EditingConnect;
			else
			{
				PropertyBag["EditingConnect"] = true;
			}
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
            if (client.CanUsedPostponedPayment())
            {
                //client.PostponedPayment = DateTime.Now;
                client.Disabled = false;
                client.Update();
                message += "Услуга \"Обещанный платеж активирована\"";
                new Appeals
                {
                    Appeal = "Услуга \"Обещанный платеж активирована\"",
                    AppealType = (int)AppealType.System,
                    Client = client,
                    Date = DateTime.Now,
                    Partner = InithializeContent.partner
                }.Save();
            }
            Flash["Applying"] = message;
            RedirectToUrl("../UserInfo/SearchUserInfo.rails?filter.ClientCode=" + ClientID);
        }

        public void ActivateService(uint clientId, uint serviceId, DateTime? startDate, DateTime? endDate)
        {
            var servise = Service.Find(serviceId);
            var client = Client.Find(clientId);
            if (ClientService.Queryable.Count(c => c.Service == servise && c.Client == client) == 0)
            {
                var clientService = new ClientService {
                                                          Client = client,
                                                          Service = servise,
                                                          BeginWorkDate =
                                                              startDate == null
                                                                  ? startDate
                                                                  : new DateTime(startDate.Value.Year,
                                                                                 startDate.Value.Month,
                                                                                 startDate.Value.Day, DateTime.Now.Hour,
                                                                                 DateTime.Now.Minute,
                                                                                 DateTime.Now.Second),
                                                          EndWorkDate = endDate == null
                                                                            ? endDate
                                                                            : new DateTime(endDate.Value.Year,
                                                                                           endDate.Value.Month,
                                                                                           endDate.Value.Day,
                                                                                           DateTime.Now.Hour,
                                                                                           DateTime.Now.Minute,
                                                                                           DateTime.Now.Second),
                                                      };
                clientService.Save();
                clientService.Activate();
                Appeals.CreareAppeal(
                    string.Format("Услуга \"{0}\" активирована на период с {1} по {2}", servise.HumanName,
                                  clientService.BeginWorkDate != null ? clientService.BeginWorkDate.Value.ToShortDateString() : DateTime.Now.ToShortDateString(),
                                  clientService.EndWorkDate != null ? clientService.EndWorkDate.Value.ToShortDateString() : string.Empty),
                    client,
                    AppealType.User);
            }
            RedirectToUrl("../UserInfo/SearchUserInfo.rails?filter.ClientCode=" + clientId);
        }

        public void SaveSwitchForClient(uint ClientID, [DataBind("ConnectInfo")]ConnectInfo ConnectInfo,
			uint BrigadForConnect)
		{
			var client = Client.Find(ClientID);
			var brigadChangeFlag = true;
			if (client.WhoConnected != null)
				brigadChangeFlag = false;
			var newFlag = false;
			var clientEntPoint = new ClientEndpoints();
			var clientsEndPoint = ClientEndpoints.Queryable.Where(c => c.Client == client).ToArray();
			if (clientsEndPoint.Length != 0)
			{
				clientEntPoint = clientsEndPoint[0];
			}
			else
			{
				newFlag = true;
			}
			var olpPort = clientEntPoint.Port;
			var oldSwitch = clientEntPoint.Switch;
			var nullFlag = false;
			if (ConnectInfo.static_IP == null)
			{
				clientEntPoint.Ip = null;
				nullFlag = true;
			}
			else
			{
				ConnectInfo.static_IP = NetworkSwitches.SetProgramIp(ConnectInfo.static_IP);
			}
			var errorMessage = Validation.ValidationConnectInfo(ConnectInfo);
			if ((ConnectInfo.static_IP != string.Empty) || (nullFlag))
			{
				if (errorMessage == string.Empty || (oldSwitch != null && ConnectInfo.Switch == oldSwitch.Id && ConnectInfo.Port == olpPort.ToString()))
				{
					if (client.GetClientType() == ClientType.Phisical)
						clientEntPoint.PackageId = client.PhysicalClient.Tariff.PackageId;
					else
						clientEntPoint.PackageId = client.LawyerPerson.Speed.PackageId;
					clientEntPoint.Client = client;
					clientEntPoint.Ip = ConnectInfo.static_IP;
					clientEntPoint.Port = Int32.Parse(ConnectInfo.Port);
					clientEntPoint.Switch = NetworkSwitches.Find(Convert.ToUInt32(ConnectInfo.Switch));
					clientEntPoint.Monitoring = ConnectInfo.Monitoring;
                    if (!newFlag)
                        clientEntPoint.UpdateAndFlush();
                    else
                    {
                        clientEntPoint.SaveAndFlush();
                        if (client.PhysicalClient != null && client.PhysicalClient.Request != null && client.PhysicalClient.Request.Registrator != null)
                            PaymentsForAgent.CreatePayment(AgentActions.ConnectClient, "Зачисление за подключение клиента", client.PhysicalClient.Request.Registrator);
                    }
				    if (brigadChangeFlag)
					{   
						var brigad = Brigad.Find(BrigadForConnect);
						client.WhoConnected = brigad;
						client.WhoConnectedName = brigad.Name;
					}
					client.ConnectedDate = DateTime.Now;
					client.Status = Status.Find((uint)StatusType.BlockedAndConnected);
					client.UpdateAndFlush();

					RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID);
					return;
				}
			}
			else
			{
				errorMessage = string.Empty;
				errorMessage += "Ошибка ввода IP адреса";
			}
			PropertyBag["ConnectInfo"] = client.GetConnectInfo();
			PropertyBag["Editing"] = true;
			PropertyBag["ChBrigad"] = BrigadForConnect;
			Flash["errorMessage"] = errorMessage;
			RedirectToReferrer();
		}

		public void CreateAppeal(string Appeal, uint ClientID)
		{
            if (!string.IsNullOrEmpty(Appeal))
			new Appeals
				{
					Appeal = Appeal,
					Date = DateTime.Now,
					Partner = InithializeContent.partner,
					Client = Client.Find(ClientID),
                    AppealType = (int)AppealType.User
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
				//var connectSumm = PaymentForConnect.FindAllByProperty("ClientId", client).First();
                PropertyBag["WhoConnected"] = _client.WhoConnected;
				PropertyBag["Client"] = client;
				PropertyBag["Password"] = Password;
				PropertyBag["AccountNumber"] = _client.Id.ToString("00000");
				//PropertyBag["ConnectSumm"] = connectSumm;
				RenderView("ClientRegisteredInfo");
			}
		}

		public void LoadEditConnectMudule(uint ClientID)
		{
			Flash["EditingConnect"] = true;
			var Cl = Client.Find(ClientID);
			PropertyBag["ConnectInfo"] = Cl.GetConnectInfo();
			RedirectToUrl("../Search/Redirect.rails?filter.ClientCode=" + ClientID + "&filter.EditingConnect=true");
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
				ARSesssionHelper<Label>.QueryWithSession(session =>
				                                         	{
				                                         		var query =
				                                         			session.CreateSQLQuery(
																		@"update internet.Requests R set r.`Label` = 0, r.`ActionDate` = :ActDate, r.`Operator` = :Oper
																where r.`Label`= :LabelIndex ;")
				                                         				.AddEntity(
				                                         					typeof (Label));
				                                         		query.SetParameter("LabelIndex", deletelabelch);
																query.SetParameter("ActDate", DateTime.Now);
																query.SetParameter("Oper", InithializeContent.partner.Id);
				                                         		query.ExecuteUpdate();
				                                         		return new List<Label>();
				                                         	});
			}
			RedirectToUrl("../UserInfo/RequestView.rails");
		}

		public void RequestView()
		{
		    var requests = Requests.FindAll().OrderByDescending(f => f.ActionDate);
            if (InithializeContent.partner.Categorie.ReductionName == "Agent")
                PropertyBag["Clients"] = requests.Where(r => r.Registrator == InithializeContent.partner).ToList();
            else
                PropertyBag["Clients"] = requests.ToList();
			SendRequestEditParameter();
		}

		/// <summary>
		/// Создать новую метку
		/// </summary>
		/// <param name="LabelName"></param>
		/// <param name="labelcolor"></param>
		public void RequestView(string LabelName, string labelcolor)
		{
			var newlab = new Label
			             	{
								Color = labelcolor,
								Name = LabelName
			             	};
			newlab.SaveAndFlush();
			RequestView();
		}

		/// <summary>
		/// Фильтр по меткам
		/// </summary>
		/// <param name="labelId"></param>
		public void RequestView(uint labelId)
		{
		    var requests = Requests.FindAll().ToList();
		                                        /*.Add(Expression.Eq("Label.Id", labelId)))*/

            if (labelId == 0)
                requests = requests.Where(r => r.Label == null).ToList();
            else
                requests = requests.Where(r => r.Label != null && r.Label.Id == labelId).ToList();
            requests = requests.OrderByDescending(f => f.ActionDate).ToList();
            if (InithializeContent.partner.Categorie.ReductionName == "Agent")
                PropertyBag["Clients"] = requests.Where(r => r.Registrator == InithializeContent.partner).ToList();
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
		public void RequestView([DataBind("LabelList")]List<uint> labelList, uint labelch)
		{
		    var _label = Label.Find(labelch);
			foreach (var label in labelList)
			{
                var request = Requests.Find(label);
                if ((request.Label == null) || (request.Label.ShortComment != "Refused" && request.Label.ShortComment != "Registered"))
                {
                    request.Label = _label;
                    request.ActionDate = DateTime.Now;
                    request.Operator = InithializeContent.partner;
                    request.UpdateAndFlush();
                }
			    if (_label.ShortComment == "Refused" && request.Registrator != null)
                    PaymentsForAgent.CreatePayment(AgentActions.DeleteRequest, "Списание за отказ заявки", request.Registrator);
			}
		    var requests = Requests.FindAll().OrderByDescending(f => f.ActionDate);
            if (InithializeContent.partner.Categorie.ReductionName == "Agent")
                PropertyBag["Clients"] = requests.Where(r => r.Registrator == InithializeContent.partner).ToList();
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
            RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID + "&filter.Editing=true&filter.appealType=" + appealType);
		}

		public void ClientRegisteredInfo()
		{
		}

		public void ClientRegisteredInfoFromDiller()
		{}

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
				updateClient.Speed = PackageSpeed.Find(Speed);

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

				var clientEndPoint = ClientEndpoints.Queryable.FirstOrDefault(c => c.Client == _client);
                if (clientEndPoint != null)
                {
                    clientEndPoint.PackageId = updateClient.Speed.PackageId;
                    clientEndPoint.Update();
                }
                RedirectToUrl("../Search/Redirect?filter.ClientCode=" + ClientID + "&filter.appealType=" + appealType);
			}
			else
			{
				updateClient.SetValidationErrors(Validator.GetErrorSummary(updateClient));
				PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(updateClient);
				ARSesssionHelper<LawyerPerson>.QueryWithSession(session =>
				{
					session.Evict(updateClient);
                    return new List<LawyerPerson>();
				});
				PropertyBag["Editing"] = true;
				PropertyBag["EditingConnect"] = false;
				PropertyBag["LegalPerson"] = updateClient;
			    PropertyBag["grouped"] = grouped;
				RedirectToReferrer();
			}
		}


        [AccessibleThrough(Verb.Post)]
        public void EditInformation([DataBind("Client")]PhysicalClients client, uint ClientID, uint tariff, uint status, string group, uint house_id, int appealType, string comment, [DataBind("filter")]ClientFilter filter)
        {
            var _client = Client.Queryable.First(c => c.Id == ClientID);
            var _status = Status.Find(status);
            var updateClient = _client.PhysicalClient;
            var oldStatus = _client.Status;
            InitializeHelper.InitializeModel(updateClient);
            InitializeHelper.InitializeModel(_client);

            BindObjectInstance(updateClient, ParamStore.Form, "Client");

            var statusCanChanged = true;
            if ((_client.Status.Type == StatusType.BlockedAndNoConnected) && ((_status.Type == StatusType.NoWorked) || _status.Type == StatusType.VoluntaryBlocking))
                statusCanChanged = false;
            /*if (_status.Type == StatusType.VoluntaryBlocking && _client.VoluntaryUnblockedDate != null && (DateTime.Now - _client.VoluntaryUnblockedDate.Value).Days < 45)
                statusCanChanged = false;*/
            _client.Status = _status;
            if (Validator.IsValid(updateClient) && statusCanChanged)
            {
                if (updateClient.PassportDate != null)
                    updateClient.PassportDate = updateClient.PassportDate;
                var _house = House.Find(house_id);
                updateClient.HouseObj = _house;
                updateClient.Street = _house.Street;
                updateClient.House = _house.Number.ToString();
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
                /*if (oldStatus.Type != StatusType.VoluntaryBlocking && _client.Status.Type == StatusType.VoluntaryBlocking)
                {
                    _client.RatedPeriodDate = DateTime.Now;
                    _client.DebtDays = 0;
                    _client.VoluntaryBlockingDate = DateTime.Now;
                    _client.VoluntaryUnblockedDate = null;
                }
                if (oldStatus.Type == StatusType.VoluntaryBlocking && _client.Status.Type != StatusType.VoluntaryBlocking)
                {
                    _client.VoluntaryBlockingDate = null;
                    _client.DebtDays = 0;
                    _client.VoluntaryUnblockedDate = DateTime.Now;
                    _client.RatedPeriodDate = DateTime.Now;
                }*/
                if (_client.Status.Blocked)
                {
                    _client.AutoUnblocked = false;
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
                if (!statusCanChanged)
                    Flash["statusCanChanged"] =
                        "Если установлет статус Зарегистрирован, но нет информации о подключении, нельзя поставить статус НеРаботает";
                RenderView("SearchUserInfo");
                Flash["Editing"] = true;
                Flash["EditingConnect"] = false;
                Flash["_client"] = _client;
                Flash["Client"] = updateClient;
                Flash["ChTariff"] = Tariff.Find(tariff).Id;
                Flash["ChStatus"] = Tariff.Find(status).Id;
                filter.ClientCode = _client.Id;
                //PropertyBag["Leases"] = filter.Find();
                PropertyBag["filter"] = filter;
                SendParam(ClientID, group, appealType);
            }
        }


        private void SendParam(UInt32 ClientCode, string grouped, int appealType)
		{
			var client = Client.Find(ClientCode);
		    PropertyBag["Houses"] = House.FindAll();
            PropertyBag["ChHouse"] = client.PhysicalClient.HouseObj != null ? client.PhysicalClient.HouseObj.Id : 0;
			PropertyBag["ConnectInfo"] = client.GetConnectInfo();
		    PropertyBag["grouped"] = grouped;
		    PropertyBag["Appeals"] =
		        Appeals.FindAllByProperty("Client", client).Where(a => a.AppealType == appealType).OrderByDescending(
		            a => a.Date);
			PropertyBag["ClientCode"] = ClientCode;
			PropertyBag["UserInfo"] = true;
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["ChTariff"] = client.PhysicalClient.Tariff.Id;
			if (client.Status != null)
				PropertyBag["ChStatus"] = client.Status.Id;
			else
				PropertyBag["ChStatus"] = Status.FindFirst().Id;
			if (client.WhoConnected != null)
				PropertyBag["ChBrigad"] = client.WhoConnected.Id;
			else
				PropertyBag["ChBrigad"] = Brigad.FindFirst().Id;
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["ChangeBy"] = new ChangeBalaceProperties {ChangeType = TypeChangeBalance.OtherSumm};
			PropertyBag["PartnerAccessSet"] = new CategorieAccessSet();
			PropertyBag["Payments"] = Payment.FindAllByProperty("Client", client).OrderBy(t => t.PaidOn).ToArray();
            PropertyBag["WriteOffs"] = client.GetWriteOffs(grouped).OrderBy(w => w.WriteOffDate);//WriteOff.FindAllByProperty("Client", client).OrderBy(t => t.WriteOffDate);
		    PropertyBag["naznach_text"] = ConnectGraph.Queryable.Count(c => c.Client.Id == ClientCode) != 0
		                                      ? "Переназначить в график"
		                                      : "Назначить в график";
		    PropertyBag["writeOffSum"] = WriteOff.FindAllByProperty("Client", client).Sum(s => s.WriteOffSum);
		}

		[AccessibleThrough(Verb.Post)]
		public void ChangeBalance([DataBind("ChangedBy")]ChangeBalaceProperties changeProperties, uint clientId, string balanceText)
		{
			var clientToch = Client.Find(clientId);
			string forChangeSumm = string.Empty;
			var thisPay = new Payment();
			PropertyBag["ChangeBalance"] = true;
			if (changeProperties.IsForTariff())
			{
				forChangeSumm = Client.Find(clientId).PhysicalClient.Tariff.Price.ToString();
			}
			if (changeProperties.IsOtherSumm())
			{
				forChangeSumm = balanceText;
			}
			thisPay.Sum = Convert.ToDecimal(forChangeSumm);
			thisPay.Agent = Agent.FindAll(DetachedCriteria.For(typeof(Agent)).Add(Expression.Eq("Partner", InithializeContent.partner)))[0];
			thisPay.Client = clientToch;
			thisPay.RecievedOn = DateTime.Now;
			thisPay.PaidOn = DateTime.Now;
			thisPay.BillingAccount = false;
			if (Validator.IsValid(thisPay))
			{
				thisPay.SaveAndFlush();
				Flash["thisPay"] = new Payment();
				Flash["Applying"] = "Баланс пополнен";
				/*if (clientToch.GetClientType() == ClientType.Phisical)
				{
					var physicalClient = clientToch.PhysicalClient;
					physicalClient.Balance = physicalClient.Balance + Convert.ToDecimal(forChangeSumm);
					physicalClient.UpdateAndFlush();
				}
				else
				{
					var lawyerPerson = clientToch.LawyerPerson;
					lawyerPerson.Balance += Convert.ToDecimal(forChangeSumm);
					lawyerPerson.UpdateAndFlush();
				}*/
			}
			else
			{
				thisPay.SetValidationErrors(Validator.GetErrorSummary(thisPay));
				Flash["thisPay"] = thisPay;
				ARSesssionHelper<Payment>.QueryWithSession(session =>
				{
					session.Evict(thisPay);
					return new List<Payment>();
				});
			}
			RedirectToUrl(@"../Search/Redirect?filter.ClientCode=" + clientId);
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
                PaymentsForAgent.CreatePayment(AgentActions.RefusedClient, "Списание за отказ клиента от подключения", client.PhysicalClient.Request.Registrator);
			CreateAppeal("Причина отказа:  " + prichina  + " \r\n Комментарий: \r\n " + Appeal, ClientID);
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
			if (DateTime.TryParse(NoPhoneDate, out _noPhoneDate))
			{
				new Appeals
					{
                        Appeal = "Причина недозвона:  " + prichina + " \r\n Дата: " + _noPhoneDate.ToShortDateString() + " \r\n Комментарий: \r\n " + Appeal,
						Date = DateTime.Now,
						Partner = InithializeContent.partner,
						Client = Client.Find(ClientID),
                        AppealType = (int)AppealType.User
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
			return new
			             	{
			             		brigads = Brigad.FindAll().Select(b => new {b.Id, b.Name}).ToArray(),
			             		graphs =
			             			ConnectGraph.Queryable.Where(c => c.Day.Date == selDate).Select(
			             				g => new {brigadId =  g.Brigad.Id, clientId = g.Client != null ? g.Client.Id : 0, g.IntervalId}).ToArray(),
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
                                Partner = InithializeContent.partner,
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

        [return : JSONReturnBinder]
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
				    "<img alt=\"Save\" src=\"../Images/tick.png\">{1}</button></a>", link.Remove(link.IndexOf("appalType")) + "appealType" + type, name);
        }
	}
}