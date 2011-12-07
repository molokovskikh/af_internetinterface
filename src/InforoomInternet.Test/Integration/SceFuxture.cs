using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Common.Web.Ui.Helpers;
using InforoomInternet.Models;
using InternetInterface.Models;
using NHibernate;
using NUnit.Framework;

namespace InforoomInternet.Test.Integration
{
	[TestFixture]
	public class SceFuxture
	{
		[Test]
		public void ThreadTest()
		{
			List<Lease> leases;
			using (new SessionScope()) {
				leases =
					Lease.Queryable.Where(
						l => l.Endpoint != null && l.Endpoint.Client != null && l.Endpoint.Client.PhysicalClient != null).ToList();
				foreach (var lease in leases) {
					InitializeHelper.InithializeAllStructure(lease);
					new SceThread(lease, "192.168.0.1").Go();
				}
				var firstClient = leases.First().Endpoint.Client.Id;
				var assertStatus = UnknownClientStatus.InProcess;
				UnknownClientStatus firstClientStatus;
				//Ждем, чтобы отработала инициализирующая очистка
				Thread.Sleep(50);
				do {
					firstClientStatus = ClientData.Get(firstClient);
					if (firstClientStatus == UnknownClientStatus.Connected) {
						assertStatus = UnknownClientStatus.Connected;
						//Ждем когда все станут Connected и проверяем
						Thread.Sleep(3000);
					}
					Console.WriteLine(assertStatus);
					foreach (var lease in leases.Select(l => l.Endpoint.Client.Id)) {
						Assert.That(ClientData.Get(lease), Is.EqualTo(assertStatus));
					}
				} while (firstClientStatus != UnknownClientStatus.Connected);
			}
		}
	}
}