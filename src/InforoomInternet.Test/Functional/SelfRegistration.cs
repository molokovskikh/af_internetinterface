using System.Linq;
using System.Net;
using Common.Tools;
using InternetInterface.Models;
using NUnit.Framework;

namespace InforoomInternet.Test.Functional
{
	[TestFixture]
	class SelfRegistration : BaseFunctionalFixture
	{
		[Test]
		public void Self_registration()
		{
			var tariff = new Tariff("Тестовый тариф для самостоятельной регистрации", 100) {
				Hidden = true,
				CanUseForSelfRegistration = true,
			};
			var zone = new Zone("Тестовая зона") {
				IsSelfRegistrationEnabled = true
			};
			var networkSwitch = new NetworkSwitch("Тестовый коммутатор", zone);
			session.Save(tariff);
			session.Save(zone);
			session.Save(networkSwitch);
			Lease.Switch = networkSwitch;
			Lease.Ip = IPAddress.Parse("192.168.1.1");
			Lease.Endpoint = null;

			Open("Main/Warning?host=ya.ru&url=/");
			AssertText("Номер лицевого счета");
			Css("#physicalClient_ExternalClientId").SendKeys(Generator.Random().First().ToString());
			Css("#physicalClient_Surname").SendKeys("Иванов");
			Css("#physicalClient_Name").SendKeys("Иван");
			Css("#physicalClient_Patronymic").SendKeys("Иванович");
			Css("#physicalClient_PhoneNumber").SendKeys("473-2606000");
			Css("#physicalClient_Tariff_Id").SelectByValue(tariff.Id.ToString());
			Click("Продолжить");
			AssertText("Пароль");
			Click("Продолжить");
			Assert.AreEqual("http://ya.ru/", browser.Url);
		}
	}
}