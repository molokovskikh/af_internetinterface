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

		[AccessibleThrough(Verb.Get)]
		public void SearchBy([DataBind("SearchBy")]UserSearchProperties searchProperties,
							[DataBind("ConnectedType")]ConnectedTypeProperties connectedType, [DataBind("clientTypeFilter")]ClientTypeProperties clientTypeFilter, 
			uint tariff, uint whoregister, uint brigad ,string searchText)
		{
			IList<Clients> clients = new List<Clients>();
			clients = GetClientsLogic.GetClients(searchProperties, connectedType, clientTypeFilter, tariff, whoregister, searchText, brigad);
			Flash["SClients"] = clients;
			//PropertyBag["ConnectBlockDisplay"] = ((List<Clients>) clients).Find(p => p.PhysicalClient.WhoConnected == null || p.LawyerPerson.WhoConnected == null);

			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["WhoRegistered"] = Partner.FindAllSort();
			PropertyBag["ChTariff"] = tariff;
			PropertyBag["ChRegistr"] = whoregister;
			PropertyBag["ChBrigad"] = brigad;
			PropertyBag["SearchText"] = searchText;
			PropertyBag["clientTypeFilter"] = clientTypeFilter;
			PropertyBag["FindBy"] = searchProperties;
			PropertyBag["ConnectBy"] = connectedType;

			Flash["Brigads"] = Brigad.FindAllSort();
		}


		public void SearchUsers(string query, PhysicalClients sClients)
		{
			var searchProperties = new UserSearchProperties {SearchBy = SearchUserBy.Auto};
			var connectProperties = new ConnectedTypeProperties {Type = ConnectedType.AllConnected};
			var clientTypeFilter = new ClientTypeProperties { Type = ForSearchClientType.AllClients };
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["WhoRegistered"] = Partner.FindAllSort();
			PropertyBag["SearchText"] = "";
			PropertyBag["ChTariff"] = 0;
			PropertyBag["ChRegistr"] = 0;
			PropertyBag["ChBrigad"] = 0;
			PropertyBag["FindBy"] = searchProperties;
			PropertyBag["ConnectBy"] = connectProperties;
			PropertyBag["clientTypeFilter"] = clientTypeFilter;
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["Connected"] = false;
			if (sClients != null)
			{
				Flash["SClients"] = sClients;
			}
		}

		public void Redirect(uint ClientCode)
		{
			var client = Clients.Find(ClientCode);
			if (client.PhysicalClient != null)
				RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientCode);
			else
				RedirectToUrl("../UserInfo/LawyerPersonInfo.rails?ClientCode=" + ClientCode);
			CancelView();
		}
	}
}
