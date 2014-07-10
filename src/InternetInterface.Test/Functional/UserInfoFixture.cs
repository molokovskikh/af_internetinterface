using System;
using System.Threading;
using InternetInterface.Models;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class UserInfoFixture : ClientFunctionalFixture
	{
		[Test]
		public void Base_view_test()
		{
			Open(string.Format("UserInfo/SearchUserInfo?filter.ClientCode={0}", Client.Id));
			AssertText(string.Format("Дата начала расчетного периода: {0}", DateTime.Now.ToShortDateString()));
			AssertText(string.Format("Дата начала программы скидок: {0}", DateTime.Now.AddMonths(-1).ToShortDateString()));
		}

		[Test]
		public void ChangeStatus()
		{
			Client.Status = Status.Find((uint)StatusType.BlockedAndNoConnected);
			session.Save(Client);
			Open(ClientUrl);

			Assert.That(Css("#ChStatus").SelectedOption.Text, Is.EqualTo(" Заблокирован "));
			WaitForText("Сохранить");
			Css("#SaveButton").Click();

			Assert.That(Client.Status.Type, Is.EqualTo(StatusType.BlockedAndNoConnected));
			AssertText("Статус не был изменен, т.к. нельзя изменить статус 'Зарегистрирован' вручную. Остальные данные были сохранены.");

			Client.Status = Status.Find((uint)StatusType.Worked);
			session.Save(Client);

			Open(ClientUrl);

			Assert.That(Css("#ChStatus").SelectedOption.Text, Is.EqualTo(" Подключен "));
			Css("#ChStatus").SelectByText(" Заблокирован ");
			Css("#SaveButton").Click();

			AssertText("Данные изменены");

			Client.Refresh();
			Assert.That(Client.Status.Type, Is.EqualTo(StatusType.NoWorked));
			Assert.That(Client.Sale, Is.EqualTo(0));
			Assert.That(Client.StartNoBlock, Is.Null);
		}

		[Test]
		public void CheckedTest()
		{
			Open(ClientUrl);
			var checkbox = browser.FindElementById("client_Checked");
			if(!checkbox.Selected)
				checkbox.Click();

			WaitForText("Сохранить");
			Css("#SaveButton").Click();

			session.Refresh(Client.PhysicalClient);
			Assert.IsTrue(Client.PhysicalClient.Checked);
		}

		[Test]
		public void Make_reservation()
		{
			Client.Status = session.Load<Status>((uint)StatusType.BlockedAndNoConnected);
			session.Save(Client);
			Open();

			Click("Назначить в график");
			AssertText("Назначение в график клиента");
			WaitAjax();
			SafeClick("[name=\"graph_button\"]");

			Click("Зарезервировать");
			WaitAjax();
			AssertText("Резерв");
		}

		[Test]
		public void TelephoneTest()
		{
			Open(string.Format("UserInfo/SearchUserInfo?filter.ClientCode={0}&filter.EditConnectInfoFlag=True", Client.Id));
			AssertText("Информация по клиенту");
			Css("#addContactButton").Click();
			browser.FindElementByClassName("telephoneField").SendKeys("900-9090900");
			Css("#SaveContactButton").Click();
			AssertText("900-9090900");
			Assert.That(IsPresent(".telephoneField"), Is.False);
		}

		[Test]
		public void NotEditAddressForRefused()
		{
			Client.AdditionalStatus = session.QueryOver<AdditionalStatus>()
				.Where(s => s.ShortName == "Refused")
				.SingleOrDefault();
			session.Save(Client);
			Open(ClientUrl);
			AssertNoText("Дом ");
		}

		[Test]
		public void EditClientNameTest()
		{
			Open(ClientUrl);
			Css("#client_Surname").Clear();
			Css("#client_Surname").SendKeys("Иванов");
			Css("#client_Name").Clear();
			Css("#client_Name").SendKeys("Иван");
			Css("#client_Patronymic").Clear();
			Css("#client_Patronymic").SendKeys("Иванович");
			Css("#SaveButton").Click();

			session.Refresh(Client);
			Assert.That(Client.Name, Is.EqualTo(string.Format("{0} {1} {2}", "Иванов", "Иван", "Иванович")));
		}

		[Test]
		public void RequestGraphTest()
		{
			Open("UserInfo/RequestGraph");
			AssertText("Настройки");
			Css("#naznach_but_1").Click();
			AssertText("Настройки");
			Css("#print_button").Click();
			AssertText("Время");
		}

		[Test]
		public void UserWriteOffsTest()
		{
			Open(ClientUrl);
			Css("#userWriteOffButton").Click();
			AssertText("Значение должно быть больше нуля");
			AssertText("Введите комментарий");
			browser.FindElementById("userWriteOffComment").SendKeys("Тестовый комментарий");
			browser.FindElementById("userWriteOffSum").SendKeys("50");
			Css("#userWriteOffButton").Click();
			AssertText("Списание ожидает обработки");
		}

		[Test]
		public void Reset_client()
		{
			var brigad = new Brigad("test");
			session.Save(brigad);
			var connectGraph = new ConnectGraph(Client, DateTime.Now, brigad);
			session.Save(connectGraph);

			Open(ClientUrl);
			WaitForText("Сохранить");
			Css("#SaveButton").Click();
			session.Refresh(Client);
			Assert.IsNotNull(Client.ConnectGraph);
			Click("Сбросить");

			Open(ClientUrl);
			session.Refresh(Client);
			Assert.IsNull(Client.ConnectGraph);
			WaitForText("Сохранить");
			Css("#SaveButton").Click();
			WaitForText("Назначить в график");

			Click("Назначить в график");
			AssertText("Назначение в график клиента");
			WaitAjax();
			SafeClick("[name=\"graph_button\"]");
			Click("Назначить");
			WaitAjax();
			AssertText("Информация по клиенту");
			AssertText("Сбросить");
		}

		private void SafeClick(string css)
		{
			WaitClickable(css);
			var element = browser.FindElementByCssSelector(css);
			browser.ExecuteScript(String.Format("window.scrollTo({0},{1})", element.Location.X, element.Location.Y));
			element.Click();
		}

		private void WaitReveal()
		{
			WaitAnimation(".reveal-modal-bg");
			WaitAnimation("#myModal");
		}
	}
}
