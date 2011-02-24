using System;
using System.Linq;
using System.Collections.Generic;
using Castle.MonoRail.Framework;
using InternetInterface.AllLogic;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate.Criterion;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class UserInfoController : SmartDispatcherController
	{
		public void SearchUserInfo(uint clientCode, bool Editing, bool EditingConnect)
		{
			var phisCl = PhisicalClients.Find(clientCode);
			PropertyBag["Client"] = phisCl;

			SendParam(clientCode);
			Flash["Editing"] = Editing;
			if (phisCl.Connected)
			Flash["EditingConnect"] = EditingConnect;
			else
			{
				Flash["EditingConnect"] = true;
			}
			PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(new PhisicalClients());
			PropertyBag["ConnectInfo"] = phisCl.GetConnectInfo();
			PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(t => t.Name != null);
		}

		public void SaveSwitchForClient(uint ClientID, [DataBind("ConnectInfo")]PhisicalClientConnectInfo ConnectInfo,
			uint BrigadForConnect)
		{
			var phisCl = PhisicalClients.Find(ClientID);
			var brigadChangeFlag = true;
			if (phisCl.HasConnected != null)
				brigadChangeFlag = false;
			var clientEntPoint = new ClientEndpoints();
			var clients = Clients.FindAllByProperty("PhisicalClient", phisCl);
			if (clients.Length != 0)
			{
				var clientsEndPoint = ClientEndpoints.FindAllByProperty("Client", clients[0]);
				if (clientsEndPoint.Length != 0)
				{
					clientEntPoint = clientsEndPoint[0];
				}
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
			var errorMessage = string.Empty;
			if ((ConnectInfo.static_IP != string.Empty) || (nullFlag))
			{
				try
				{
					var Port = Convert.ToInt32(ConnectInfo.Port);
					if ((Port > 48) && (Port < 1))
					{
						errorMessage += "Неправильно введен порт, введите число от 1 до 48";
					}
					if (Point.isUnique(NetworkSwitches.Find(Convert.ToUInt32(ConnectInfo.Switch)), Port) ||
					((NetworkSwitches.Find(Convert.ToUInt32(ConnectInfo.Switch)) == oldSwitch) && (Port == olpPort)))
					{
						if (errorMessage == string.Empty)
						{
							var newFlag = false;
							if (clients.Length == 0)
							{
								var client = new Clients
								             	{
								             		Name = string.Format("{0} {1} {2}", phisCl.Surname, phisCl.Name, phisCl.Patronymic),
								             		PhisicalClient = phisCl,
								             		Type = ClientType.Phisical
								             	};
								client.SaveAndFlush();
								clientEntPoint.Client = client;
								newFlag = true;
							}
							clientEntPoint.Ip = ConnectInfo.static_IP;
							clientEntPoint.Port = Port;
							clientEntPoint.Switch = NetworkSwitches.Find(Convert.ToUInt32(ConnectInfo.Switch));
							clientEntPoint.Monitoring = ConnectInfo.Monitoring;
							if (!newFlag)
								clientEntPoint.UpdateAndFlush();
							else
								clientEntPoint.SaveAndFlush();
							if (brigadChangeFlag)
							phisCl.HasConnected = Brigad.Find(BrigadForConnect);
							phisCl.Connected = true;
							phisCl.ConnectedDate = DateTime.Now;
							phisCl.UpdateAndFlush();
							PropertyBag["Editing"] = false;
							Flash["EditFlag"] = "Данные изменены";
							RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID);
							return;
						}
					}
					else
					{
						errorMessage = "Такая пара порт/свич уже существует";
					}
				}
				catch (Exception)
				{
					throw;
					errorMessage += "Неправильно введен порт, введите число от 1 до 48";
				}
			}
			else
			{
				errorMessage += "Ошибка ввода IP адреса";
			}
			PropertyBag["ConnectInfo"] = phisCl.GetConnectInfo();
			PropertyBag["Editing"] = true;
			PropertyBag["ChBrigad"] = BrigadForConnect;
			Flash["errorMessage"] = errorMessage;

			RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID + "&EditingConnect=true");

		}

		public void PassAndShowCard(uint ClientID)
		{
			if (CategorieAccessSet.AccesPartner("SSI"))
			{
				var client = PhisicalClients.Find(ClientID);
				var Password = CryptoPass.GeneratePassword();
				client.Password = CryptoPass.GetHashString(Password);
				client.UpdateAndFlush();
				var connectSumm = PaymentForConnect.FindAllByProperty("ClientId", client).First();
				Flash["Password"] = Password;
				Flash["Client"] = client;
				Flash["ConnectSumm"] = connectSumm;
				RedirectToUrl("..//UserInfo/ClientRegisteredInfo.rails");
			}
		}

		public void LoadEditConnectMudule(uint ClientID)
		{
			Flash["EditingConnect"] = true;
			var phisCl = PhisicalClients.Find(ClientID);
			PropertyBag["ConnectInfo"] = phisCl.GetConnectInfo();
			RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID + "&EditingConnect=true");
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
			if (labelForEdit != null)
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
			if (labelForDel != null)
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
			PropertyBag["Clients"] = Requests.FindAll().OrderByDescending(f => f.ActionDate).ToArray();
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
			PropertyBag["Clients"] = Requests.FindAll(DetachedCriteria.For(typeof(Requests))
				.Add(Expression.Eq("Label.Id", labelId))).OrderByDescending(f => f.ActionDate).ToArray();
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
			foreach (var label in labelList)
			{
				var request = Requests.Find(label);
				request.Label = Label.Find(labelch);
				request.ActionDate = DateTime.Now;
				request.Operator = InithializeContent.partner;
				request.UpdateAndFlush();
			}
			PropertyBag["Clients"] = Requests.FindAll().OrderByDescending(f => f.ActionDate).ToArray();;
			SendRequestEditParameter();
		}

		public void InforoomUsersPreview()
		{
		}

		[AccessibleThrough(Verb.Post)]
		public void LoadEditMudule(uint ClientID)
		{
			Flash["Editing"] = true;
			RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID + "&Editing=true");
		}

		public void ClientRegisteredInfo()
		{}

		public void ClientRegisteredInfoFromDiller()
		{ }

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
		public void EditInformation([DataBind("Client")]PhisicalClients client, uint ClientID, uint tariff, uint status)
		{
			var updateClient = PhisicalClients.Find(ClientID);
			BindObjectInstance(updateClient, ParamStore.Form, "Client");
			updateClient.Tariff = Tariff.Find(tariff);
			var statusCanChanged = true;
			if ((updateClient.Status.Id == (uint)StatusType.BlockedAndNoConnected) && (status == (uint)StatusType.NoWorked))
				statusCanChanged = false;
			updateClient.Status = Status.Find(status);
			if (Validator.IsValid(updateClient) && statusCanChanged)
			{
				updateClient.UpdateAndFlush();
				var clients = Clients.FindAllByProperty("PhisicalClient", updateClient);
				if (clients.Length != 0)
				{
					if (updateClient.Status.Blocked)
					{

						foreach (var clientse in clients)
						{
							clientse.Disabled = true;
							clientse.UpdateAndFlush();
						}
					}
					else
					{
						foreach (var clientse in clients)
						{
							clientse.Disabled = false;
							clientse.UpdateAndFlush();
						}
					}
				}
				PropertyBag["Editing"] = false;
				Flash["EditFlag"] = "Данные изменены";
				RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID);
			}
			else
			{
				updateClient.SetValidationErrors(Validator.GetErrorSummary(updateClient));
				PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(updateClient);
				ARSesssionHelper<PhisicalClients>.QueryWithSession(session =>
				{
					session.Evict(updateClient);
					return new List<PhisicalClients>();
				});
				if (!statusCanChanged)
					Flash["statusCanChanged"] = "Если установлет статус Зарегистрирован, но нет информации о подключении, нельзя поставить статус НеРаботает";
				RenderView("SearchUserInfo");
				Flash["Editing"] = true;
				Flash["EditingConnect"] = false;
				Flash["Client"] = updateClient;
				Flash["ChTariff"] = Tariff.Find(tariff).Id;
				Flash["ChStatus"] = Tariff.Find(status).Id;
				SendParam(ClientID);
			}
		}

		private void SendParam(UInt32 ClientCode)
		{
			var phisCl = PhisicalClients.Find(ClientCode);
			PropertyBag["ConnectInfo"] = phisCl.GetConnectInfo();
			PropertyBag["ClientCode"] = ClientCode;
			PropertyBag["UserInfo"] = true;
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["ChTariff"] = phisCl.Tariff.Id;
			if (phisCl.Status != null)
				PropertyBag["ChStatus"] = phisCl.Status.Id;
			else
				PropertyBag["ChStatus"] = Status.FindFirst().Id;
			if (phisCl.HasConnected != null)
				PropertyBag["ChBrigad"] = phisCl.HasConnected.Id;
			else
				PropertyBag["ChBrigad"] = Brigad.FindFirst().Id;
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["ChangeBy"] = new ChangeBalaceProperties {ChangeType = TypeChangeBalance.OtherSumm};
			PropertyBag["PartnerAccessSet"] = new CategorieAccessSet();
			PropertyBag["Payments"] = Payment.FindAllByProperty("Client", phisCl);
		}

		[AccessibleThrough(Verb.Post)]
		public void ChangeBalance([DataBind("ChangedBy")]ChangeBalaceProperties changeProperties, uint clientId, string balanceText)
		{
			var clientToch = PhisicalClients.Find(clientId);
			string forChangeSumm = string.Empty;
			var thisPay = new Payment();
			PropertyBag["ChangeBalance"] = true;
			if (changeProperties.IsForTariff())
			{
				forChangeSumm = PhisicalClients.Find(clientId).Tariff.Price.ToString();
			}
			if (changeProperties.IsOtherSumm())
			{
				forChangeSumm = balanceText;
			}
			thisPay.Sum = forChangeSumm;
			thisPay.Agent = Agent.FindAll(DetachedCriteria.For(typeof(Agent)).Add(Expression.Eq("Partner", InithializeContent.partner)))[0];
			thisPay.Client = PhisicalClients.Find(clientId);
			thisPay.RecievedOn = DateTime.Now;
			thisPay.PaidOn = DateTime.Now;
			if (Validator.IsValid(thisPay))
			{
				thisPay.SaveAndFlush();
				Flash["thisPay"] = new Payment();
				Flash["Applying"] = "Баланс пополнен";
				clientToch.Balance = Convert.ToString(Convert.ToDecimal(clientToch.Balance) + Convert.ToDecimal(forChangeSumm));
				clientToch.UpdateAndFlush();
			}
			else
			{
				thisPay.SetValidationErrors(Validator.GetErrorSummary(thisPay));
				Flash["thisPay"] = thisPay;
				ARSesssionHelper<Payment>.QueryWithSession(session => { session.Evict(thisPay);
				                                                      	return new List<Payment>();
				});
			}
			RedirectToUrl(@"../UserInfo/SearchUserInfo.rails?ClientCode=" + clientId);
		}
	}
}