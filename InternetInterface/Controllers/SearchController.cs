﻿using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class SearchController : SmartDispatcherController
	{
		private IList<Client> GetClients(UserSearchProperties _searchProperties, uint _tariff, uint _whoregister, string _SearchText)
		{
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
				var session = sessionHolder.CreateSession(typeof (Client));
				_searchProperties.SearchText = _SearchText;
				try
				{
					var sqlStr = String.Format(@"SELECT * FROM internet.PhysicalClients P {0} ORDER BY P.Login",
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
					throw;
					return new List<Client>();
				}
				finally
				{
					sessionHolder.ReleaseSession(session);
				}
				return new List<Client>();
		}

		private IList<Client> GetClientsForCloseDemand()
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
WHERE PA.ID = {0} and PC.Connected = false", InithializeContent.partner.Id));
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
				finally
				{
					sessionHolder.ReleaseSession(session);
				}
			return new List<Client>();

		}

		[AccessibleThrough(Verb.Post)]
		public void CloseDemand([DataBind("ForClose")]List<int> ForClose)
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

		[AccessibleThrough(Verb.Post)]
		public void CreateDemandConnect([DataBind("ForConnect")]List<int> ForConnect, uint BrigadID,
			string ConnectBut, string CloseDemandBut)
		{
			foreach (uint ClientToConnect in ForConnect)
			{
				var newRequest = new RequestsConnection
				                 	{
				                 		BrigadNumber = Brigad.Find(BrigadID),
				                 		ManagerID = InithializeContent.partner,
				                 		ClientID = Client.Find(ClientToConnect),
				                 		RegDate = DateTime.Now
				                 	};
				newRequest.ClientID.HasConnected = newRequest.BrigadNumber;
				newRequest.SaveAndFlush();
			}
			Flash["CreateDemandConnect"] = true;
			RedirectToUrl(@"../Search/SearchUsers.rails");
		}

		[AccessibleThrough(Verb.Get)]
		public void SearchBy([DataBind("SearchBy")]UserSearchProperties searchProperties, uint tariff, uint whoregister, string SearchText, Boolean CloseDemand)
		{
			//var partner = Partner.GetPartnerForLogin(Session["Login"].ToString());
			IList<Client> clients = new List<Client>();
			if (CloseDemand)
			{
				clients = GetClientsForCloseDemand();
			}
			if (!CloseDemand)
			{
				clients = GetClients(searchProperties, tariff, whoregister, SearchText);
			}
			Flash["SClients"] = clients;
			var tariffText = new List<Tariff>();
			var partnerText = new List<Partner>();
			foreach (Client t in clients)
			{
				tariffText.Add(t.Tariff);
				partnerText.Add(t.HasRegistered);
			}
			Flash["tariffText"] = tariffText;
			Flash["partnerText"] = partnerText;
			var mapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			PropertyBag["PARTNERNAME"] = mapPartner[0].Name;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["ChTariff"] = tariff;
			PropertyBag["ChRegistr"] = whoregister;
			PropertyBag["WhoRegistered"] = Partner.FindAllSort();
			PropertyBag["FindBy"] = searchProperties;
			PropertyBag["CloseDemand"] = CloseDemand;
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["SearchText"] = SearchText;
			Flash["Brigads"] = Brigad.FindAllSort();
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
			var searchProperties = new UserSearchProperties {SearchBy = SearchUserBy.Auto};
			PropertyBag["PARTNERNAME"] = InithializeContent.partner.Name;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["WhoRegistered"] = Partner.FindAllSort();
			PropertyBag["SearchText"] = "";
			PropertyBag["ChTariff"] = 0;
			PropertyBag["ChRegistr"] = 0;
			PropertyBag["FindBy"] = searchProperties;
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			if (SClients != null)
			{
				Flash["SClients"] = SClients;
			}
		}
	}
}
