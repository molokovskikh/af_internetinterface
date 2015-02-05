using System;
using System.Linq;
using Common.Tools;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class ConnectionRequestFixture : HeadlessFixture
	{
		[Test]
		public void Create_request()
		{
			Open();
			Click("Регистрация");
			AssertText("Регистрация новой заявки на подключение");
			Input("request_ApplicantName", "Ребкин Вадим Леонидович");
			Input("request_ApplicantPhoneNumber", "8-473-606-20-00");
			Input("request_Street", "Суворова");
			Input("request_City", "Москва");
			Input("request_House", "1");
			Input("request_Apartment", "1");
			Select("request_RequestSource", "2");			// Назначить "от оператора"
			ClickButton("Сохранить");
			AssertText("Сохранено");
			AssertText("от оператора");
		}

		[Test]
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
			Input("filter_City", request.City);
			ClickButton("Найти");
			AssertText(request.ApplicantName);
			Assert.That(page.Html, Is.Not.StringContaining(request2.ApplicantName));
		}
	}
}