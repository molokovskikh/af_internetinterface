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
			var MapPartner = Partner.FindAllByProperty("Pass", Session["HashPass"]);
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

		[AccessibleThrough(Verb.Get)]
		public void SearchBy([DataBind("SearchBy")]UserSearchProperties searchProperties, uint tariff, uint whoregister, string SearchText)
		{
				var MapPartner = Partner.FindAllByProperty("Pass", Session["HashPass"]);
				if (MapPartner.Length != 0)
				{
					var Clients = GetClients(searchProperties, tariff, whoregister, SearchText);
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
					var mapPartner = Partner.FindAllByProperty("Pass", Session["HashPass"]);
					PropertyBag["PARTNERNAME"] = mapPartner[0].Name;
					PropertyBag["Tariffs"] = Tariff.FindAll();
					PropertyBag["ChTariff"] = tariff;
					PropertyBag["ChRegistr"] = whoregister;
					PropertyBag["WhoRegistered"] = Partner.FindAll();
					PropertyBag["FindBy"] = searchProperties;
				}
				else
				{
					RedirectToUrl(@"..\\Errors\AccessDin.aspx");
				}
			//RedirectToUrl(@"SearchUsers?Query=YES");

		}


		private string GetWhere(UserSearchProperties Sp, uint whoregister, uint tariff, string SearchText)
		{
			string _return = "";
			if (whoregister != 0)
			{
				//string _register = Partner.FindAllByProperty("Name", whoregister)[0].Id.ToString();
				_return += " and P.HasRegistered = :whoregister";
			}

			if (tariff != 0)
			{
				//string _tariff = Tariff.FindAllByProperty("Name", tariff)[0].Id.ToString();
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
			/*if (Session["HashPass"] == null)
			{
				
			}*/
			var searchProperties = new UserSearchProperties { SearchBy = SearchUserBy.Auto };
			var MapPartner = Partner.FindAllByProperty("Pass", Session["HashPass"]);
			if (MapPartner.Length != 0)
			{
				PropertyBag["PARTNERNAME"] = MapPartner[0].Name;
				PropertyBag["Tariffs"] = Tariff.FindAll();
				PropertyBag["WhoRegistered"] = Partner.FindAll();
				PropertyBag["SearchText"] = "";
				PropertyBag["ChTariff"] = 0;
				PropertyBag["ChRegistr"] = 0;
				PropertyBag["FindBy"] = searchProperties;
				if (SClients != null)
				{
					Flash["SClients"] = SClients;
				}
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}
	}
}
