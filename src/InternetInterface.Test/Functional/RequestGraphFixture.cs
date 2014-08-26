using System;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	class RequestGraphFixture : SeleniumFixture
	{
		[Test]
		public void Redirect_from_create_and_print_graph()
		{
			var brigad = Brigad.All().First();
			var client = ClientHelper.Client(session);
			var connect = new ConnectGraph(client, DateTime.Today, brigad);
			Save(brigad, client, connect);
			Open("UserInfo/RequestGraph");

			Click("Сформировать и распечатать заявки");
			Click(client.Id.ToString());


			AssertText("Информация по клиенту");
		}
	}
}
