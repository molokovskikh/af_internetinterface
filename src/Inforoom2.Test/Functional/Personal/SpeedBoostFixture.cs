using System;
using System.Configuration;
using System.Linq;
using Common.Tools;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.Personal
{
	internal class SpeedBoostFixture : PersonalFixture
	{ 

		[Test, Description("Тест услуги 'повышения скорости'")]
		public void SpeedBoostTestOff()
		{
			var client = Client;
			var tariffSpeed = client.PhysicalClient.Plan.PackageSpeed.PackageId;
			var boostedSpeed = NullableConvert.ToInt32(ConfigurationManager.AppSettings["SpeedBoostPackageId"]);
			var boostService = DbSession.Query<Service>().FirstOrDefault(s => s.Name == "Увеличить скорость");
			// Скорость не должна быть одинаковой 
			Assert.IsTrue(tariffSpeed != boostedSpeed);
			const int countDays = 10;
			var cServive = new ClientService {
				Client = client,
				BeginDate = SystemTime.Now().AddDays(-1),
				EndDate = SystemTime.Now().AddDays(1),
				Service = boostService
			};
			client.ClientServices.Add(cServive);
			DbSession.Save(client);
			DbSession.Flush();
			GetBilling().ProcessPayments();
			DbSession.Refresh(client);
			bool allEndpointsOk = !client.Endpoints.Any(e => e.PackageId != boostedSpeed && !e.Disabled);
			Assert.IsTrue(allEndpointsOk, "не все свичи переведены на повышенную скорость");

			cServive.EndDate = SystemTime.Now().AddDays(-1);
			DbSession.Save(cServive);
			DbSession.Flush();
			GetBilling().ProcessPayments();
			DbSession.Flush();
			client = DbSession.Query<Client>().First(i => i.Comment == ClientCreateHelper.ClientMark.normalClient.GetDescription());
			DbSession.Refresh(client);

			allEndpointsOk = !client.Endpoints.Any(e => e.PackageId != tariffSpeed && !e.Disabled);
			Assert.IsTrue(allEndpointsOk, "не все свичи переведены на обычную скорость");
		}
	}
}