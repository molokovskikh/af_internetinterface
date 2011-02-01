using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace InternetInterface.AllLogic
{
	public class GetClientsLogic
	{
		/*public static IList<PhisicalClients> GetClientsForCloseDemand()
		{
			IList<PhisicalClients> _result = new List<PhisicalClients>();
			ARSesssionHelper<PhisicalClients>.QueryWithSession(session =>
			{
				var result = RequestsConnection.FindAll(DetachedCriteria.For(typeof(RequestsConnection))
															.CreateAlias("BrigadNumber", "BR", JoinType.InnerJoin)
															.CreateAlias("ClientID", "PC", JoinType.InnerJoin)
															.Add(Expression.Eq("BR.PartnerID", InithializeContent.partner))
															.Add(Expression.Eq("PC.Connected", false))).ToList();
				var PcList = result.Select(requestsConnection => requestsConnection.ClientID).ToList();
				_result = PcList;
				return PcList;
			});
			return _result;
		}
		*/


		public static IList<PhisicalClients> GetClients(UserSearchProperties searchProperties,
			ConnectedTypeProperties connectedType, uint tariff, uint whoregister, string searchText, uint brigad)
		{
			IList<PhisicalClients> result = new List<PhisicalClients>();
			ARSesssionHelper<PhisicalClients>.QueryWithSession(session =>
			{
				searchProperties.SearchText = searchText;
				var sqlStr = string.Empty;
				ISQLQuery query = null;
				if (CategorieAccessSet.AccesPartner("SSI"))
				if (!searchProperties.IsSearchAccount())
				{
					sqlStr = String.Format(@"SELECT * FROM internet.PhysicalClients P {0} ORDER BY P.Surname",
										   GetWhere(searchProperties, connectedType, whoregister, tariff, searchText, brigad));
					query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(PhisicalClients));
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
				}
				else
				{
					sqlStr = @"SELECT * FROM internet.PhysicalClients P where P.id = :SearchText ORDER BY P.Surname";
					query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(PhisicalClients));
					if (searchText != null)
						query.SetParameter("SearchText", searchText.ToLower());
				}
				else
				{
					if (searchText != null)
					sqlStr = string.Format(@"SELECT * FROM internet.PhysicalClients P 
WHERE LOWER(P.Name) like {0} or LOWER(P.Surname) like {0} or LOWER(P.Patronymic) like {0} or LOWER(P.Id) like {0}
ORDER BY P.Surname", ":SearchText");
					else
					{
						sqlStr = @"SELECT * FROM internet.PhysicalClients P ORDER BY P.Surname";
					}
					query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(PhisicalClients));
					if (searchText != null)
						query.SetParameter("SearchText", "%" + searchText.ToLower() + "%");
				}
				result = query.List<PhisicalClients>();
				return result;
			});
			return result;
		}


		public static string GetWhere(UserSearchProperties sp, ConnectedTypeProperties ct, uint whoregister, uint tariff, string searchText, uint brigad)
		{
			var _return = string.Empty;
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
			if ((ct.IsConnected()) || (ct.IsNoConnected()))
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
or LOWER(P.PassportSeries) like {0}
or LOWER(P.PassportNumber) like {0} or LOWER(P.WhoGivePassport) like {0}
or LOWER(P.RegistrationAdress) like {0}",
							":SearchText") + _return;
				}
				if (sp.IsSearchByFio())
				{
					return
						String.Format(@"
WHERE LOWER(P.Name) like {0} or LOWER(P.Surname) like {0}
or LOWER(P.Patronymic) like {0}", ":SearchText") + _return;
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
	}
}