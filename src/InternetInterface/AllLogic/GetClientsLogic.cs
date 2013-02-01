using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Queries;
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
				if (filter.StatusType > 0)
					_return += " and S.Id = :statusType";

				if (filter.ClientTypeFilter == ForSearchClientType.Physical)
					_return += " and C.PhysicalClient is not null";

				if (filter.ClientTypeFilter == ForSearchClientType.Lawyer)
					_return += " and C.LawyerPerson is not null";

				if (filter.EnabledTypeProperties == EndbledType.Disabled)
					_return += " and c.Disabled";

				if (filter.EnabledTypeProperties == EndbledType.Enabled)
					_return += " and c.Disabled = false";

				if(filter.Region != null && filter.Region > 0) {
					_return += " and (h.RegionId = :regionid or l.RegionId = :regionid)";
				}

				if (!string.IsNullOrEmpty(filter.SearchText)) {
					if (filter.SearchProperties == SearchUserBy.Auto) {
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
					if (filter.SearchProperties == SearchUserBy.SearchAccount) {
						var id = 0u;
						UInt32.TryParse(filter.SearchText, out id);
						if (id > 0)
							return string.Format("where C.id = {0}", id);
					}
					if (filter.SearchProperties == SearchUserBy.ByFio) {
						return
							String.Format(@"
	WHERE (LOWER(C.Name) like {0} )", ":SearchText")
								+ _return;
					}
					if (filter.SearchProperties == SearchUserBy.TelNum) {
						return String.Format(@"WHERE (LOWER(co.Contact) like {0})", ":SearchText") + _return;
					}
					if (filter.SearchProperties == SearchUserBy.ByAddress) {
						return String.Format(@"
	WHERE (LOWER(h.Street) like {0} or
	LOWER(l.ActualAdress) like {0})", ":SearchText")
							+ _return;
					}
				}
			}
			else {
				var id = 0u;
				UInt32.TryParse(filter.SearchText, out id);
				if (id > 0) {
					return string.Format("WHERE (c.Id = {0}) and (C.PhysicalClient is not null)", id);
				}
				else if (!string.IsNullOrEmpty(filter.SearchText))
					return "WHERE (LOWER(C.Name) like :SearchText) and (C.PhysicalClient is not null)";
			}
			return string.IsNullOrEmpty(_return) ? string.Empty : string.Format("WHERE {0}", _return.Remove(0, 4));
		}
	}
}