﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Castle.ActiveRecord;
using InternetInterface;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support.Web;
using WatiN.Core;

namespace InforoomInternet.Test.Functional
{
	[TestFixture, Ignore("Чинить")]
	public class BaseFunctional : WatinFixture2
	{
		private Client _client;

		[SetUp]
		public void SetUp()
		{
			_client = ClientHelper.PhysicalClient().Client;
			_client.PhysicalClient.Password = CryptoPass.GetHashString("1234");
			session.Save(_client);
			browser = Open("/PrivateOffice/IndexOffice");
			browser.TextField("Login").AppendText(_client.Id.ToString());
			browser.TextField("Password").AppendText("1234");
			browser.Button("LogBut").Click();
		}

		[TearDown]
		public void Exit()
		{
			browser = Open("/PrivateOffice/IndexOffice");
			browser.Link("exitLink").Click();
		}

		[Test]
		public void MainTest()
		{
			browser = Open();
			Assert.That(browser.Text, Is.StringContaining("Тарифы"));
			Assert.That(browser.Text, Is.StringContaining("Контакты"));
			Assert.That(browser.Text, Is.StringContaining("Личный кабинет"));
			Assert.That(browser.Text, Is.StringContaining("Реквизиты"));
			Assert.That(browser.Text, Is.StringContaining("Уважаемые абоненты!"));
		}

		[Test]
		public void RequsiteTest()
		{
			browser = Open("Main/requisite");
			Assert.That(browser.Text, Is.StringContaining("ОГРН"));
			Assert.That(browser.Text, Is.StringContaining("КПП"));
			Assert.That(browser.Text, Is.StringContaining("Телефон"));
			Assert.That(browser.Text, Is.StringContaining("Расчетный"));
			Assert.That(browser.Text, Is.StringContaining("Директор"));
		}

		[Test]
		public void ZayavkaTest()
		{
			browser = Open("Main/zayavka");
			Assert.That(browser.Text, Is.StringContaining("Номер телефона:"));
			Assert.That(browser.Text, Is.StringContaining("Электронная почта:"));
			browser.TextField("fio").AppendText("pupkin vasilii aristarkhovich");
			browser.TextField("phone_").AppendText("8-900-900-90-90");
			browser.TextField("email").AppendText("vasia@mail.ru");
			browser.TextField("City").AppendText("bebsk");
			browser.TextField("residence").AppendText("matrosova");
			browser.TextField("House").AppendText("45");
			browser.TextField("CaseHouse").AppendText("5");
			browser.TextField("Apartment").AppendText("1");
			browser.TextField("Entrance").AppendText("1");
			browser.TextField("Floor").AppendText("1");
			browser.Button("appbut").Click();
			Thread.Sleep(2000);
			Assert.That(browser.Text, Is.StringContaining("Спасибо, Ваша заявка принята."));
			Request.FindAll().Last().DeleteAndFlush();
			browser = Open("Main/zayavka");
			browser.Button("appbut").Click();
			Assert.That(browser.Text, Is.StringContaining("Введите ФИО"));
			Assert.That(browser.Text, Is.StringContaining("Введите улицу"));
			Assert.That(browser.Text, Is.StringContaining("Введите номер дома"));
		}


		[Test]
		public void OfferTest()
		{
			browser = Open("Main/OfferContract");
			Assert.That(browser.Text, Is.StringContaining("Договор оферта стр1."));
			Assert.That(browser.Text, Is.StringContaining("Договор оферта стр5."));
			Assert.That(browser.Text, Is.StringContaining("Тарифы "));
			Assert.That(browser.Text, Is.StringContaining("Порядок расчетов стр1."));
			var imageIds = new List<string> {
				"imd1",
				"imd2",
				"imd3",
				"imd4",
				"imd5",
				"imt",
				"imo1",
				"imo2",
				"imo3",
				"imp1",
				"imp2",
				"impo1",
				"impo2",
				"impo3"
			};
			var imagesUris = new List<string>();
			foreach (var imageId in imageIds) {
				imagesUris.Add(browser.Image(imageId).Uri.AbsoluteUri);
			}
			foreach (var imagesUri in imagesUris) {
				browser.GoTo(imagesUri);
				Assert.That(browser.Text, Is.Not.StringContaining("Description: HTTP 404"));
			}
		}

		[Test]
		public void LinkTest()
		{
			browser = Open();
			var links = browser.Div(Find.ByClass("middle")).Links.Select(l => l.Url.Split(new[] { '/' })[3] + "/" + l.Url.Split(new[] { '/' })[4]).ToList();
			foreach (var link in links) {
				browser = Open(link);
				Assert.That(browser.Text, Is.StringContaining("Новости"));
			}
		}
	}
}