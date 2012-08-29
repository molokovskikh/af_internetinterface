using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support.Web;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class ClientFunctionalFixture : WatinFixture2
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