using InternetInterface.Models;

namespace InternetInterface.Test.Helpers
{
	internal class UserSearchPropertiesHelper : UserSearchProperties
	{
		public static UserSearchProperties CreateUserSearchProperties()
		{
			return new UserSearchProperties {
				SearchText = string.Empty,
				SearchBy = SearchUserBy.Auto
			};
		}
	}

	internal class ConnectedTypePropertiesHelper : ConnectedTypeProperties
	{
		public static ConnectedTypeProperties CreateUserSearchProperties()
		{
			return new ConnectedTypeProperties {
				Type = ConnectedType.AllConnected
			};
		}
	}

	internal class ClientTypeHelper : ClientTypeProperties
	{
		public static ClientTypeProperties CreateUserSearchProperties()
		{
			return new ClientTypeProperties {
				Type = ForSearchClientType.AllClients
			};
		}
	}
}