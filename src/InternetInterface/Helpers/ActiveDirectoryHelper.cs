using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Web;
using log4net;

namespace InternetInterface.Helpers
{
	[Serializable]
	public class ActiveDirectoryHelper
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(ActiveDirectoryHelper));

		private static DirectoryEntry entryAu;
		private static string _path;
		private static string _filterAttribute;
		public static string ErrorMessage;

		public static bool IsAuthenticated(string username, string pwd)
		{
			if (Authenticated(@"LDAP://OU=Офис,DC=adc,DC=analit,DC=net", username, pwd))
				return true;
			if (Authenticated(@"LDAP://OU=Клиенты,DC=adc,DC=analit,DC=net", username, pwd))
				return true;
			return false;
		}

		public static bool Authenticated(string ldap, string username, string pwd)
		{
			var domainAndUsername = @"analit\" + username;
			entryAu = new DirectoryEntry(ldap, domainAndUsername, pwd, AuthenticationTypes.None);
			try {
				// Bind to the native AdsObject to force authentication.
				var obj = entryAu.NativeObject;
				var search = new DirectorySearcher(entryAu);
				search.Filter = "(SAMAccountName=" + username + ")";
				search.PropertiesToLoad.Add("cn");
				SearchResult result = search.FindOne();
				// Update the new path to the user in the directory
				_path = result.Path;
				_filterAttribute = (String)result.Properties["cn"][0];
			}
			catch (Exception ex) {
				_log.Info("Пароль или логин был введен неправильно");
				_log.Info(ErrorMessage);
				ErrorMessage = ex.Message;
				return false;
			}
			entryAu.RefreshCache();
			return true;
		}
	}
}