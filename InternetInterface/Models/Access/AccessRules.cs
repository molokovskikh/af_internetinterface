using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InternetInterface.Models.Access
{
	public class AccessRules
	{
		private HashSet<string> _getClientInfo;
		private HashSet<string> _registerClient;
		private HashSet<string> _sendDemand;
		private HashSet<string> _closeDemand;
		private HashSet<string> _registerPartner;
		private HashSet<string> _changeBalance;

		/*public string GetRulesName_getClientInfo()
		{
			_getClientInfo = new HashSet<string>
			                 	{
									""
			                 	};
		}

		public string GetRulesName_registerClient()
		{
			_registerClient = new HashSet<string>
			                 	{
									"RegisterClient",
									""
			                 	};
		}

		public string GetRulesName_sendDemand()
		{
			_sendDemand = new HashSet<string>
			                 	{
									"CreateDemandConnect"
			                 	};
		}

		public string GetRulesName_closeDemand()
		{
			_closeDemand = new HashSet<string>
			                 	{
									""
			                 	};
		}

		public string GetRulesName_registerPartner()
		{
			_registerPartner = new HashSet<string>
			                 	{
									"RegisterPartner",
									""
			                 	};
		}

		public string GetRulesName_changeBalance()
		{
			_changeBalance = new HashSet<string>
			                 	{
									""
			                 	};
		}*/
	}
}