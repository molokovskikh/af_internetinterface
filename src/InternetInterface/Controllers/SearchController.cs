using System;
using System.Collections;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Queries;
using InternetInterface.Services;
using log4net;
using MonoRail.Debugger.Toolbar;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.SqlCommand;

namespace InternetInterface.Controllers
{
	public class ClientInfo
	{
		public ClientInfo(Client client)
		{
			this.client = client;
		}

		public Client client;
		public bool OnLine;

		public string Address { get { return client.GetAdress(); } }

		public virtual string ForSearchAddress(string query)
		{
			return TextHelper.SelectQuery(query, Address);
		}
	}

	[Helper(typeof(PaginatorHelper))]
	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class SearchController : BaseController
	{
		[AccessibleThrough(Verb.Get)]
		public void SearchBy([SmartBinder("filter")] SearchFilter filter)
		{
			var result = filter.Find(DbSession);
			if (result.Count == 1) {
				RedirectToUrl(result[0].client.Redirect());
				return;
			}
			PropertyBag["SClients"] = result;
			PropertyBag["Direction"] = filter.Direction;
			PropertyBag["SortBy"] = filter.SortBy;
			PropertyBag["filter"] = filter;
			PropertyBag["forClientSearch"] = true;
			AddIndispensableParameters();
		}

		public void SearchUsers(string query, PhysicalClient sClients)
		{
			var filter = new SearchFilter();
			PropertyBag["filter"] = filter;
			PropertyBag["SearchText"] = "";
			PropertyBag["ChTariff"] = 0;
			PropertyBag["ChRegistr"] = 0;
			PropertyBag["ChBrigad"] = 0;
			PropertyBag["Connected"] = false;
			PropertyBag["ChAdditional"] = 0;
			if (sClients != null) {
				Flash["SClients"] = sClients;
			}
			AddIndispensableParameters();
		}

		private void AddIndispensableParameters()
		{
			PropertyBag["Statuses"] = Status.FindAllAdd();
			PropertyBag["RegionList"] = RegionHouse.FindAllAdd();
			PropertyBag["additionalStatuses"] = AdditionalStatus.FindAllAdd();
			PropertyBag["Tariffs"] = Tariff.FindAllAdd();
			PropertyBag["WhoRegistered"] = Partner.FindAllAdd();
			PropertyBag["serviceItems"] = DbSession.Query<Service>().OrderBy(s => s.HumanName).ToList();
		}

		public void Redirect([SmartBinder("filter")] ClientFilter filter)
		{
			var builder = string.Empty;
			foreach (string name in Request.QueryString)
				builder += String.Format("{0}={1}&", name, Request.QueryString[name]);
			builder = builder.Substring(0, builder.Length - 1);
			if (DbSession.Load<Client>(filter.ClientCode).GetClientType() == ClientType.Phisical) {
				RedirectToUrl(string.Format("../UserInfo/SearchUserInfo.rails?{0}", builder));
			}
			else {
				RedirectToUrl(string.Format("../UserInfo/LawyerPersonInfo.rails?{0}", builder));
			}
		}
	}
}