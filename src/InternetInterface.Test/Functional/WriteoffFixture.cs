using System;
using System.Linq;
using Headless;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Rhino.Mocks;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class WriteoffFixture : HeadlessFixture
	{
		[Test]
		public void View_write_off()
		{
			Open();
			Click("Списания");
			AssertText("Имя клиента");
		}

		[Test]
		public void View_connections()
		{
			var client = ClientHelper.Client(session);
			session.Save(client);
			session.Save(new ConnectGraph(client, DateTime.Today, session.Query<Brigad>().First()));

			Open();
			Click("Подключения");
			AssertText(client.Name);
		}
	}
}