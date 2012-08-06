using System;
using System.Linq;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class CommutatorFixture : global::Test.Support.Web.WatinFixture2
	{
		[Test]
		public void Delete_commutator()
		{
			var commutator = new NetworkSwitches("Тестовый коммутатор", session.Query<Zone>().First());
			Save(commutator);
			Open("Switches/MakeSwitch?Switch={0}", commutator.Id);
			AssertText("Редактирование коммутатора");
			Click("Удалить");
			AssertText("Удалено");
			Console.WriteLine();
			session.Clear();
			commutator = session.Get<NetworkSwitches>(commutator.Id);
			Assert.That(commutator, Is.Null);
		}
	}
}