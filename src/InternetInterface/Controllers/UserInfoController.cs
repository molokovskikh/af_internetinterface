using System;
using System.Web;
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
			var client = Clients.Find(clientCode);
			PropertyBag["_client"] = client;
			PropertyBag["Client"] = client.PhysicalClient;

			SendParam(clientCode);
			PropertyBag["Editing"] = Editing;
			if (client.Status.Connected)
			PropertyBag["EditingConnect"] = EditingConnect;
			else
			{
				PropertyBag["EditingConnect"] = true;
			}
			PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClients>(new PhysicalClients());
			PropertyBag["ConnectInfo"] = client.GetConnectInfo();
			PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(t => t.Name != null);
		}

		public void LawyerPersonInfo(uint clientCode, bool Editing, bool EditingConnect)
		{
			var client = Clients.Find(clientCode);


			if (client.Status != null)
				PropertyBag["ChStatus"] = client.Status.Id;
			else
				PropertyBag["ChStatus"] = Status.FindFirst().Id;
			if (client.WhoConnected != null)
				PropertyBag["ChBrigad"] = client.WhoConnected.Id;
			else
				PropertyBag["ChBrigad"] = Brigad.FindFirst().Id;
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(t => t.Name != null);
			PropertyBag["Brigads"] = Brigad.FindAllSort();

			PropertyBag["Client"] = client.LawyerPerson;

			PropertyBag["LegalPerson"] = client.LawyerPerson;
			PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(new LawyerPerson());

			PropertyBag["_client"] = client;
			PropertyBag["Editing"] = Editing;
			PropertyBag["ConnectInfo"] = client.GetConnectInfo();
			PropertyBag["Payments"] = client.Payments.OrderBy(c => c.PaidOn).ToArray();
			PropertyBag["WriteOffs"] = WriteOff.Queryable.Where(w => w.Client == client).OrderBy(w => w.WriteOffDate).ToArray();
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["Appeals"] = Appeals.Queryable.Where(a => a.Client == client).ToArray();
			if (client.Status.Connected)
				PropertyBag["EditingConnect"] = EditingConnect;
			else
			{
				PropertyBag["EditingConnect"] = true;
			}
		}

		public void SaveSwitchForClient(uint ClientID, [DataBind("ConnectInfo")]ConnectInfo ConnectInfo,
			uint BrigadForConnect)
		{
			var client = Clients.Find(ClientID);
			//var phisCl = PhysicalClients.Find(ClientID);
			var brigadChangeFlag = true;
			if (client.WhoConnected != null)
				brigadChangeFlag = false;
			var newFlag = false;
			var clientEntPoint = new ClientEndpoints();
			//var clients = Clients.FindAllByProperty("PhysicalClient", phisCl);
			//if (clients.Length != 0)
			//var clientsEndPoint = ClientEndpoints.FindAllByProperty("Client", clients[0]);
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
				/*try
				{
					var Port = Convert.ToInt32(ConnectInfo.Port);
					if ((Port > 48) && (Port < 1))
					{
						errorMessage += "Неправильно введен порт, введите число от 1 до 48";
					}*/
				/*if (Point.isUnique(ConnectInfo.Switch, Int32.Parse(ConnectInfo.Port)) ||
				(((ConnectInfo.Switch)) == oldSwitch) && (Port == olpPort)))
				{*/
				if (errorMessage == string.Empty || (oldSwitch != null && ConnectInfo.Switch == oldSwitch.Id && ConnectInfo.Port == olpPort.ToString()))
				{
					/*if (clients.Length == 0)
					{
						var client = new Clients
										{
											Name = string.Format("{0} {1} {2}", phisCl.Surname, phisCl.Name, phisCl.Patronymic),
											PhysicalClient = phisCl,
											Type = ClientType.Phisical,
											//FirstLease = true
										};
						client.SaveAndFlush();
						clientEntPoint.Client = client;
						newFlag = true;
					}*/
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
						clientEntPoint.SaveAndFlush();
					if (brigadChangeFlag)
					{
						var brigad = Brigad.Find(BrigadForConnect);
						client.WhoConnected = brigad;
						client.WhoConnectedName = brigad.Name;
					}
					client.ConnectedDate = DateTime.Now;
					client.Status = Status.Find((uint)StatusType.NoWorked);
					client.UpdateAndFlush();
					/*PropertyBag["Editing"] = false;
					PropertyBag["EditFlag"] = "Данные изменены";*/

					RedirectToUrl("../Search/Redirect?ClientCode=" + ClientID);
					return;
				}
				/*}
				else
				{
					errorMessage = "Такая пара порт/свич уже существует";
				}
			}
			catch (Exception)
			{
				throw;
				errorMessage += "Неправильно введен порт, введите число от 1 до 48";
			}*/
			}
			else
			{
				errorMessage = string.Empty;
				errorMessage += "Ошибка ввода IP адреса";
			}
			PropertyBag["ConnectInfo"] = client.GetConnectInfo();
			PropertyBag["Editing"] = true;
			PropertyBag["ChBrigad"] = BrigadForConnect;
			PropertyBag["errorMessage"] = errorMessage;
			RedirectToReferrer();
			//RedirectToUrl("../Search/Redirect?ClientCode=" + ClientID + "&EditingConnect=true");
		}

		public void CreateAppeal(string Appeal, uint ClientID)
		{
			new Appeals
				{
					Appeal = Appeal,
					Date = DateTime.Now,
					Partner = InithializeContent.partner,
					Client = Clients.Find(ClientID)
				}.SaveAndFlush();
			RedirectToUrl("../Search/Redirect?ClientCode=" + ClientID);
		}

		public void PassAndShowCard(uint ClientID)
		{
			if (CategorieAccessSet.AccesPartner("SSI"))
			{
				var client = Clients.Find(ClientID).PhysicalClient;
				var Password = CryptoPass.GeneratePassword();
				client.Password = CryptoPass.GetHashString(Password);
				client.UpdateAndFlush();
				//var connectSumm = PaymentForConnect.FindAllByProperty("ClientId", client).First();
				PropertyBag["Client"] = client;
				PropertyBag["Password"] = Password;
				//PropertyBag["ConnectSumm"] = connectSumm;
				RenderView("ClientRegisteredInfo");
			}
		}

		public void LoadEditConnectMudule(uint ClientID)
		{
			Flash["EditingConnect"] = true;
			var Cl = Clients.Find(ClientID);
			PropertyBag["ConnectInfo"] = Cl.GetConnectInfo();
			RedirectToUrl("../Search/Redirect.rails?ClientCode=" + ClientID + "&EditingConnect=true");
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
			PropertyBag["Clients"] = Requests.FindAll(DetachedCriteria.For(typeof (Requests))
			                                          	.Add(Expression.Eq("Label.Id", labelId)))
														.OrderByDescending(
															f => f.ActionDate).ToArray();
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
			PropertyBag["Clients"] = Requests.FindAll().OrderByDescending(f => f.ActionDate).ToArray();
			SendRequestEditParameter();
		}

		public void InforoomUsersPreview()
		{
		}

		[AccessibleThrough(Verb.Post)]
		public void LoadEditMudule(uint ClientID)
		{
			Flash["Editing"] = true;
			RedirectToUrl("../Search/Redirect?ClientCode=" + ClientID + "&Editing=true");
		}

		//public void ClientRegisteredInfo(PhysicalClients client, string Password, PaymentForConnect connectSumm)
		public void ClientRegisteredInfo()
		{
			/*PropertyBag["Client"] = client;
			PropertyBag["Password"] = Password;
			PropertyBag["ConnectSumm"] = connectSumm;*/
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
		public void EditInformation([DataBind("Client")]PhysicalClients client, uint ClientID, uint tariff, uint status)
		{
			//var updateClient = PhysicalClients.Find(ClientID);
			var _client = Clients.Queryable.First(c => c.Id == ClientID);
			var updateClient = _client.PhysicalClient;
			
			BindObjectInstance(updateClient, ParamStore.Form, "Client");
			var statusCanChanged = true;
			if ((_client.Status.Id == (uint)StatusType.BlockedAndNoConnected) && (status == (uint)StatusType.NoWorked))
				statusCanChanged = false;
			_client.Status = Status.Find(status);
			if (Validator.IsValid(updateClient) && statusCanChanged)
			{
				if (updateClient.PassportDate != null)
				updateClient.PassportDate = DateTime.Parse(updateClient.PassportDate).ToShortDateString();
				updateClient.Tariff = Tariff.Find(tariff);
				updateClient.UpdateAndFlush();
					var endPoints = ClientEndpoints.Queryable.Where(p => p.Client == _client).ToList();
					foreach (var clientEndpointse in endPoints)
					{
						clientEndpointse.PackageId = updateClient.Tariff.PackageId;
					}
					if (_client.Status.Blocked)
					{
						_client.Disabled = true;
						_client.UpdateAndFlush();
					}
					else
					{
						_client.Disabled = false;
						_client.UpdateAndFlush();
					}
				PropertyBag["Editing"] = false;
				Flash["EditFlag"] = "Данные изменены";
				RedirectToUrl("../Search/Redirect?ClientCode=" + ClientID);
			}
			else
			{
				updateClient.SetValidationErrors(Validator.GetErrorSummary(updateClient));
				PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClients>(updateClient);
				ARSesssionHelper<PhysicalClients>.QueryWithSession(session =>
				{
					session.Evict(updateClient);
					return new List<PhysicalClients>();
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
			var client = Clients.Find(ClientCode);
			PropertyBag["ConnectInfo"] = client.GetConnectInfo();
			PropertyBag["Appeals"] = Appeals.FindAllByProperty("Client", client);
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
			PropertyBag["WriteOffs"] = WriteOff.FindAllByProperty("Client", client).OrderBy(t => t.WriteOffDate);
		}

		[AccessibleThrough(Verb.Post)]
		public void ChangeBalance([DataBind("ChangedBy")]ChangeBalaceProperties changeProperties, uint clientId, string balanceText)
		{
			var clientToch = Clients.Find(clientId);
			string forChangeSumm = string.Empty;
			var thisPay = new Payment();
			PropertyBag["ChangeBalance"] = true;
			if (changeProperties.IsForTariff())
			{
				forChangeSumm = PhysicalClients.Find(clientId).Tariff.Price.ToString();
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
			thisPay.BillingAccount = true;
			if (Validator.IsValid(thisPay))
			{
				thisPay.SaveAndFlush();
				Flash["thisPay"] = new Payment();
				Flash["Applying"] = "Баланс пополнен";
				if (clientToch.GetClientType() == ClientType.Phisical)
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
				}
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
			RedirectToUrl(@"../Search/Redirect?ClientCode=" + clientId);
		}
	}
}