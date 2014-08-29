using System;
using InternetInterface.Models;
using NHibernate.Exceptions;
using NUnit.Framework;
using Test.Support;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class SwitchesFixture : IntegrationFixture
	{
		[Test]
		public void Check_delete_with_sended_leases()
		{
			var zone = new Zone("Зона 51");
			session.Save(zone);
			var testingSwich = new NetworkSwitch("Контроллер для теста удаления", zone) {
				PortCount = 8
			};
			session.Save(testingSwich);
			var sendedLease = new SendedLease() {
				Switch = testingSwich,
				Port = 8
			};
			session.Save(sendedLease);
			session.Delete(testingSwich);
		}
	}
}