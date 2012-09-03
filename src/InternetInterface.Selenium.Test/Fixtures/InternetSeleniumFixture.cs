using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Selenium.Test
{
	public class InternetSeleniumFixture : SeleniumFixture
	{
		public string ClientUrl;
		public PhysicalClient PhysicalClient;
		public Client Client;
		public ClientEndpoint EndPoint;
		protected bool Editing;

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
			ClientUrl = string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}&filter.EditingConnect=true&filter.Editing={1}", Client.Id, Editing);
		}
	}
}
