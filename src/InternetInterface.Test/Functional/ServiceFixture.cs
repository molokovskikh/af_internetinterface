using System;
using System.Linq;
using System.Threading;
using Common.Tools.Calendar;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	public class ServiceFixture : SeleniumFixture
	{
		Client client;
		Partner registrator;
		Partner performer;

		[SetUp]
		public void Setup()
		{
			performer = new Partner(session.Query<UserRole>().First(c => c.ReductionName == "service")) {
				Name = Guid.NewGuid().ToString(),
				Login = Guid.NewGuid().ToString(),
			};
			session.Save(performer);
			registrator = session.Query<Partner>().First(p => p.Login == Environment.UserName);
			client = ClientHelper.Client(session);
			session.Save(client);
		}

		[Test]
		public void Create_request()
		{
			Open("UserInfo/SearchUserInfo?filter.ClientCode={0}", client.Id);
			Click("Сервисная заявка");

			Css("#request_Performer_Id").SelectByText(performer.Name);
			Css("textarea[name=\"request.Description\"]").SendKeys("test");
			Css("input[name=\"request.PerformanceDate\"]").SendKeys("21.05.2012");
			Css("input[name=\"request.Contact\"]").SendKeys("900-9090900");

			Eval("$('.input-date').datepicker('hide')");
			WaitForHiddenCss("#ui-datepicker-div");

			WaitAjax("date=21.05.2012&id=" + performer.Id);

			Css("input[name=\"request.PerformanceTime\"][value=\"10:00:00\"]").Click();
			Click("Сохранить");
			AssertText("Информация по клиенту");

			var request = session.Query<ServiceRequest>().Where(r => r.Client == client).ToArray().Last();
			Assert.That(request.PerformanceDate.ToString(), Is.EqualTo("21.05.2012 10:00:00"));
			Assert.That(request.PerformanceTime.ToString(), Is.EqualTo("10:00:00"));
		}

		[Test]
		public void Disable_occupied_timeunit()
		{
			var request = CreateRequest();
			session.Save(request);

			Open("ServiceRequest/New?ClientCode={0}", client.Id);
			Css("#request_Performer_Id").SelectByText(performer.Name);
			WaitAjax("id=" + performer.Id);
			Assert.AreEqual("true", Css("input[name=\"request.PerformanceTime\"][value=\"12:30:00\"]").GetAttribute("disabled"));
		}

		[Test, Description("Проверка работоспособности ссылки - сервисной заявки")]
		public void ServiceRequestLinkCheck()
		{
			var request = CreateRequest();
			session.Save(request);
			Open("UserInfo/SearchUserInfo?filter.ClientCode={0}", client.Id);

			ClickLink(request.Id.ToString());

			Assert.That(browser.Url.Contains(request.Id.ToString()), Is.True);
		}

		[Test]
		public void ViewRequests()
		{
			var request = CreateRequest();
			session.Save(request);

			Open("ServiceRequest/ViewRequests");
			AssertText("Фильтр");
			AssertText("900-9090900");
		}

		[Test]
		public void Filter_test()
		{
			var request = CreateRequest();
			session.Save(request);
			Open("ServiceRequest/ViewRequests");

			browser.FindElementById("filter_Text").SendKeys("test_text");
			Click("Применить");
			AssertText("По вашему запросу ничего не найдено, либо вы не ввели информацию для поиска");
			browser.FindElementById("filter_Text").Clear();
			browser.FindElementById("filter_Text").SendKeys(client.Id.ToString());
			Click("Применить");
			AssertText("Фильтр");
			AssertText("900-9090900");
		}

		[Test]
		public void Sms_on_close()
		{
			var request = CreateRequest();
			session.Save(request);
			Open("ServiceRequest/ShowRequest?Id={0}", request.Id);
			Click("(Редактировать)");
			Css("#request_Status").SelectByValue("3");
			Css("#sumField").SendKeys("500");
			var sms = Css("#request_CloseSmsMessage");
			sms.SendKeys("тестовое сообщение");
			Assert.IsTrue(sms.Displayed);
			Click("Сохранить");
		}

		private void WaitAjax(string querystring)
		{
			var wait = new WebDriverWait(new SystemClock(), browser, 5.Second(), TimeSpan.FromMilliseconds(100));
			wait.Until(d => {
				var text = (Eval("return $('#timetable').data('url')") ?? "").ToString();
				wait.Message = String.Format("Не удалось дождаться состояния #timetable ждем '{0}' текущее {1}", querystring, text);
				return text.EndsWith(querystring);
			});
		}

		protected void WaitForHiddenCss(string css)
		{
			var wait = new WebDriverWait(browser, 2.Second());
			wait.Until(d => !((RemoteWebDriver)d).FindElementByCssSelector(css).Displayed);
		}

		private ServiceRequest CreateRequest()
		{
			var request = new ServiceRequest(registrator, performer, DateTime.Today.AddDays(1).Add(new TimeSpan(12, 30, 0))) {
				Client = client,
				Contact = "900-9090900",
				Description = "test"
			};
			return request;
		}
	}
}
