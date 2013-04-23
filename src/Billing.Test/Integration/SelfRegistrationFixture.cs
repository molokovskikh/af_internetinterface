using System.Linq;
using Common.Tools;
using InternetInterface.Controllers;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class SelfRegistrationFixture : MainBillingFixture
	{
		[Test]
		public void Write_off_from_self_regsitred_client()
		{
			InitSession();

			var settings = new Settings(session);
			var physicalClient = new PhysicalClient {
				Apartment = 0,
				Entrance = 0,
				Floor = 0,
			};

			var lease = new Lease {
				Switch = new NetworkSwitch()
			};
			physicalClient.ExternalClientIdRequired = true;
			physicalClient.ExternalClientId = Generator.Random().First();
			physicalClient.Tariff = session.Query<Tariff>().First(t => t.Price > 0);
			physicalClient.Name = "Иван";
			physicalClient.Surname = "Иванов";
			physicalClient.Patronymic = "Иванович";
			var client = new Client(physicalClient, settings);
			client.SelfRegistration(lease, Status.Get(StatusType.Worked, session));

			foreach (var contact in client.Contacts)
				session.Save(contact);

			foreach (var payment in client.Payments)
				session.Save(payment);

			session.Save(client);

			billing.OnMethod();
			billing.Compute();

			session.Refresh(client);
			Assert.That(client.WriteOffs.Count, Is.EqualTo(1));
		}
	}
}