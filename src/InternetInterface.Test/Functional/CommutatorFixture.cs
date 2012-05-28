using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class CommutatorFixture : global::Test.Support.Web.WatinFixture2
	{
		[Test]
		public void Delete_commutator()
		{
			var zone = new Zone {
				Name = "Тестовая зона"
			};
			var commutator = new NetworkSwitches {
				Name = "Тестовый коммутатор",
				Zone = zone,
			};
			Save(zone, commutator);
			Open("Switches/MakeSwitch?Switch={0}", commutator.Id);
			AssertText("Редактирование коммутатора");
			Click("Удалить");
			AssertText("Удалено");

			session.Clear();
			commutator = session.Get<NetworkSwitches>(commutator.Id);
			Assert.That(commutator, Is.Null);
		}
	}
}