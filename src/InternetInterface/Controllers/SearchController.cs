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
							[DataBind("ConnectedType")]ConnectedTypeProperties connectedType,
			uint tariff, uint whoregister, uint brigad ,string searchText)
		{
			IList<PhysicalClients> clients = new List<PhysicalClients>();
			clients = GetClientsLogic.GetClients(searchProperties, connectedType, tariff, whoregister, searchText, brigad);
			Flash["SClients"] = clients;
			PropertyBag["ConnectBlockDisplay"] = ((List<PhysicalClients>) clients).Find(p => p.WhoConnected == null);

			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["WhoRegistered"] = Partner.FindAllSort();
			PropertyBag["ChTariff"] = tariff;
			PropertyBag["ChRegistr"] = whoregister;
			PropertyBag["ChBrigad"] = brigad;
			PropertyBag["SearchText"] = searchText;

			PropertyBag["FindBy"] = searchProperties;
			PropertyBag["ConnectBy"] = connectedType;

			Flash["Brigads"] = Brigad.FindAllSort();
		}


		public void SearchUsers(string query, PhysicalClients sClients)
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
			if (sClients != null)
			{
				Flash["SClients"] = sClients;
			}
		}
	}
}
