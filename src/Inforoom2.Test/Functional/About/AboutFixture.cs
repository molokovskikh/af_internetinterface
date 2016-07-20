using System.Linq;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.Faq
{
	[TestFixture, Ignore]
	public class AboutFixture : BaseFixture
	{
		[Test, Description("Проверка Подключенных домов")]
		public void ConnectedHousesTest()
		{
			ConnectedHouse.SynchronizeConnections(DbSession);

			SetCookie("userCity", null);
			Open();
			AssertText("ВЫБЕРИТЕ ГОРОД");
			var links = browser.FindElementsByCssSelector("#CityWindow .cities a");
			links[1].Click();
			var userCity = GetCookie("userCity");
			Assert.That(userCity, Is.EqualTo("Борисоглебск"));
			var ConnectedHousesUpdate = DbSession.Query<ConnectedHouse>().First(s => s.Region.Name == userCity);
			Open("About/ConnectedHousesLists");

			AssertText(ConnectedHousesUpdate.Street.Name);
			AssertText(ConnectedHousesUpdate.Number);
		}
	}
}