using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Selenium.Test.Fixtures
{
	public class AcceptanceFixture : SeleniumFixture
	{
		public PhysicalClient physicalClient;
		public Client client;
		public ClientEndpoint endpoint;

		[SetUp]
		public void FixtureSetup()
		{
			client = ClientHelper.Client();
			physicalClient = client.PhysicalClient;

			session.Save(client);
			endpoint = new ClientEndpoint {
				Client = client,
			};
			session.Save(endpoint);
			defaultUrl = string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}&filter.EditingConnect=true&filter.Editing={1}", client.Id, false);
		}
	}
}
