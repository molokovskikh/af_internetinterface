using System;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using WatinFixture2 = Test.Support.Web.WatinFixture2;

namespace InternetInterface.Test.Functional
{
	public class RequestGraphFixture : WatinFixture2
	{
		[Test]
		public void View_client_in_request_graph()
		{
			var brigad = session.Query<Brigad>().First();
			var client = ClientHelper.Client();
			var connect = new ConnectGraph(client, DateTime.Today, brigad);

			Save(brigad, client, connect);

			Open("UserInfo/RequestGraph.rails");
			AssertText("Настройки");
		}
	}
}