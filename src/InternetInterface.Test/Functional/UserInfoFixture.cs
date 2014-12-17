using System;
using System.Linq;
using System.Threading;
using Common.Tools;
using Common.Web.Ui.Components;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Test.Helpers;
using NHibernate;
using NHibernate.Linq;
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
			Open(string.Format("UserInfo/ShowPhysicalClient?filter.ClientCode={0}", Client.Id));
			AssertText(string.Format("Дата начала расчетного периода: {0}", DateTime.Now.ToShortDateString()));
			AssertText(string.Format("Дата начала программы скидок: {0}", DateTime.Now.AddMonths(-1).ToShortDateString()));
		}

		[Test(Description = "Проверяет отображение различной информации в зависимости от роли партнера")]
		public void CategoriesAccess()
		{
			//Setup
			var partner = session.Query<Partner>().First(p => p.Login == Environment.UserName);
			var prevRole = partner.Role;
			var office = session.Query<UserRole>().First(r => r.ReductionName == "Office");
			var dealer = session.Query<UserRole>().First(r => r.ReductionName == "Diller");

			try {
				partner.Role = office;
				session.Save(partner);
				Open(string.Format("UserInfo/ShowPhysicalClient?filter.ClientCode={0}", Client.Id));
				Css("#userWriteOffSum").SendKeys("1000");
				Css("#userWriteOffComment").SendKeys("Test");
				Css("#userWriteOffButton").Click();

				//Test
				var clientAppeals = "Обращения клиента";
				AssertText(clientAppeals);
				var elements = browser.FindElementsByCssSelector("#WriteOffTable .cancelButton");
				Assert.That(elements.Count, Is.GreaterThan(0));

				partner.Role = dealer;
				session.Save(partner);
				Open(string.Format("UserInfo/ShowPhysicalClient?filter.ClientCode={0}", Client.Id));
				AssertNoText(clientAppeals);
				elements = browser.FindElementsByCssSelector("#WriteOffTable .cancelButton");
				Assert.That(elements.Count, Is.EqualTo(0));
			}
			finally {
				partner.Role = prevRole;
				session.Save(partner);
			}
		}

		[Test]
		public void ChangeStatus()
		{
			Client.Status = Status.Find((uint)StatusType.BlockedAndNoConnected);
			session.Save(Client);
			Open(ClientUrl);

			Css("#ChStatus").SelectByText("Заблокирован");
			WaitForText("Сохранить");
			Css("#SaveButton").Click();

			session.Refresh(Client);
			Assert.That(Client.Status.Type, Is.EqualTo(StatusType.BlockedAndNoConnected));
			AssertText("Статус не был изменен, т.к. нельзя изменить статус 'Зарегистрирован' вручную. Остальные данные были сохранены.");

			Client.Status = Status.Find((uint)StatusType.Worked);
			session.Save(Client);

			Open(ClientUrl);

			Assert.That(Css("#ChStatus").SelectedOption.Text, Is.EqualTo("Подключен"));
			Css("#ChStatus").SelectByText("Заблокирован");
			Css("#SaveButton").Click();
			AssertText("Данные изменены");

			session.Refresh(Client);
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
			var brigad = new Brigad("Тестовая бригада");
			session.Save(brigad);
			Client.Status = session.Load<Status>((uint)StatusType.BlockedAndNoConnected);
			session.Save(Client);
			Open();

			Click("Назначить в график");
			AssertText("Назначение в график клиента");
			WaitAjax();
			SafeClick("[name=graph_button]");

			Click("Зарезервировать");
			WaitAjax();
			AssertText("Резерв");
		}

		[Test]
		public void TelephoneTest()
		{
			Open(string.Format("UserInfo/ShowPhysicalClient?filter.ClientCode={0}&filter.EditConnectInfoFlag=True", Client.Id));
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

		[Test(Description = "Проверяет создание заявки на подключение ТВ для пользователя")]
		public void CreateTvRequest()
		{
			var phone = "+7 926 123 12 13";
			var contact = new Contact(Client,ContactType.MobilePhone,phone);
			session.Save(contact);
			Open("UserInfo/ShowPhysicalClient?filter.ClientCode={0}", Client.Id);
			Click("Заявка на ТВ");
			Css("#request_Hdmi").Click();
			Css("#request_Contact").SelectByText(phone);
			Css("#request_Comment").SendKeys("Hello");
			Css("#send").Click();
			var request = session.Query<TvRequest>().First(i => i.Client == Client);
			Assert.That(request,Is.Not.Null);
			Assert.That(request.Hdmi,Is.True);
			Assert.That(request.Comment.Contains("Hello"),Is.True);
			Assert.That(request.Contact,Is.Not.Null);

			
			var issue = session.Query<RedmineIssue>().First(i => i.project_id == 67);
			Assert.That(issue, Is.Not.Null);
			Assert.That(issue.description.Contains("HDMI: да"),Is.True);

			var appeal = session.Query<Appeals>().First(i => i.Client == Client && i.Appeal.Contains("на подключение ТВ"));
			Assert.
				That(appeal, Is.Not.Null);
		}

		[Test(Description = "Проверяет создание заявки на подключение ТВ для пользователя, но используем поле, а не контакт пользователя")]
		public void CreateTvRequestWithoutContacts()
		{
			var phone = "+7 926 123 12 13";
			var contact = new Contact(Client, ContactType.MobilePhone, phone);
			session.Save(contact);
			Open("UserInfo/ShowPhysicalClient?filter.ClientCode={0}", Client.Id);
			Click("Заявка на ТВ");
			Css("#request_Hdmi").Click();
			Css("#request_AdditionalContact").SendKeys("8-926-152-23-23");
			Css("#request_Comment").SendKeys("Hello");
			Css("#send").Click();
			var request = session.Query<TvRequest>().First(i => i.Client == Client);
			Assert.That(request, Is.Not.Null);
			Assert.That(request.Hdmi, Is.True);
			Assert.That(request.Comment.Contains("Hello"), Is.True);
			Assert.That(request.AdditionalContact, Is.EqualTo("8-926-152-23-23"));
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
			SafeClick("[name=graph_button]");
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

		[Test(Description = "Проверяет возм-ть задания IP-пула для данной точки подключения пользователя")]
		public void SetIpPoolForEndPoint()
		{
			// Занесение в БД нового региона "region"
			var region = new RegionHouse("NewTestRegion");
			session.Save(region);
			region.Name += region.Id;
			session.SaveOrUpdate(region);

			// Занесение в БД 2-х новых IP-пулов
			var pool1 = new IpPool {
				Begin = 12345,
				End = 54321,
				IsGray = false
			};
			session.Save(pool1);
			var pool2 = new IpPool {
				Begin = pool1.End,
				End = pool1.Begin,
				IsGray = false
			};
			session.Save(pool2);

			// Создание в БД 2-х новых ассоциаций между IP-пулами и регионом "region"
			var poolReg1 = new IpPoolRegion(pool1, region);
			session.Save(poolReg1);
			var poolReg2 = new IpPoolRegion(pool2, region);
			session.Save(poolReg2);

			// Занесение в БД нового пользователя "client"
			var client = ClientHelper.Client(session);
			client.Name = "User_from_" + region.Name;
			client.PhysicalClient = ClientHelper.PhysicalClient(session);
			client.PhysicalClient.City = region.Name;
			client.PhysicalClient.HouseObj.Region = region;
			session.Save(client);

			// Создание в БД точки подключения для пользователя "client"
			var zone = new Zone("Zone_of_" + region.Name, region);
			session.Save(zone);
			var netSwitch = new NetworkSwitch("Switch#" + client.Id, zone);
			session.Save(netSwitch);
			var clientEndpoint = new ClientEndpoint(client, 10, netSwitch);
			session.Save(clientEndpoint);
			client.AddEndpoint(clientEndpoint, new Settings(session));

			// Проверка отсутствия IP-пула в точке подключения пользователя "client"
			Assert.That(client.Endpoints[0].Pool, Is.EqualTo(null));

			Open("UserInfo/ShowPhysicalClient?filter.ClientCode={0}", client.Id);
			Css("#EditConnectionBtn").Click();
			Css("#PoolsSelect").SelectByText(pool1.Id + " (" + pool1.Begin + "-" + pool1.End + ")");
			Css("#SaveConnectionBtn").Click();

			// Проверка наличия IP-пула "pool1" в точке подключения пользователя "client"
			client.Refresh();
			client = session.Get<Client>(client.Id);
			Assert.That(client.Endpoints[0].Pool, Is.EqualTo(pool1.Id));
		}

		[Test(Description = "Проверяет правильность первичного сохранения контактов пользователя")]
		public void PrimarySaveUserContactsTest()
		{
			// Занесение в БД нового пользователя "client"
			var client = ClientHelper.Client(session);
			session.Save(client);
			client.Name = "User_#" + client.Id;
			session.Update(client);

			// Авто-заполнение контактов клиента
			Open("UserInfo/ShowPhysicalClient?filter.ClientCode={0}&filter.EditConnectInfoFlag={1}", client.Id, true);
			var parentTag = "#ContactsTableBody2";
			var childTag = string.Empty;
			var contactText = string.Empty;
			for (var i = 1; i <= 4; i++) {
				Css("#addContactButton").Click();

				childTag = " > tr:nth-child(" + i + ")";
				contactText = Contact.GetReadbleCategorie((ContactType) (i - 1));		// (i-1) <= ContactType.Email

				Css(parentTag + childTag + "> td:nth-child(1) > input").SendKeys((i < 4) ? "999-1112233" : "mymail@mail.ru");
				Css(parentTag + childTag + "> td:nth-child(2) > select").SelectByText(contactText);
				Css(parentTag + childTag + "> td:nth-child(3) > input").SendKeys((i < 4) ? ("phone" + i) : "email");
			}
			Css("#SaveContactButton").Click();

			// Проверка правильного сохранения типов контактов в данных пользователя "client"
			parentTag = "#ContactsTableBody1";
			for (var i = 1; i <= 4; i++) {
				childTag = " > tr:nth-child(" + i + ") > td:nth-child(2)";
				var cssElement = browser.FindElementByCssSelector(parentTag + childTag);
				contactText = Contact.GetReadbleCategorie((ContactType) (i - 1));		// (i-1) <= ContactType.Email

				Assert.That(cssElement.Text, Is.EqualTo(contactText));
			}
		}
	}
}
