﻿using System;
using System.Linq;
using System.Threading;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	class RegisterClientFixture : WatinFixture
	{

		[Test]
		public void RegisterClientTest()
		{
			using (var browser = Open("Register/RegisterClient.rails"))
			{
				Assert.That(browser.Text, Is.StringContaining("Форма регистрации"));
				Assert.That(browser.Text, Is.StringContaining("Личная информация"));
				Assert.That(browser.Text, Is.StringContaining("Фамилия"));
				Assert.That(browser.Text, Is.StringContaining("Имя"));
				Assert.That(browser.Text, Is.StringContaining("Отчество"));
				Assert.That(browser.Text, Is.StringContaining("Город"));
				Assert.That(browser.Text, Is.StringContaining("Адрес"));
				Assert.That(browser.Text, Is.StringContaining("Паспортные данные"));
				Assert.That(browser.Text, Is.StringContaining("Серия паспорта"));
				Assert.That(browser.Text, Is.StringContaining("Номер паспорта"));
				Assert.That(browser.Text, Is.StringContaining("Кем выдан"));
				Assert.That(browser.Text, Is.StringContaining("Адрес регистрации"));
				Assert.That(browser.Text, Is.StringContaining("Регистрационные данные"));
				Assert.That(browser.Text, Is.StringContaining("Тариф"));
				Assert.That(browser.Text, Is.StringContaining("Внести сумму по тарифному плану"));
				Assert.That(browser.Text, Is.StringContaining("Внести сумму:"));
				Assert.That(browser.Text, Is.StringContaining("Зарегистрировать"));
				browser.TextField(Find.ById("Surname")).AppendText("TestSurname");
				browser.TextField(Find.ById("Name")).AppendText("TestName");
				browser.TextField(Find.ById("Patronymic")).AppendText("TestPatronymic");
				browser.TextField(Find.ById("City")).AppendText("TestCity");
				browser.TextField(Find.ById("Street")).AppendText("TestStreet");
				browser.TextField(Find.ById("House")).AppendText("5");
				browser.TextField(Find.ById("Apartment")).AppendText("5");
				browser.TextField(Find.ById("Entrance")).AppendText("5");
				browser.TextField(Find.ById("Floor")).AppendText("1");
				browser.TextField(Find.ById("PhoneNumber")).AppendText("8-111-222-33-44");
				browser.TextField(Find.ById("HomePhoneNumber")).AppendText("1111-22222");
				browser.TextField(Find.ById("PassportSeries")).AppendText("1234");
				browser.TextField(Find.ById("PassportNumber")).AppendText("123456");
				browser.TextField(Find.ById("WhoGivePassport")).AppendText("TestWhoGivePassport");
				browser.TextField(Find.ById("RegistrationAdress")).AppendText("TestRegistrationAdress");
				browser.TextField(Find.ById("PassportDate")).AppendText("10.01.2002");
				browser.TextField(Find.ById("ConnectSumm")).AppendText("100");
				browser.Button(Find.ById("RegisterClientButton")).Click();
				Thread.Sleep(2000);
				Assert.That(browser.Text, Is.StringContaining("прописанный по адресу:"));
				Assert.That(browser.Text, Is.StringContaining("адрес подключения:"));
				Assert.That(browser.Text, Is.StringContaining("принимаю подключение к услугам доступа"));
				var pc = PhysicalClients.FindAllByProperty("Login", browser.Span(Find.ById("LoginSpan")).Text).ToList();
				foreach (var phisicalClientse in pc)
				{
					phisicalClientse.DeleteAndFlush();
				}
			}
		}
	}
}