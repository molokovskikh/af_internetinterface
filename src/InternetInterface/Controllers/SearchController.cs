using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InternetInterface.AllLogic;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using log4net;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace InternetInterface.Controllers
{
	public class Connect
	{
		private int _clientId;
		private int _switch;
		private string _port;
		public int ClientId
		{
			get { return _clientId; }
			set { _clientId = value; }
		}
		public int SwitchId
		{
			get { return _switch; }
			set { _switch = value; }
		}
		public string PortNumber
		{
			get { return _port; }
			set { _port = value; }
		}
	}

	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class SearchController : SmartDispatcherController
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(SearchController));

		/*[AccessibleThrough(Verb.Post)]
		public void CloseDemand([DataBind("ConnectList")]List<Connect> forConnect)
		{
			var validSwiches = NetworkSwitches.FindAll().Where(r => ((r.Mac != null) && (r.Name != null)));
			var valiConnections = forConnect.Where(t => validSwiches.Select(y => y.Id).Contains((uint)t.SwitchId));
			if (valiConnections.Count() > 0)
			{
				foreach (var client in valiConnections)
				{
					var inetClient = new Clients();
					var portNum = 0;
					try
					{
						portNum = Convert.ToInt32(client.PortNumber);
					}
					catch (Exception)
					{
						Flash["PornNumError"] = "Номер порта принимает значение от 1 до 48, вы ввели " + client.PortNumber.ToString() +
						                        @" операция 'Подключения' прервана на клиенте " + inetClient.Name;
						break;
					}
					if ((client.ClientId != 0) && (portNum != 0) && (client.SwitchId != 0))
					{
						var forUpdate = PhisicalClients.Find((uint) client.ClientId);
						inetClient = new Clients
						             	{
						             		Name = forUpdate.Surname + " " + forUpdate.Name + " " + forUpdate.Patronymic
						             	};
						if ((portNum >= 1) && (portNum <= 48))
						{
							using (var scope = new TransactionScope(OnDispose.Rollback))
							{
								var Switch = NetworkSwitches.Find((uint) client.SwitchId);
								/*if (ClientEndpoints.FindAll(DetachedCriteria.For(typeof (ClientEndpoints))
								                            	.Add(Expression.Eq("Switch", Switch))
								                            	.Add(Expression.Eq("Port", portNum))).Length == 0)*/
								/*if (Point.isUnique(Switch, portNum))
								{
									var cdDate = RequestsConnection.FindAll(DetachedCriteria.For(typeof (RequestsConnection))
									                                        	.CreateAlias("ClientID", "PS", JoinType.InnerJoin)
									                                        	.CreateAlias("BrigadNumber", "CB", JoinType.InnerJoin)
									                                        	.Add(Expression.Eq("ClientID", forUpdate))
									                                        	.Add(Expression.Eq("ManagerID", InithializeContent.partner))
									                                        	.Add(Expression.EqProperty("BrigadNumber", "CB.Id"))
									                                        	.Add(Expression.Eq("PS.Connected", false)));
									forUpdate.Connected = true;
									forUpdate.UpdateAndFlush();
									var endPoint = new ClientEndpoints();
									inetClient.PhisicalClient = (uint) forUpdate.Id;
									inetClient.Type = ClientType.Phisical;
									inetClient.SaveAndFlush();
									endPoint.Port = portNum;
									endPoint.Switch = NetworkSwitches.Find((uint) client.SwitchId);
									endPoint.Client = inetClient;
									endPoint.PackageId = forUpdate.Tariff.PackageId;
									endPoint.SaveAndFlush();
									foreach (var requestsConnection in cdDate)
									{
										requestsConnection.CloseDemandDate = DateTime.Now;
										cdDate[0].UpdateAndFlush();
									}
									scope.VoteCommit();
								}
								else
								{
									Flash["PornNumError"] = "Такая пара порт/свич уже существует";
									break;
								}
							}
						}
						else
						{
							Flash["PornNumError"] = "Номер порта принимает значение от 1 до 48, вы ввели " + client.PortNumber.ToString() +
							                        @" операция 'Подключения' прервана на клиенте " + inetClient.Name;
							break;
						}
					}
					PropertyBag["DemandAccept"] = "Заявки закрыты";
				}
			}
			else
			{
				Flash["PornNumError"] = "Выберите заявки для закрытия";
			}
			RedirectToUrl(@"../Search/SearchBy.rails?CloseDemand=true");
		}*/

		/*[AccessibleThrough(Verb.Post)]
		public void CreateDemandConnect([DataBind("ForConnect")]List<int> forConnect, uint brigadId,
			string connectBut, string closeDemandBut)
		{
			var brigad = Brigad.Find(brigadId);
			//var Connected = new List<bool>();
			foreach (uint clientToConnect in forConnect)
			{
				var newRequest = new RequestsConnection
				                 	{
										BrigadNumber = brigad,
				                 		ManagerID = InithializeContent.partner,
				                 		ClientID = PhisicalClients.Find(clientToConnect),
				                 		RegDate = DateTime.Now
				                 	};
				newRequest.ClientID.HasConnected = newRequest.BrigadNumber;
				newRequest.SaveAndFlush();
			}
			Flash["CreateDemandConnect"] = "Заявки оформлены";
			RedirectToUrl(@"../Search/SearchUsers.rails");
		}*/

		[AccessibleThrough(Verb.Get)]
		public void SearchBy([DataBind("SearchBy")]UserSearchProperties searchProperties, 
							[DataBind("ConnectedType")]ConnectedTypeProperties connectedType,
			uint tariff, uint whoregister, uint brigad ,string searchText)
		{
			IList<PhisicalClients> clients = new List<PhisicalClients>();
			clients = GetClientsLogic.GetClients(searchProperties, connectedType, tariff, whoregister, searchText, brigad);
			Flash["SClients"] = clients;
			PropertyBag["ConnectBlockDisplay"] = ((List<PhisicalClients>) clients).Find(p => p.HasConnected == null);

			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["WhoRegistered"] = Partner.FindAllSort();
			PropertyBag["ChTariff"] = tariff;
			PropertyBag["ChRegistr"] = whoregister;
			PropertyBag["ChBrigad"] = brigad;
			PropertyBag["SearchText"] = searchText;

			PropertyBag["FindBy"] = searchProperties;
			PropertyBag["ConnectBy"] = connectedType;

			//PropertyBag["CloseDemand"] = closeDemand;

			Flash["Brigads"] = Brigad.FindAllSort();
		}


		public void SearchUsers(string query, PhisicalClients sClients)
		{
			var searchProperties = new UserSearchProperties {SearchBy = SearchUserBy.Auto};
			var connectProperties = new ConnectedTypeProperties {Type = ConnectedType.AllConnected};
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["WhoRegistered"] = Partner.FindAllSort();
			PropertyBag["SearchText"] = "";
			PropertyBag["ChTariff"] = 0;
			PropertyBag["ChRegistr"] = 0;
			PropertyBag["ChBrigad"] = 0;
			PropertyBag["FindBy"] = searchProperties;
			PropertyBag["ConnectBy"] = connectProperties;
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["Connected"] = false;
			//PropertyBag["CloseDemand"] = closeDemand;
			if (sClients != null)
			{
				Flash["SClients"] = sClients;
			}
		}
	}
}
