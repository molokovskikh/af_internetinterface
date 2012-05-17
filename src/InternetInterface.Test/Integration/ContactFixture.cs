using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support;
using ContactType = InternetInterface.Models.ContactType;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class ContactFixture : IntegrationFixture
	{
		[Test]
		public void Log_changes()
		{
			var client = ClientHelper.Client();
			Save(client);

			var contact = new Contact(client, ContactType.MobilePhone, "111-1111111");
			Save(contact);

			Reopen();

			contact = session.Load<Contact>(contact.Id);
			contact.Type = ContactType.HousePhone;

			Save(contact);
		}
	}
}