﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using InternetInterface.Controllers;
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
		public static string GetWhere(SeachFilter filter)
		{
			var _return = string.Empty;
			if (!InitializeContent.Partner.IsDiller()) {
				if (filter.statusType > 0)
					_return += " and S.Id = :statusType";

				if (filter.clientTypeFilter.IsPhysical())
					_return += " and C.PhysicalClient is not null";

				if (filter.clientTypeFilter.IsLawyer())
					_return += " and C.LawyerPerson is not null";

				if (filter.EnabledTypeProperties.IsDisabled())
					_return += " and c.Disabled";

				if (filter.EnabledTypeProperties.IsEnabled())
					_return += " and c.Disabled = false";

				if (!string.IsNullOrEmpty(filter.searchText)) {
					if (filter.searchProperties.IsSearchAuto()) {
						return
							String.Format(
								@"
	WHERE
	(LOWER(C.Name) like {0} or
	C.id like {0} or
	LOWER(co.Contact) like {0} or
	LOWER(h.Street) like {0} or
	LOWER(l.ActualAdress) like {0} )",
								":SearchText") + _return;
					}
					if (filter.searchProperties.IsSearchAccount()) {
						var id = 0u;
						UInt32.TryParse(filter.searchText, out id);
						if (id > 0)
							return string.Format("where C.id = {0}", id);
					}
					if (filter.searchProperties.IsSearchByFio()) {
						return
							String.Format(@"
	WHERE (LOWER(C.Name) like {0} )", ":SearchText") + _return;
					}
					if (filter.searchProperties.IsSearchTelephone()) {
						return String.Format(@"WHERE (LOWER(co.Contact) like {0})", ":SearchText") + _return;
					}
					if (filter.searchProperties.IsSearchByAddress()) {
						return String.Format(@"
	WHERE (LOWER(h.Street) like {0} or
	LOWER(l.ActualAdress) like {0})", ":SearchText") + _return;
					}
				}
			}
			else {
				var id = 0u;
				UInt32.TryParse(filter.searchText, out id);
				if (id > 0) {
					return string.Format("WHERE (c.Id = {0}) and (C.PhysicalClient is not null)", id);
				}
				else if (!string.IsNullOrEmpty(filter.searchText))
					return "WHERE (LOWER(C.Name) like :SearchText) and (C.PhysicalClient is not null)";
			}
			return string.IsNullOrEmpty(_return) ? string.Empty : string.Format("WHERE {0}", _return.Remove(0, 4));
		}
	}
}