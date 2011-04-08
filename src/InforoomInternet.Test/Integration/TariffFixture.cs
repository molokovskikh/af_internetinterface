using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using InforoomInternet.Initializers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate.Impl;
using NUnit.Framework;

namespace InforoomInternet.Test.Integration
{
	[TestFixture]
	public class TariffFixture
	{
		[Test]
		public void Do_not_show_hidden_tariff()
		{
			ActiveRecord.SetupFilters();

			using(new SessionScope())
			{
				var tariff = new Tariff {
					Name = "Vip �����",
					Price = 100,
					Description = "Vip �����",
					Hidden = true,
				};
				tariff.Save();

				var holder = ActiveRecordMediator.GetSessionFactoryHolder();
				var session = holder.CreateSession(typeof(ActiveRecordBase));
				session.EnableFilter("HiddenTariffs");
				holder.ReleaseSession(session);

				var tariffs = Tariff.Queryable.ToList();
				Assert.That(tariffs.FirstOrDefault(t => t.Name == "Vip �����"), Is.Null, tariffs.Implode());
			}
		}
	}
}