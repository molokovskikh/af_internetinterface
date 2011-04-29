﻿using System;
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
		public static IList<Clients> GetClients(UserSearchProperties searchProperties,
			ConnectedTypeProperties connectedType, ClientTypeProperties clientType, uint tariff, uint whoregister, string searchText, uint brigad)
		{
			IList<Clients> result = new List<Clients>();
			ARSesssionHelper<Clients>.QueryWithSession(session =>
			{
				searchProperties.SearchText = searchText;
				var sqlStr = string.Empty;
				ISQLQuery query = null;
				if (CategorieAccessSet.AccesPartner("SSI"))
				if (!searchProperties.IsSearchAccount())
				{
					sqlStr = String.Format(@"SELECT * FROM internet.Clients c
left join internet.PhysicalClients p on p.id = c.PhysicalClient
left join internet.LawyerPerson l on l.id = c.LawyerPerson
join internet.Status S on s.id = p.Status or s.id = l.status
{0} ORDER BY C.Name",
					GetWhere(searchProperties, connectedType, clientType, whoregister, tariff, searchText, brigad));
					query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Clients));
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
					sqlStr = @"SELECT * FROM internet.Clients c
left join internet.PhysicalClients p on p.id = c.PhysicalClient
left join internet.LawyerPerson l on l.id = c.LawyerPerson
join internet.Status S on s.id = p.Status or s.id = l.status
where C.id = :SearchText ORDER BY C.name";
					query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Clients));
					if (searchText != null)
						query.SetParameter("SearchText", searchText.ToLower());
				}
				else
				{
					if (searchText != null)
						sqlStr = string.Format(@"SELECT * FROM internet.Clients c
left join internet.PhysicalClients p on p.id = c.PhysicalClient
left join internet.LawyerPerson l on l.id = c.LawyerPerson
join internet.Status S on s.id = p.Status or s.id = l.status
WHERE LOWER(C.Name) like {0} or LOWER(C.Id) like {0}
ORDER BY C.Name", ":SearchText");
					else
					{
						sqlStr = @"SELECT * FROM internet.Clients P ORDER BY C.name";
					}
					query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Clients));
					if (searchText != null)
						query.SetParameter("SearchText", "%" + searchText.ToLower() + "%");
				}
				result = query.List<Clients>();
				return result;
			});
			return result;
		}


		public static string GetWhere(UserSearchProperties sp, ConnectedTypeProperties ct, ClientTypeProperties clT, uint whoregister, uint tariff, string searchText, uint brigad)
		{
			var _return = string.Empty;
			if (whoregister != 0)
			{
				_return += " and P.WhoRegistered = :whoregister or l.WhoRegistered = :whoregister";
			}

			if (tariff != 0)
			{
				_return += " and P.Tariff = :tariff";
			}
			if (brigad != 0)
			{
				_return += " and P.WhoConnected = :Brigad or l.WhoConnected = :Brigad";
			}
			if ((ct.IsConnected()) || (ct.IsNoConnected()))
			{
				_return += " and S.Connected = :Connected";
			}
			if (clT.IsPhysical())
			{
				_return += " and C.PhysicalClient is not null";
			}
			if (clT.IsLawyer())
			{
				_return += " and C.LawyerPerson is not null";
			}
			if (searchText != null)
			{
				if (sp.IsSearchAuto())
				{
					return
						String.Format(
							@"
WHERE LOWER(C.Name) like {0} or C.id = :SearchText",
							":SearchText") + _return;
				}
				if (sp.IsSearchByFio())
				{
					return
						String.Format(@"
WHERE LOWER(C.Name) like {0} ", ":SearchText") + _return;
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