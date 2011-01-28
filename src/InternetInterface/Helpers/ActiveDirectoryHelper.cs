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

		static DirectoryEntry entryAu;
		static string _path;
		static string _filterAttribute;
		public static string ErrorMessage;

		//"LDAP://OU=Клиенты,DC=adc,DC=analit,DC=net"

		public static bool IsAuthenticated(string username, string pwd)
		{
			if (Authenticated(@"LDAP://OU=Офис,DC=adc,DC=analit,DC=net", username, pwd))
				return true;
			if (Authenticated(@"LDAP://OU=Клиенты,DC=adc,DC=analit,DC=net", username, pwd))
				return true;
			return false;
		}

		public static bool Authenticated(string LDAP, string username, string pwd)
		{ 

			var domainAndUsername = @"analit\" + username;
			entryAu = new DirectoryEntry(LDAP, domainAndUsername, pwd, AuthenticationTypes.None);
#if DEBUG
			//Console.WriteLine(entryAu.Guid);
#endif
			try
			{
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
			catch (Exception ex)
			{
				_log.Info("Пароль или логин был введен неправильно");
				ErrorMessage = ex.Message;
				return false;
			}
			entryAu.RefreshCache();
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

		public static DirectoryEntry FindDirectoryEntry(string login)
		{
			using (var searcher = new DirectorySearcher(String.Format(@"(&(objectClass=user)(sAMAccountName={0}))", login)))
			{
				var searchResult = searcher.FindOne();
				if (searchResult != null)
					return searcher.FindOne().GetDirectoryEntry();
				return null;
			}
		}

		private static DirectoryEntry GetDirectoryEntry(string login)
		{
			var entry = FindDirectoryEntry(login);
			if (entry == null)
				throw new Exception(String.Format("Учетная запись Active Directory {0} не найдена", login));
			return entry;
		}

		public static void ChangePassword(string login, string password)
		{
#if DEBUG
			var entry = GetDirectoryEntry(login);
			entry.Invoke("ChangePassword", "TqKvdG46", password);
			//GetDirectoryEntry(login).Invoke("SetPassword", password);
			entry.CommitChanges();
#endif
		}
	}
}