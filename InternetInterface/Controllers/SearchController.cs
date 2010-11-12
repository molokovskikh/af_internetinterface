using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InternetInterface.Models;
using NHibernate;

namespace InternetInterface.Controllers
{

	public class SearchController : SmartDispatcherController
	{

		public IList<Client> GetClients(UserSearchProperties _searchProperties, uint _tariff, uint _whoregister, string _SearchText)
		{
			var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if (MapPartner.Length != 0)
			{
				var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
				var session = sessionHolder.CreateSession(typeof (Client));
				_searchProperties.SearchText = _SearchText;
				try
				{
					var sqlStr = String.Format(@"SELECT * FROM internet.PhysicalClients P {0} GROUP BY P.Id",
					                           GetWhere(_searchProperties, _whoregister, _tariff, _SearchText));
					var query = session.CreateSQLQuery(sqlStr).AddEntity(typeof (Client))
						.SetParameter("whoregister", _whoregister)
						.SetParameter("tariff", _tariff);
					if (_SearchText != null)
						query.SetParameter("SearchText", "%" + _SearchText.ToLower() + "%");
					var result = query.List<Client>();
					foreach (var item in result)
						session.Evict(item);
					return result;
				}
				catch (Exception e)
				{
					return new List<Client>();
				}
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
			return new List<Client>();
		}

		public IList<Client> GetClientsForCloseDemand()
		{
			var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if (MapPartner.Length != 0)
			{
				var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
				var session = sessionHolder.CreateSession(typeof(Client));
				try
				{
					var sqlStr = String.Format(String.Format(@"Select PC.Id, PC.name, PC.Surname, PC.Patronymic, PC.City, PC.AdressConnect,
PC.PassportSeries, PC.PassportNumber, PC.WhoGivePassport, PC.RegistrationAdress,
PC.RegDate, PC.Tariff, PC.Balance, PC.Login, PC.Password, PC.HasRegistered, PC.HasConnected, PC.Connected
FROM internet.RequestsConnection R
join internet.ConnectBrigads CB on R.BrigadNumber = CB.ID
Join internet.PhysicalClients PC on R.ClientID = PC.Id
join accessright.Partners PA on CB.PartnerID = PA.Id
WHERE PA.ID = {0} and PC.Connected = false", MapPartner[0].Id));
					var query = session.CreateSQLQuery(sqlStr).AddEntity(typeof (Client));
					var result = query.List<Client>();
					foreach (var item in result)
						session.Evict(item);
					return result;
				}
				catch (Exception e)
				{
					throw;
					return new List<Client>();
				}
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
			return new List<Client>();

		}

		[AccessibleThrough(Verb.Post)]
		public void CreateDemandConnect([DataBind("ForConnect")]List<int> ForConnect, uint BrigadID,
			[DataBind("ForClose")]List<int> ForClose, string ConnectBut, string CloseDemandBut)
		{
			var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if ((ConnectBut != null) && (CloseDemandBut != null))
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
			else
			{
				if ((MapPartner.Length != 0) && (AccessCategories.AccesPartner(MapPartner[0], (uint)AccessCategoriesType.SendDemand)) && (ConnectBut == "Connecting"))
				{
					var MID = Partner.FindAllByProperty("Login", Session["Login"])[0];
					foreach (uint ClientToConnect in ForConnect)
					{
						var newRequest = new RequestsConnection
						                 	{
						                 		BrigadNumber = Brigad.Find(BrigadID),
						                 		ManagerID = MID,
						                 		ClientID = Client.Find(ClientToConnect),
						                 		RegDate = DateTime.Now
											};
						newRequest.ClientID.HasConnected = newRequest.BrigadNumber;
						newRequest.SaveAndFlush();
					}
					Flash["CreateDemandConnect"] = true;
					RedirectToUrl(@"../Search/SearchUsers.rails");
				}
				else
				{
					if ((MapPartner.Length != 0) && (AccessCategories.AccesPartner(MapPartner[0], (uint)AccessCategoriesType.CloseDemand)) &&
					    (CloseDemandBut == "CloseDemand"))
					{
						foreach (uint i in ForClose)
						{
							var forUpdate = Client.Find(i);
							forUpdate.Connected = true;
							forUpdate.UpdateAndFlush();
						}
						Flash["DemandAccept"] = true;
						RedirectToUrl(@"../Search/SearchBy.rails?CloseDemand=true");
					}
					else
					{
						RedirectToUrl(@"..\\Errors\AccessDin.aspx");
					}
				}
			}
			//var g = Connecting[0];
		}

		[AccessibleThrough(Verb.Get)]
		public void SearchBy([DataBind("SearchBy")]UserSearchProperties searchProperties, uint tariff, uint whoregister, string SearchText, Boolean CloseDemand)
		{
				var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
				if (MapPartner.Length != 0)
				{
					IList<Client> Clients = new List<Client>();
					if ((CloseDemand) && (AccessCategories.AccesPartner(MapPartner[0], (uint)AccessCategoriesType.CloseDemand)))
					{
						Clients = GetClientsForCloseDemand();
					}
					if ((!CloseDemand) && (AccessCategories.AccesPartner(MapPartner[0], (uint)AccessCategoriesType.GetClientInfo)))
					{
						Clients = GetClients(searchProperties, tariff, whoregister, SearchText);
					}
					Flash["SClients"] = Clients;
					var TariffText = new List<Tariff>();
					var PartnerText = new List<Partner>();
					for (int i = 0; i < Clients.Count; i++)
					{
						TariffText.Add(Clients[i].Tariff);
						PartnerText.Add(Clients[i].HasRegistered);
					}
					Flash["tariffText"] = TariffText;
					Flash["partnerText"] = PartnerText;
					var mapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
					PropertyBag["PARTNERNAME"] = mapPartner[0].Name;
					PropertyBag["Tariffs"] = Tariff.FindAll();
					PropertyBag["ChTariff"] = tariff;
					PropertyBag["ChRegistr"] = whoregister;
					PropertyBag["WhoRegistered"] = Partner.FindAll();
					PropertyBag["FindBy"] = searchProperties;
					if ((AccessCategories.AccesPartner(mapPartner[0], (uint)AccessCategoriesType.SendDemand)) && (!CloseDemand))
					{
						Flash["Brigads"] = Brigad.FindAll();
						PropertyBag["ConnectAccess"] = true;
						PropertyBag["FindAccess"] = true;
					}
					else
					{
						if ((AccessCategories.AccesPartner(mapPartner[0], (uint)AccessCategoriesType.GetClientInfo)) && (!CloseDemand))
						{
							PropertyBag["ConnectAccess"] = false;
							PropertyBag["FindAccess"] = true;
						}
						else
						{
							PropertyBag["ConnectAccess"] = false;
							PropertyBag["FindAccess"] = false;
						}
					}
					if ((AccessCategories.AccesPartner(mapPartner[0], (uint)AccessCategoriesType.CloseDemand)) && (CloseDemand))
					{
						PropertyBag["CloseDemand"] = true;
					}
					else
					{
						PropertyBag["CloseDemand"] = false;
					}
				}
				else
				{
					RedirectToUrl(@"..\\Errors\AccessDin.aspx");
				}
		}


		private string GetWhere(UserSearchProperties Sp, uint whoregister, uint tariff, string SearchText)
		{
			string _return = "";
			if (whoregister != 0)
			{
				_return += " and P.HasRegistered = :whoregister";
			}

			if (tariff != 0)
			{
				_return += " and P.Tariff = :tariff";
			}
			if ((whoregister != 0) && (tariff == 0))
			{
				_return += " and :tariff = :tariff";
			}
			if ((whoregister == 0) && (tariff != 0))
			{
				_return += " and :whoregister = :whoregister";
			}
			if ((whoregister == 0) && (tariff == 0))
			{
				_return += " and :tariff = :tariff and :whoregister = :whoregister";
			}
			if (SearchText != null)
			{
				if (Sp.IsSearchAuto())
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
				if (Sp.IsSearchByFio())
				{
					return
						String.Format(@"
WHERE LOWER(P.Name) like {0} or LOWER(P.Surname) like {0}
or LOWER(P.Patronymic) like {0}",
						              ":SearchText") + _return;
				}
				if (Sp.IsSearchByLogin())
				{
					return String.Format(@"WHERE LOWER(P.Login) like {0}", ":SearchText") + _return;
				}
				if (Sp.IsSearchByPassportSet())
				{
					return
						String.Format(
							@"
WHERE LOWER(P.PassportSeries) like {0}
or LOWER(P.PassportNumber) like {0} or LOWER(P.WhoGivePassport) like {0}
or LOWER(P.RegistrationAdress) like {0}",
							":SearchText") + _return;
				}
			}
			else
			{
				return "WHERE" + _return.Remove(0, 4);
			}
			return "";
		}

		public void SearchUsers(string Query, Client SClients)
		{
			var searchProperties = new UserSearchProperties { SearchBy = SearchUserBy.Auto };
			var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if (MapPartner.Length != 0)
			{
				if (AccessCategories.AccesPartner(MapPartner[0], (uint)AccessCategoriesType.GetClientInfo))
				{
					PropertyBag["PARTNERNAME"] = MapPartner[0].Name;
					PropertyBag["Tariffs"] = Tariff.FindAll();
					PropertyBag["WhoRegistered"] = Partner.FindAll();
					PropertyBag["SearchText"] = "";
					PropertyBag["ChTariff"] = 0;
					PropertyBag["ChRegistr"] = 0;
					PropertyBag["FindBy"] = searchProperties;
					PropertyBag["ConnectAccess"] = true;
					PropertyBag["findAccess"] = true;
					if (SClients != null)
					{
						Flash["SClients"] = SClients;
					}
				} else
				{
					PropertyBag["ConnectAccess"] = false;
					RedirectToUrl(@"../Search/SearchBy.rails?CloseDemand=true");
				}
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}
	}
}
