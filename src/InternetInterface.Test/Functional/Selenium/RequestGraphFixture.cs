using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional.Selenium
{
	[TestFixture]
	class RequestGraphFixture : SeleniumFixture
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
