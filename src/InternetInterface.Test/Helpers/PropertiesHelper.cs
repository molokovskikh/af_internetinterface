using InternetInterface.Models;

namespace InternetInterface.Test.Helpers
{
	class UserSearchPropertiesHelper : UserSearchProperties
	{
		public static UserSearchProperties CreateUserSearchProperties()
		{
			return new UserSearchProperties {
				SearchText = string.Empty,
				SearchBy = SearchUserBy.Auto
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

	class ClientTypeHelper : ClientTypeProperties
	{
		public static ClientTypeProperties CreateUserSearchProperties()
		{
			return new ClientTypeProperties
			{
				Type = ForSearchClientType.AllClients
			};
		}
	}
}
