using System;
using System.Linq;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional.Selenium
{
	[TestFixture]
	public class CommutatorFixture : SeleniumFixture
	{
		[Test, Ignore("Не работает на сервере")]
		public void Delete_commutator()
		{
			var commutator = new NetworkSwitch("Тестовый коммутатор", session.Query<Zone>().First());
			Save(commutator);
			Open("Switches/MakeSwitch?Switch={0}", commutator.Id);
			AssertText("Редактирование коммутатора");
			Click("Удалить");
			AssertText("Удалено");
			session.Clear();
			commutator = session.Get<NetworkSwitch>(commutator.Id);
			Assert.That(commutator, Is.Null);
		}
	}
}
