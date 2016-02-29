using System;
using System.Linq;
using Common.Tools;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class ConnectionRequestFixture : SeleniumFixture
	{
		[Test, Ignore("Игнорируется в связи с переносом функционала на новую админку")]
		public void Create_request()
		{
			Open();
			Click("Регистрация");
			AssertText("Регистрация новой заявки на подключение");
			Css("#request_ApplicantName").SendKeys("Ребкин Вадим Леонидович");
			Css("#request_ApplicantPhoneNumber").SendKeys("8-473-606-20-00");
			Css("#request_Street").SendKeys("Суворова");
			Css("#request_City").SendKeys("Москва");
			Css("#request_House").SendKeys("1");
			Css("#request_Apartment").SendKeys("1");
			Css("#request_RequestSource").SelectByValue("2");			// Назначить "от оператора"
			ClickButton("Сохранить");
			AssertText("Сохранено");
			AssertText("от оператора");
		}

		[Test, Ignore("Функционал перенесен в новую админку.")]
		public void Filter_request_city()
		{
			//Подготовка данных
			Request request = new Request();
			request.City = "Севастополь";
			request.Street = "Вильнюсская";
			request.House = 1;
			request.Apartment = 1;
			request.ApplicantPhoneNumber = "8-473-606-20-00";
			request.ApplicantName = "Типцова Анастасия Ивановна";
			request.Tariff = session.Query<Tariff>().First();
			request.PreInsert();
			session.Save(request);
			Request request2 = new Request();
			request2.City = "Киев";
			request2.Street = "Ленина";
			request2.House = 1;
			request2.Apartment = 1;
			request2.ApplicantPhoneNumber = "8-473-606-20-00";
			request2.ApplicantName = "Суворов Александр Владимирович";
			request2.Tariff = session.Query<Tariff>().First();
			request2.PreInsert();
			session.Save(request2);

			//Тест
			Open();
			Click("Заявки");
			AssertText(request.ApplicantName);
			AssertText(request2.ApplicantName);
			Css("#filter_City").SendKeys(request.City);
			Click("Найти");
			AssertText(request.ApplicantName);
			AssertNoText(request2.ApplicantName);
		}
	}
}