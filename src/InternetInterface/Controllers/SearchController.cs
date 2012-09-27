using System;
using System.Collections;
using System.Text;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Queries;
using log4net;
using MonoRail.Debugger.Toolbar;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace InternetInterface.Controllers
{
	public class ClientInfo
	{
		public ClientInfo(Client _client)
		{
			client = _client;
		}

		public Client client;
		public bool OnLine;

		public string Address
		{
			get
			{
				if (client.IsPhysical())
					return client.GetAdress();
				else {
					return client.LawyerPerson.ActualAdress;
				}
			}
		}

		public virtual string ForSearchAddress(string query)
		{
			return TextHelper.SelectQuery(query, Address);
		}
	}

	[Helper(typeof(PaginatorHelper)),
	 Helper(typeof(CategorieAccessSet)),]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class SearchController : BaseController
	{
		[AccessibleThrough(Verb.Get)]
		public void SearchBy([DataBind("filter")] SeachFilter filter)
		{
			var result = filter.Find();
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
			var filter = new SeachFilter {
				SearchProperties = SearchUserBy.Auto,
				EnabledTypeProperties = EndbledType.All,
				StatusType = 0,
				ClientTypeFilter = ForSearchClientType.AllClients,
			};
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
			PropertyBag["Brigads"] = Brigad.FindAllAdd();
			PropertyBag["additionalStatuses"] = AdditionalStatus.FindAllAdd();
			PropertyBag["Tariffs"] = Tariff.FindAllAdd();
			PropertyBag["WhoRegistered"] = Partner.FindAllAdd();
			PropertyBag["Stat"] = new Statistic(DbSession).GetStatistic();
		}

		public void Redirect([DataBind("filter")] ClientFilter filter)
		{
			var builder = string.Empty;
			foreach (string name in Request.QueryString)
				builder += String.Format("{0}={1}&", name, Request.QueryString[name]);
			builder = builder.Substring(0, builder.Length - 1);
			if (Client.Find(filter.ClientCode).GetClientType() == ClientType.Phisical) {
				RedirectToUrl(string.Format("../UserInfo/SearchUserInfo.rails?{0}", builder));
			}
			else {
				RedirectToUrl(string.Format("../UserInfo/LawyerPersonInfo.rails?{0}", builder));
			}
			CancelView();
		}
	}
}