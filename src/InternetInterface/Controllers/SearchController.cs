using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using log4net;
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

		private IList<PhisicalClients> GetClients(UserSearchProperties searchProperties, ConnectedTypeProperties connectedType, uint tariff, uint whoregister, string searchText, uint brigad)
		{
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
				var session = sessionHolder.CreateSession(typeof (PhisicalClients));
				searchProperties.SearchText = searchText;
				try
				{
					var sqlStr = String.Format(@"SELECT * FROM internet.PhysicalClients P {0} ORDER BY P.Login",
					                           GetWhere(searchProperties, connectedType, whoregister, tariff, searchText, brigad));
					var query = session.CreateSQLQuery(sqlStr).AddEntity(typeof (PhisicalClients));
					if (whoregister != 0)
						query.SetParameter("whoregister", whoregister);
					if (tariff != 0)
						query.SetParameter("tariff", tariff);
					if (brigad != 0)
						query.SetParameter("Brigad", brigad);
					if (connectedType.IsConnected())
						query.SetParameter("Connected", true);
					if (connectedType.IsNoConnected())
						query.SetParameter("Connected", false);
					if (searchText != null)
						query.SetParameter("SearchText", "%" + searchText.ToLower() + "%");
					var result = query.List<PhisicalClients>();
					foreach (var item in result)
						session.Evict(item);
					return result;
				}
				catch (Exception e)
				{
					_log.Error("Не выбирается клиенты при поиске с фильтром");
				}
				finally
				{
					sessionHolder.ReleaseSession(session);
				}
				return new List<PhisicalClients>();
		}

		private IList<PhisicalClients> GetClientsForCloseDemand()
		{
				var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
				var session = sessionHolder.CreateSession(typeof(PhisicalClients));
				try
				{
					var sqlStr = String.Format(String.Format(@"Select PC.Id, PC.name, PC.Surname, PC.Patronymic, PC.City, PC.AdressConnect,
PC.PassportSeries, PC.PassportNumber, PC.WhoGivePassport, PC.RegistrationAdress,
PC.RegDate, PC.Tariff, PC.Balance, PC.Login, PC.Password, PC.HasRegistered, PC.HasConnected, PC.Connected
FROM internet.RequestsConnection R
join internet.ConnectBrigads CB on R.BrigadNumber = CB.ID
Join internet.PhysicalClients PC on R.ClientID = PC.Id
join internet.Partners PA on CB.PartnerID = PA.Id
WHERE PA.ID = {0} and PC.Connected = false", InithializeContent.partner.Id));
					var query = session.CreateSQLQuery(sqlStr).AddEntity(typeof (PhisicalClients));
					var result = query.List<PhisicalClients>();
					foreach (var item in result)
						session.Evict(item);
					return result;
				}
				catch (Exception e)
				{
					_log.Error("Ошибка при выборке книетов для закрытия заявок");
				}
				finally
				{
					sessionHolder.ReleaseSession(session);
				}
			return new List<PhisicalClients>();

		}

		[AccessibleThrough(Verb.Post)]
		public void CloseDemand([DataBind("ConnectList")]List<Connect> forConnect)
		{
			if (forConnect.Find(t => t.SwitchId != 6) != null)
			{
				foreach (var client in forConnect)
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
								if (ClientEndpoints.FindAll(DetachedCriteria.For(typeof (ClientEndpoints))
								                            	.Add(Expression.Eq("Switch", Switch))
								                            	.Add(Expression.Eq("Port", portNum))).Length == 0)
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
									inetClient.PhisicalClient = (int) forUpdate.Id;
									inetClient.Type = ClientType.Phisical;
									inetClient.SaveAndFlush();
									endPoint.Port = portNum;
									endPoint.Switch = NetworkSwitches.Find((uint) client.SwitchId);
									endPoint.Client = inetClient;
									endPoint.SaveAndFlush();
									cdDate[0].CloseDemandDate = DateTime.Now;
									cdDate[0].UpdateAndFlush();
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
		}

		[AccessibleThrough(Verb.Post)]
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
		}

		[AccessibleThrough(Verb.Get)]
		public void SearchBy([DataBind("SearchBy")]UserSearchProperties searchProperties, 
							[DataBind("ConnectedType")]ConnectedTypeProperties connectedType,
			uint tariff, uint whoregister, uint brigad ,string searchText, Boolean closeDemand)
		{
			//var partner = Partner.GetPartnerForLogin(Session["Login"].ToString());
			IList<PhisicalClients> clients = new List<PhisicalClients>();
			if (closeDemand)
			{
				Flash["ConnectAdd"] = new Connect();
				Flash["ConnectList"] = new List<Connect>();
				Flash["Switches"] = NetworkSwitches.FindAllSort();
				clients = GetClientsForCloseDemand();
			}
			if (!closeDemand)
			{
				clients = GetClients(searchProperties, connectedType, tariff, whoregister, searchText, brigad);
			}
			Flash["SClients"] = clients;
			PropertyBag["ConnectBlockDisplay"] = ((List<PhisicalClients>) clients).Find(p => p.HasConnected == null);
			/*var tariffText = new List<Tariff>();
			var partnerText = new List<Partner>();
			foreach (var t in clients)
			{
				tariffText.Add(t.Tariff);
				partnerText.Add(t.HasRegistered);
			}*/
			//Flash["tariffText"] = tariffText;
			//Flash["partnerText"] = partnerText;
			//var mapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["WhoRegistered"] = Partner.FindAllSort();
			PropertyBag["ChTariff"] = tariff;
			PropertyBag["ChRegistr"] = whoregister;
			PropertyBag["ChBrigad"] = brigad;
			PropertyBag["SearchText"] = searchText;

			PropertyBag["FindBy"] = searchProperties;
			PropertyBag["ConnectBy"] = connectedType;

			PropertyBag["CloseDemand"] = closeDemand;

			/*PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["PARTNERNAME"] = InithializeContent.partner.Name;*/
			
			Flash["Brigads"] = Brigad.FindAllSort();
			//PropertyBag["Connected"] = connected;
		}


		private string GetWhere(UserSearchProperties sp, ConnectedTypeProperties ct, uint whoregister, uint tariff, string searchText, uint brigad)
		{
			string _return = string.Empty;
			if (whoregister != 0)
			{
				_return += " and P.HasRegistered = :whoregister";
			}

			if (tariff != 0)
			{
				_return += " and P.Tariff = :tariff";
			}
			if (brigad != 0)
			{
				_return += " and P.HasConnected = :Brigad";
			}
			if ((ct.IsConnected())||(ct.IsNoConnected()))
			{
				_return += " and P.Connected = :Connected";
			}
			if (searchText != null)
			{
				if (sp.IsSearchAuto())
				{
					return
						String.Format(
							@"
WHERE LOWER(P.Name) like {0} or LOWER(P.Surname) like {0}
or LOWER(P.Patronymic) like {0} or LOWER(P.City) like {0} 
or LOWER(P.AdressConnect) like {0} or LOWER(P.PassportSeries) like {0}
or LOWER(P.PassportNumber) like {0} or LOWER(P.WhoGivePassport) like {0}
or LOWER(P.RegistrationAdress) like {0} or LOWER(P.Login) like {0}",
							":SearchText") + _return;
				}
				if (sp.IsSearchByFio())
				{
					return
						String.Format(@"
WHERE LOWER(P.Name) like {0} or LOWER(P.Surname) like {0}
or LOWER(P.Patronymic) like {0}", ":SearchText") + _return;
				}
				if (sp.IsSearchByLogin())
				{
					return String.Format(@"WHERE LOWER(P.Login) like {0}", ":SearchText") + _return;
				}
				if (sp.IsSearchByPassportSet())
				{
					return
						String.Format(
							@"
WHERE LOWER(P.PassportSeries) like {0}
or LOWER(P.PassportNumber) like {0} or LOWER(P.WhoGivePassport) like {0}
or LOWER(P.RegistrationAdress) like {0}", ":SearchText") + _return;
				}
			}
			else
			{
				if (_return != string.Empty)
				return "WHERE" + _return.Remove(0, 4);
				return _return;
			}
			return string.Empty;
		}

		public void SearchUsers(string query, PhisicalClients sClients, Boolean closeDemand)
		{
			var searchProperties = new UserSearchProperties {SearchBy = SearchUserBy.Auto};
			var connectProperties = new ConnectedTypeProperties {Type = ConnectedType.AllConnected};
			//PropertyBag["PARTNERNAME"] = InithializeContent.partner.Name;
			//PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
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
			PropertyBag["CloseDemand"] = closeDemand;
			if (sClients != null)
			{
				Flash["SClients"] = sClients;
			}
		}
	}
}
