using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;

namespace InternetInterface.Test.Helpers
{
	class UserSearchPropertiesHelper : UserSearchProperties
	{
		public static UserSearchProperties CreateUserSearchProperties()
		{
			return new UserSearchProperties
			       	{
			       		SearchText = string.Empty,
			       		SearchBy = SearchUserBy.Auto
			       	};
		}
	}

	class ChangeBalacePropertiesHelper : ChangeBalaceProperties
	{
		public static ChangeBalaceProperties CreateUserSearchProperties()
		{
			return new ChangeBalaceProperties
			{
				ChangeType = TypeChangeBalance.ForTariff
			};
		}
	}

	class ConnectedTypePropertiesHelper : ConnectedTypeProperties
	{
		public static ConnectedTypeProperties CreateUserSearchProperties()
		{
			return new ConnectedTypeProperties
			{
				Type = ConnectedType.AllConnected
			};
		}
	}
}
