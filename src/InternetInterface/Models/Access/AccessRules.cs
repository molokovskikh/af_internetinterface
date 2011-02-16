﻿using System;
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
		private static HashSet<string> _manageBrigads;
		//private static HashSet<string> _closeDemand;
		private static HashSet<string> _registerPartner;
		private static HashSet<string> _changeBalance;
		private static HashSet<string> _accessDHCP;
		private static HashSet<string> _visibleDemand;

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
			if (GetRulesName_manageBrigads() != string.Empty)
			{
				result.Add(GetRulesName_manageBrigads());
			}
			/*if (GetRulesName_closeDemand() != string.Empty)
			{
				result.Add(GetRulesName_closeDemand());
			}*/
			if (GetRulesName_registerPartner() != string.Empty)
			{
				result.Add(GetRulesName_registerPartner());
			}
			if (GetRulesName_changeBalance() != string.Empty)
			{
				result.Add(GetRulesName_changeBalance());
			}
			if (GetRulesName_accessDHCP() != string.Empty)
			{
				result.Add(GetRulesName_accessDHCP());
			}
			if (GetRulesName_visibleDemand() != string.Empty)
			{
				result.Add(GetRulesName_visibleDemand());
			}
			if (GetRulesName_showSecretInfo() != string.Empty)
			{
				result.Add(GetRulesName_showSecretInfo());
			}
			if (GetRulesName_editClientInfo() != string.Empty)
			{
				result.Add(GetRulesName_editClientInfo());
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
									"SearchUserInfo",
			                 	};
			return _getClientInfo.Contains(methodName) ? "GCI" : string.Empty;
		}

		private static string GetRulesName_registerClient()
		{
			_registerClient = new HashSet<string>
			                 	{
									"RegisterClient",
									"SiteMap",
									"ClientRegisteredInfoFromDiller"
			                 	};
			return _registerClient.Contains(methodName) ? "RC" : string.Empty;
		}

		private static string GetRulesName_manageBrigads()
		{
			_manageBrigads = new HashSet<string>
			                 	{
									"MakeBrigad",
									"RegisterBrigad",
									"EditBrigad"
			                 	};
			return _manageBrigads.Contains(methodName) ? "MB" : string.Empty;
		}

		/*private static string GetRulesName_closeDemand()
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
		}*/

		private static string GetRulesName_registerPartner()
		{
			_registerPartner = new HashSet<string>
			                 	{
									"RegisterPartner",
									"SiteMap",
									"PartnerRegisteredInfo",
									"PartnersPreview",
									"EditPartner",
									"RegisterPartnerI"
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

		private static string GetRulesName_accessDHCP()
		{
			_accessDHCP = new HashSet<string>
			                 	{
									"ShowSwitches",
									"MakeSwitch",
									"EditSwitch",
									"RegisterSwitch",
									"OnLineClient",
									"SaveSwitchForClient",
									"LoadEditConnectMudule",
									"GoZone",
									"SiteMap"
			                 	};
			return _accessDHCP.Contains(methodName) ? "DHCP" : string.Empty;
		}

		private static string GetRulesName_visibleDemand()
		{
			_visibleDemand = new HashSet<string>
			                 	{
									"RequestView",
									"ChangeLabel",
									"EditLabel",
									"DeleteLabel",
									"SiteMap"
			                 	};
			return _visibleDemand.Contains(methodName) ? "VD" : string.Empty;
		}

		private static string GetRulesName_showSecretInfo()
		{
			_visibleDemand = new HashSet<string>
			                 	{
									"ClientRegisteredInfo",
									"ShowBrigad",
									"PassAndShowCard"
			                 	};
			return _visibleDemand.Contains(methodName) ? "SSI" : string.Empty;
		}

		private static string GetRulesName_editClientInfo()
		{
			_visibleDemand = new HashSet<string>
			                 	{
									"EditInformation",
									"LoadEditMudule",
									"SiteMap",
			                 	};
			return _visibleDemand.Contains(methodName) ? "ECI" : string.Empty;
		}
	}
}