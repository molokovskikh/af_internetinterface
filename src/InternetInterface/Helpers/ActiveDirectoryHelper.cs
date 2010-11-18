using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Web;

namespace InternetInterface.Helpers
{
	public class ActiveDirectoryHelper
	{
		static string _path;
		static string _filterAttribute;
		public static string ErrorMessage;

		public static bool IsAuthenticated(string username, string pwd)
		{
			string domainAndUsername = @"analit\" + username;
			DirectoryEntry entry = new DirectoryEntry("LDAP://OU=Офис,DC=adc,DC=analit,DC=net",
													   domainAndUsername,
														 pwd);
			try
			{
				// Bind to the native AdsObject to force authentication.
				Object obj = entry.NativeObject;
				DirectorySearcher search = new DirectorySearcher(entry);
				search.Filter = "(SAMAccountName=" + username + ")";
				search.PropertiesToLoad.Add("cn");
				SearchResult result = search.FindOne();
				if (null == result)
				{
					return false;
				}
				// Update the new path to the user in the directory
				_path = result.Path;
				_filterAttribute = (String)result.Properties["cn"][0];
			}
			catch (Exception ex)
			{
				ErrorMessage = ex.Message;
				return false;
			}
			return true;
		}

		public static void CreateUserInAD(string login, string password)
		{
#if !DEBUG
			var root = new DirectoryEntry("LDAP://acdcserv/OU=Пользователи,OU=Клиенты,DC=adc,DC=analit,DC=net");
			var userGroup = new DirectoryEntry("LDAP://acdcserv/CN=Базовая группа клиентов - получателей данных,OU=Группы,OU=Клиенты,DC=adc,DC=analit,DC=net");
			var user = root.Children.Add("CN=" + login, "user");
			user.Properties["samAccountName"].Value = login;
			user.Properties["userWorkstations"].Add("acdcserv,solo");
			//user.Properties["description"].Value = clientCode.ToString();
			user.CommitChanges();
			user.Invoke("SetPassword", password);
			user.Properties["userAccountControl"].Value = 66048;
			user.CommitChanges();
			userGroup.Invoke("Add", user.Path);
			userGroup.CommitChanges();
			root.CommitChanges();
#endif
		}

		public static void ChangePassword(string login, string password)
		{
#if !DEBUG
			var entry = GetDirectoryEntry(login);
			GetDirectoryEntry(login).Invoke("SetPassword", password);
			entry.CommitChanges();
#endif
		}
	}
}