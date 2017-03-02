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
			session.BeginTransaction();
			var brigad = Brigad.All(session).First();
			var client = ClientHelper.Client(session);
			var connect = new ConnectGraph(client, DateTime.Today.AddHours(10), brigad);
			session.Save(brigad);
			session.Save(client);
			session.Save(connect);
			FlushAndCommit();
			var brigad2 = Brigad.All(session).First();
			var client2 = ClientHelper.Client(session);
			var connect2 = new ConnectGraph(client2, DateTime.Today.AddHours(15), brigad2);
			session.Save(brigad2);
			session.Save(client2);
			session.Save(connect2);
			Open("UserInfo/RequestGraph");

			Click("Сформировать и распечатать заявки");

			AssertText(client.Id.ToString());
			AssertText(client2.Id.ToString());

			Click(client.Id.ToString());


			AssertText("Информация по клиенту");
		}
	}
}
