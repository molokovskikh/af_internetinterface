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
		public static string GetWhere(
			UserSearchProperties searchProperty,
			uint statusType,
			ClientTypeProperties clientTypeProperty,
			EnabledTypeProperties enabledTypeProperty,
			string searchText)
		{
			var _return = string.Empty;
			if (statusType > 0) {
				_return += " and S.Id = :statusType";
			}
			if (clientTypeProperty.IsPhysical()) {
				_return += " and C.PhysicalClient is not null";
			}
			if (clientTypeProperty.IsLawyer()) {
				_return += " and C.LawyerPerson is not null";
			}
			if (enabledTypeProperty.IsDisabled())
				_return += " and c.Disabled";
			if (enabledTypeProperty.IsEnabled())
				_return += " and c.Disabled = false";
			if (searchText != null) {
				if (!InitializeContent.Partner.IsDiller()) {
					if (searchProperty.IsSearchAuto()) {
						return
							String.Format(
								@"
	WHERE
	LOWER(C.Name) like {0} or
	C.id like {0} or
	LOWER(co.Contact) like {0} or
	LOWER(h.Street) like {0} or
	LOWER(l.ActualAdress) like {0} " ,
								":SearchText") + _return;
					}
					if (searchProperty.IsSearchByFio()) {
						return
							String.Format(@"
	WHERE LOWER(C.Name) like {0} ", ":SearchText") + _return;
					}
					if (searchProperty.IsSearchTelephone()) {
						return String.Format(@"WHERE LOWER(co.Contact) like {0} ", ":SearchText") + _return;
					}
					if (searchProperty.IsSearchByAddress()) {
						return String.Format(@"
	WHERE LOWER(h.Street) like {0} or
	LOWER(l.ActualAdress) like {0}", ":SearchText") + _return;
					}
				}
				else {
					var id = 0u;
					UInt32.TryParse(searchText, out id);
					if (id > 0) {
						return "WHERE c.Id = " + id;
					}
					else {
						return "LOWER(C.Name) like :SearchText";
					}
				}
			}
			else {
				if (_return != string.Empty)
					return "WHERE" + _return.Remove(0, 4);
				return _return;
			}
			return string.Empty;
		}
	}
}