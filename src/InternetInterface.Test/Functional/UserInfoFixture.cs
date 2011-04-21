using System;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	class UserInfoFixture : WatinFixture
	{
		[Test]
		public void SaveSwitchForClientTest()
		{
			var physicalClient = new PhysicalClients
			                     	{
			                     		Name = "Alexandr"
										,Surname = "Zolotarev",
										Patronymic = "Alekseevich",
										Street = "Stud",
										House = "12",
										Apartment = "1",
										Entrance = "2",
										Floor = "2",
										PhoneNumber = "8-473-2606-000",
										Balance = 0,
										Tariff = Tariff.Queryable.First()

			                     	};
			physicalClient.SaveAndFlush();
			var client = new Clients
			             	{
			             		PhysicalClient = physicalClient,
								BeginWork = null,
								Name = string.Format("{0} {1} {2}",physicalClient.Name , physicalClient.Surname, physicalClient.Patronymic)
			             	};
			client.SaveAndFlush();
			var endPoint = new ClientEndpoints
			               	{
			               		Client = client,
			               	};
			endPoint.SaveAndFlush();
			var format = string.Format("UserInfo/SearchUserInfo.rails?ClientCode={0}&EditingConnect=true", physicalClient.Id);
			using (var browser = Open(format))
			{
				var selectList = browser.SelectList(Find.ByName("ConnectInfo.Switch"));
				selectList.SelectByValue(selectList.Options.First(o => o.Text == "Юго-Восточный 10 саt 2950").Value);
				browser.TextField("Port").AppendText("10");
				browser.Button("Submit2").Click();
				Thread.Sleep(500);
				Assert.That(browser.Text, Is.StringContaining("Данные изменены"));
				using (new SessionScope())
				{
					endPoint.Refresh();
					Assert.That(endPoint.Port, Is.EqualTo(10));
					Assert.NotNull(endPoint.Switch);
					Assert.That(endPoint.Switch.Name, Is.EqualTo("Юго-Восточный 10 саt  2950"));
				}
			}
		}
	}
}
