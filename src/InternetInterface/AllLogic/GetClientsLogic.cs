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
        public static string GetWhere(UserSearchProperties sp, ConnectedTypeProperties ct, ClientTypeProperties clT, uint whoregister, uint tariff, string searchText, uint brigad, uint addtionalStatus)
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
            if (addtionalStatus != 0)
            {
                _return += " and C.AdditionalStatus = :addtionalStatus";
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
WHERE LOWER(C.Name) like {0} or C.id like :SearchText",
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