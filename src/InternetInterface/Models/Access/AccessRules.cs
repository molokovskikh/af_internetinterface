using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetInterface.Models.Access
{
	public class AccessRules
	{
		private static string methodName;
		private static HashSet<string> _getClientInfo;
		private static HashSet<string> _registerClient;
		private static HashSet<string> _sendDemand;
		private static HashSet<string> _closeDemand;
		private static HashSet<string> _registerPartner;
		private static HashSet<string> _changeBalance;

		public static List<string> GetAccessName(string _methodName)
		{
			var result = new List<string>();
			methodName = _methodName;

			if (GetRulesName_getClientInfo() != string.Empty)
			{
				result.Add(GetRulesName_getClientInfo());
			}
			if (GetRulesName_registerClient() != string.Empty)
			{
				result.Add(GetRulesName_registerClient());
			}
			if (GetRulesName_sendDemand() != string.Empty)
			{
				result.Add(GetRulesName_sendDemand());
			}
			if (GetRulesName_closeDemand() != string.Empty)
			{
				result.Add(GetRulesName_closeDemand());
			}
			if (GetRulesName_registerPartner() != string.Empty)
			{
				result.Add(GetRulesName_registerPartner());
			}
			if (GetRulesName_changeBalance() != string.Empty)
			{
				result.Add(GetRulesName_changeBalance());
			}
			return result;
		}

		private static string GetRulesName_getClientInfo()
		{
			_getClientInfo = new HashSet<string>
			                 	{
									"GetClients",
									"SearchUsers",
									"SearchBy",
									"SiteMap",
									"SearchUserInfo"
			                 	};
			return _getClientInfo.Contains(methodName) ? "GCI" : string.Empty;
		}

		private static string GetRulesName_registerClient()
		{
			_registerClient = new HashSet<string>
			                 	{
									"RegisterClient",
									"SiteMap",
									"LoadEditMudule",
									"EditInformation"
			                 	};
			return _registerClient.Contains(methodName) ? "RC" : string.Empty;
		}

		private static string GetRulesName_sendDemand()
		{
			_sendDemand = new HashSet<string>
			                 	{
									"CreateDemandConnect",
									"SiteMap"
			                 	};
			return _sendDemand.Contains(methodName) ? "SD" : string.Empty;
		}

		private static string GetRulesName_closeDemand()
		{
			_closeDemand = new HashSet<string>
			                 	{
									"GetClientsForCloseDemand",
									"CloseDemand",
									"SearchBy",
									"SiteMap",
									"SearchUserInfo"
			                 	};
			return _closeDemand.Contains(methodName) ? "CD" : string.Empty;
		}

		private static string GetRulesName_registerPartner()
		{
			_registerPartner = new HashSet<string>
			                 	{
									"RegisterPartner",
									"SiteMap"
			                 	};
			return _registerPartner.Contains(methodName) ? "RP" : string.Empty;
		}

		private static string GetRulesName_changeBalance()
		{
			_changeBalance = new HashSet<string>
			                 	{
									"ChangeBalance",
									"SiteMap",
									"SearchUserInfo"
			                 	};
			return _changeBalance.Contains(methodName) ? "CB" : string.Empty;
		}
	}
}