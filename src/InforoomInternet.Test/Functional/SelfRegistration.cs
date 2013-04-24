using System;
using System.Linq;
using System.Net;
using Common.Tools;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NUnit.Framework;

namespace InforoomInternet.Test.Functional
{
	[TestFixture]
	public class SelfRegistration : BaseFunctionalFixture
	{
		[Test]
		public void Self_registration()
		{
			var tariff = new Tariff("Тестовый тариф для самостоятельной регистрации", 100) {
				Hidden = true,
				CanUseForSelfRegistration = true,
			};
			var zone = new Zone {
				IsSelfRegistrationEnabled = true
			};
			var networkSwitch = new NetworkSwitch("Тестовый коммутатор", zone);
			session.Save(tariff);
			session.Save(zone);
			session.Save(networkSwitch);
			Lease.Switch = networkSwitch;
			Lease.Ip = IPAddress.Parse("192.168.1.1");
			Lease.Endpoint = null;

			Open("Main/Warning");
			AssertText("Номер абонента");
			Css("#physicalClient_ExternalClientId").TypeText(Generator.Random().First());
			Css("#physicalClient_Surname").TypeText("Иванов");
			Css("#physicalClient_Name").TypeText("Иван");
			Css("#physicalClient_Patronymic").TypeText("Иванович");
			Css("#physicalClient_PhoneNumber").TypeText("473-2606000");
			Css("#physicalClient_Tariff_Id").Select("Тестовый тариф для самостоятельной регистрации");
			Click("Продолжить");
			AssertText("Пароль");
		}
	}
}