using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class ClientFunctionalFixture : SeleniumFixture
	{
		public string ClientUrl;
		public PhysicalClient PhysicalClient;
		public Client Client;
		public ClientEndpoint EndPoint;

		[SetUp]
		public void FixtureSetup()
		{
			Client = ClientHelper.Client();
			PhysicalClient = Client.PhysicalClient;
			session.Save(Client);
			EndPoint = new ClientEndpoint {
				Client = Client,
			};
			session.Save(EndPoint);
			ClientUrl = string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}&filter.EditingConnect=true&filter.Editing=true", Client.Id);
		}
	}
}
