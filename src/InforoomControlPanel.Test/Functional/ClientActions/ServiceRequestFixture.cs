using System;
using System.Linq;
using Common.Tools.Calendar;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace InforoomControlPanel.Test.Functional.ClientActions
{
	internal class ServiceRequestFixture : ClientActionsFixture
	{	
		[Test, Description("Успешное создание сервисной заявки")]
		public void ServiceRequestAddSuccessfully()
		{
			Open("Client/List");
			var serviceRequestCount = DbSession.Query<ServiceRequest>().ToList().Count;
			var client = "Кузнецов Иван нормальный клиент";
			var pathForButton = browser.FindElementByXPath("//td[contains(.,'" + client + "')]");
			var row = pathForButton.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector(".entypo-doc-text"));
			button.Click();
			var serviceRequestText = browser.FindElementByName("serviceRequest.Description");
			serviceRequestText.SendKeys("Тестовая сервисная заявка");
			browser.FindElementByCssSelector(".btn-green").Click();
			AssertText("Сервисная заявка успешно добавлена");
			var serviceRequestCountAfterTest = DbSession.Query<ServiceRequest>().ToList().Count;
			Assert.That(serviceRequestCountAfterTest, Is.EqualTo(serviceRequestCount + 1), "В базе данных колличество сервисных заявок должно увеличиться на один");
			var serviceRequest = DbSession.Query<ServiceRequest>().First(p => p.Description == "Тестовая сервисная заявка");
			Assert.That(serviceRequest.Client.Comment, Is.StringContaining("нормальный клиент"), "В базе данных у сервисной заявки должен быть сохранен клиент для которого создавали заявку");
		}

		[Test, Description("Неуспешное создание сервисной заявки")]
		public void ServiceRequestAddNosuccessfully()
		{
			Open("Client/List");
			var serviceRequestCount = DbSession.Query<ServiceRequest>().ToList().Count;
			var client = "Кузнецов Иван нормальный клиент";
			var pathForButton = browser.FindElementByXPath("//td[contains(.,'" + client + "')]");
			var row = pathForButton.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector(".entypo-doc-text"));
			button.Click();
			browser.FindElementByCssSelector(".btn-green").Click();
			AssertText("may not be null or empty");
			var serviceRequestCountAfterTest = DbSession.Query<ServiceRequest>().ToList().Count;
			Assert.That(serviceRequestCountAfterTest, Is.EqualTo(serviceRequestCount), "В базе данных колличество сервисных заявок не должно увеличиться");
		}
	}
}