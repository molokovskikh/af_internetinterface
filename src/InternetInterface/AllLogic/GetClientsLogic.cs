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

				if (filter.SearchProperties != SearchUserBy.Address) {
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
						if (filter.SearchProperties == SearchUserBy.ByPassport) {
							return String.Format(@"
	WHERE (LOWER(p.PassportSeries) like {0} or LOWER(p.PassportNumber)  like {0} or
	LOWER(l.ActualAdress) like {0})", ":SearchText")
								+ _return;
						}
					}
				}
				else {
					var where = "where";
					var whereCount = 0;
					if (!string.IsNullOrEmpty(filter.City)) {
						where += "(LOWER(p.City) like :City or LOWER(l.ActualAdress) like :City)";
						whereCount++;
					}
					if (!string.IsNullOrEmpty(filter.Street)) {
						if (whereCount > 0)
							where += " and ";
						where += "(LOWER(h.Street) like :Street or LOWER(l.ActualAdress) like :Street)";
						whereCount++;
					}
					if (!string.IsNullOrEmpty(filter.House)) {
						if (whereCount > 0)
							where += " and ";
						where += "(p.House = :House)";
						whereCount++;
					}
					if (!string.IsNullOrEmpty(filter.CaseHouse)) {
						if (whereCount > 0)
							where += " and ";
						where += "(LOWER(p.CaseHouse) like :CaseHouse or LOWER(l.ActualAdress) like :CaseHouse)";
						whereCount++;
					}
					if (!string.IsNullOrEmpty(filter.Apartment)) {
						if (whereCount > 0)
							where += " and ";
						where += "(p.Apartment = :Apartment)";
						whereCount++;
					}

					if (whereCount == 0)
						where += "(1 = 1)";

					return where + _return;
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