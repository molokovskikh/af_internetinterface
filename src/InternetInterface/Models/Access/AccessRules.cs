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
		private static HashSet<string> _manageBrigads;
		//private static HashSet<string> _closeDemand;
		private static HashSet<string> _registerPartner;
		private static HashSet<string> _changeBalance;
		private static HashSet<string> _accessDHCP;
		private static HashSet<string> _visibleDemand;
		private static HashSet<string> _houseMapUse;
		private static HashSet<string> _agentInfo;
		private static HashSet<string> _agentPayers; 
		private static HashSet<string> _serviceRequest; 
		private static HashSet<string> _adminServiceRequest; 

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
			if (GetRulesName_houseMapUse() != string.Empty)
			{
				result.Add(GetRulesName_houseMapUse());
			}
			if (GetRulesName_agentInfo() != string.Empty)
			{
				result.Add(GetRulesName_agentInfo());
			}
			if (GetRulesName_agentPayers() != string.Empty)
			{
				result.Add(GetRulesName_agentPayers());
			}
			if (GetRulesName_serviceRequest() != string.Empty)
			{
				result.Add(GetRulesName_serviceRequest());
			}
			if (GetRulesName_adminServiceRequest() != string.Empty) {
				result.Add(GetRulesName_adminServiceRequest());
			}
			return result;
		}

		public static string GetRulesName_agentPayers()
		{
			_agentPayers = new HashSet<string> {
													"AgentFilter",
													"ShowAgent",
											   };
			return _agentPayers.Contains(methodName) ? "PFA" : string.Empty;
		}

		private static string GetRulesName_agentInfo()
		{
			_agentInfo = new HashSet<string> {
												 "SummaryInformation"
											 };
			return _agentInfo.Contains(methodName) ? "AIV" : string.Empty;
		}

		private static string GetRulesName_houseMapUse()
		{
			_houseMapUse = new HashSet<string> {
												   "SiteMap",
												   "Register",
												   "EditHouse",
												   "FindHouse",
												   "HouseFindResult",
												   "SaveApartment",
												   "HouseEdit",
												   "V_Prohod",
												   "Agent",
												   "RegisterHouseAgent",
												   "EditHouseAgent",
												   "ForPrintToAgent",
												   "RegisterHouse",
												   "ViewHouseInfo",
												   "BasicHouseInfo",
												   "NetworkSwitches",
												   "SaveHouseMap",
												   "GetCompetitorCount",
												   "LoadApartmentHistory",
												   "RegisterRequest",
												   "GetApartment"
											   };
			return _houseMapUse.Contains(methodName) ? "HMA" : string.Empty;
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
									"Redirect",
									"LawyerPersonInfo",
								};
			return _getClientInfo.Contains(methodName) ? "GCI" : string.Empty;
		}

		private static string GetRulesName_registerClient()
		{
			_registerClient = new HashSet<string>
								{
									"RegisterClient",
									"SiteMap",
									"ClientRegisteredInfoFromDiller",
									"RegisterLegalPerson"
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


		private static string GetRulesName_registerPartner()
		{
			_registerPartner = new HashSet<string>
								{
									"RegisterPartner",
									"SiteMap",
									"PartnerRegisteredInfo",
									"PartnersPreview",
									"EditPartner",
									"RegisterPartnerI",
									"Administration"
								};
			return _registerPartner.Contains(methodName) ? "RP" : string.Empty;
		}

		private static string GetRulesName_changeBalance()
		{
			_changeBalance = new HashSet<string>
								{
									"ChangeBalance",
									"SiteMap",
									"SearchUserInfo",
									"NotifyInforum"
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
									"SiteMap",
									"GetSubnet",
									"GetStaticIp",
									"FreePortForSwitch",
									"AddEndPoint",
									"DeleteEndPoint",
									"AddPoint",
									"PortInfo"
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
									"SiteMap",
									"CreateLabel",
									"SetLabel",
									"RequestInArchive",
									"RequestOne",
									"CreateRequestComment"
								};
			return _visibleDemand.Contains(methodName) ? "VD" : string.Empty;
		}

		private static string GetRulesName_showSecretInfo()
		{
			_visibleDemand = new HashSet<string>
								{
									"ClientRegisteredInfo",
									"ShowBrigad",
									"PassAndShowCard",
									"CreateAppeal",
									"Filter",
									"Show",
									"AgentFilter",
									"ShowAgent",
									"ShowAgents",
									"GroupInfo",
									"Leases",
									 "ActivateService",
									"DiactivateService",
									"ProcessPayments",
									"NewPaymets",
									"Index",
									"New",
									"SavePayments",
									"EditTemp",
									"Delete",
									"DeleteTemp",
									"Edit",
									"SearchPayer",
									"CancelPayments",
									"ShowAppeals"
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
									"EditLawyerPerson",
									"AddInfo",
									"Refused",
									"NoPhoned",
									"AppointedToTheGraph",
									"GetGraph",
									"ReservGraph",
									"SaveGraph",
									"RequestGraph",
									"CreateAndPrintGraph",
									"UserWriteOff",
									"BindPhone",
									"LoadContactEditModule",
									"SaveContacts",
									"DeleteContact"
								};
			return _visibleDemand.Contains(methodName) ? "ECI" : string.Empty;
		}

		private static string GetRulesName_serviceRequest()
		{
			_serviceRequest = new HashSet<string> {
				"SiteMap",
				"ViewRequests",
				"ShowRequest",
				"AddIteration",
				"EditServiceRequest",
				"AddServiceComment"
			};
			return _serviceRequest.Contains(methodName) ? "SR" : string.Empty;
		}

		private static string GetRulesName_adminServiceRequest()
		{
			_adminServiceRequest = new HashSet<string> {
				"RegisterServiceRequest",
				"ShowRequest",
				"AddIteration",
				"EditServiceRequest",
				"AddServiceComment"
			};
			return _adminServiceRequest.Contains(methodName) ? "ASR" : string.Empty;
		}
	}
}