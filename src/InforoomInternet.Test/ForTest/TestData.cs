using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;

namespace InforoomInternet.Test.ForTest
{
	public class TestData
	{
		public static Client ClientAndPhysical()
		{
			return new Client {
				Disabled = false,
				RatedPeriodDate = DateTime.Now,
				BeginWork = DateTime.Now,
				PhysicalClient =
					new PhysicalClients {
						Balance = -200,
						Tariff = new Tariff {
								Name = "Тестовый",
								Price = 100,
								Description = "Тестовый тариф"
							}
					}
			};
		}
	}
}
