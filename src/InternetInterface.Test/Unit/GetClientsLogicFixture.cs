using InternetInterface.AllLogic;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture, Ignore("Чинить")]
	public class GetClientsLogicFixture
	{
		[Test]
		public void GetWhereTest()
		{
			var query = GetClientsLogic.GetWhere(UserSearchPropertiesHelper.CreateUserSearchProperties(),
			                                     0, ClientTypeHelper.CreateUserSearchProperties(), new EnabledTypeProperties(), 
			                                      "Test");
			Assert.That(query, Is.StringContaining(@"WHERE LOWER(P.Name) like :SearchText or LOWER(P.Surname) like :SearchText
or LOWER(P.Patronymic) like :SearchText or LOWER(P.City) like :SearchText 
or LOWER(P.PassportSeries) like :SearchText
or LOWER(P.PassportNumber) like :SearchText or LOWER(P.WhoGivePassport) like :SearchText
or LOWER(P.RegistrationAdress) like :SearchText"));
		}
	}
}
