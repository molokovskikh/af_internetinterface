using System;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using WatiN.Core;
using WatiN.Core.DialogHandlers;
using WatiN.Core.Native.Windows;
using UseDialogOnce = InternetInterface.Test.Helpers.UseDialogOnce;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class UserInfoFixture : ClientFunctionalFixture
	{
		[Test]
		public void Base_view_test()
		{
			Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", Client.Id));
			AssertText(string.Format("Дата начала расчетного периода: {0}", DateTime.Now.ToShortDateString()));
			AssertText(string.Format("Дата начала программы скидок: {0}", DateTime.Now.AddMonths(-1).ToShortDateString()));
		}

		[Test, Ignore("Чинить")]
		public void ChangeBalanceLawyerPersonTest()
		{
			var lp = new Client();
			using (new SessionScope()) {
				lp = ClientHelper.CreateLaywerPerson();
				lp.Sale = 5;
				lp.StartNoBlock = DateTime.Now;
				lp.Save();
			}
			using (var browser = Open("Search/Redirect?filter.ClientCode=" + lp.Id)) {
				browser.TextField("BalanceText").AppendText("1234");
				browser.Button("ChangeBalanceButton").Click();
				Assert.That(browser.Text, Is.StringContaining("1234"));
			}
			lp.Delete();
		}

		[Test]
		public void ChangeStatus()
		{
			Client.Status = Status.Find((uint)StatusType.BlockedAndNoConnected);
			Client.Update();
			browser = Open(ClientUrl);

			Assert.That(browser.SelectList("ChStatus").SelectedItem, Is.EqualTo(" Заблокирован "));
			browser.Button("SaveButton").Click();

			Client.Refresh();
			Assert.That(Client.Status.Type, Is.EqualTo(StatusType.BlockedAndNoConnected));

			Client.Status = Status.Find((uint)StatusType.Worked);
			Client.Update();

			browser = Open(ClientUrl);

			Assert.That(browser.SelectList("ChStatus").SelectedItem, Is.EqualTo(" Подключен "));
			browser.SelectList("ChStatus").Select(" Заблокирован ");
			browser.Button("SaveButton").Click();
			Assert.That(browser.Text, Is.StringContaining("Данные изменены"));

			Client.Refresh();
			Assert.That(Client.Status.Type, Is.EqualTo(StatusType.NoWorked));
			Assert.That(Client.Sale, Is.EqualTo(0));
			Assert.That(Client.StartNoBlock, Is.Null);
		}

		[Test]
		public void ReservTest()
		{
			Client.Status = Status.Find((uint)StatusType.BlockedAndNoConnected);
			session.SaveOrUpdate(Client);
			using (var browser = Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", Client.Id))) {
				browser.Button("naznach_but").Click();
				Thread.Sleep(500);
				browser.RadioButton(Find.ByName("graph_button")).Checked = true;
				browser.Button("reserv_but").Click();
				Assert.IsTrue(browser.Text.Contains("Резерв"));
			}
		}

		[Test, Ignore("Чинить")]
		public void HardWareTest()
		{
			Client validClient;
			using (new SessionScope()) {
				validClient = Models.Client.Queryable.FirstOrDefault(c => c.Status.Id == 5);
			}
			if (validClient != null)
				using (var browser = Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", validClient.Id))) {
					browser.Link(Find.ByClass("button hardwareInfo")).Click();
					Assert.That(browser.Text, Is.StringContaining(validClient.Name));
					Assert.That(browser.Text, Is.StringContaining("FastEthernet"));
				}
			else
				throw new Exception();
		}

		[Test]
		public void TelephoneTest()
		{
			using (var browser = Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}&filter.EditConnectInfoFlag=True", Client.Id))) {
				Assert.That(browser.Text, Is.StringContaining("Информация по клиенту"));
				browser.Button("addContactButton").Click();
				browser.TextFields.Last(f => f.ClassName == "telephoneField").AppendText("900-9090900");
				browser.Button("SaveContactButton").Click();
				Assert.That(browser.Text, Is.StringContaining("900-9090900"));
				Assert.IsNull(browser.TextFields.LastOrDefault(f => f.ClassName == "telephoneField"));
			}
		}

		[Test]
		public void NotEditAddressForRefused()
		{
			Client.AdditionalStatus = session.QueryOver<AdditionalStatus>()
				.Where(s => s.ShortName == "Refused")
				.SingleOrDefault();
			session.Save(Client);
			Open(ClientUrl);
			Assert.That(browser.Text, Is.Not.Contains("Дом "));
		}

		[Test, Ignore]
		public void AdditionalStatusTest()
		{
			Client.AdditionalStatus = null;
			Client.Status = Status.Find((uint)StatusType.BlockedAndNoConnected);
			session.SaveOrUpdate(Client);
			ClientUrl = string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", Client.Id);
			using (var browser = Open(ClientUrl)) {
				browser.Button("NotPhoned").Click();
				browser.TextField("NotPhoned_textField").AppendText("Тестовое сообщение перезвонить");
				browser.Button("NotPhoned_but").Click();
				Client.Refresh();
				Assert.That(Client.AdditionalStatus.Id, Is.EqualTo((uint)AdditionalStatusType.NotPhoned));
				Assert.That(browser.Text, Is.StringContaining("Тестовое сообщение перезвонить"));
				Assert.That(browser.Text, Is.StringContaining("Неудобно говорить"));
				browser.Button("naznach_but").Click();
				browser.RadioButton(Find.ByName("graph_button")).Checked = true;
				browser.Button("naznach_but_1").Click();
				Thread.Sleep(1000);
				Flush();
				//Client = session.Get<Client>(Client.Id);
				session.Refresh(Client);
				Assert.That(Client.AdditionalStatus.Id, Is.EqualTo((uint)AdditionalStatusType.AppointedToTheGraph));
				Assert.That(ConnectGraph.Queryable.Where(c => c.Client == Client).Count(), Is.EqualTo(1));
				browser.Button(Find.ById("Refused")).Click();
				Thread.Sleep(1000);
				browser.TextField("Refused_textField").AppendText("Тестовое сообщение отказ");
				browser.Button("Refused_but").Click();
				Thread.Sleep(1000);
				Client.Refresh();
				Assert.That(Client.AdditionalStatus.Id, Is.EqualTo((uint)AdditionalStatusType.Refused));
				Assert.That(browser.Text, Is.StringContaining("Тестовое сообщение отказ"));
				Assert.That(browser.Text, Is.StringContaining("Перезвонит сам"));
			}
		}

		[Test, Ignore("Чинить")]
		public void SaveSwitchForClientTest()
		{
			using (var browser = Open(ClientUrl)) {
				var selectList = browser.SelectList(Find.ByName("ConnectInfo.Switch"));
				selectList.SelectByValue(selectList.Options.First(o => o.Text == "Юго-Восточный 10 саt 2950").Value);
				browser.TextField("Port").AppendText("10");
				browser.Button("Submit2").Click();
				Thread.Sleep(500);
				using (new SessionScope()) {
					EndPoint.Refresh();
					Assert.That(EndPoint.Port, Is.EqualTo(10));
					Assert.NotNull(EndPoint.Switch);
					Assert.That(EndPoint.Switch.Name, Is.EqualTo("Юго-Восточный 10 саt  2950"));
				}
			}
		}

		[Test, Ignore("Чинить")]
		public void StatisticTest()
		{
			using (new SessionScope()) {
				var insStatClient =
					Internetsessionslog.Queryable.Where(i => i.EndpointId.Client.PhysicalClient != null).ToList()
						.GroupBy(i => i.EndpointId).First(g => g.Count() > 20).Select(
						e => e.EndpointId.Client.Id)
						.First();
				using (
					var browser =
						Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", insStatClient))) {
					browser.Button("Statistic").Click();
					Assert.That(browser.Text, Is.StringContaining("Статистика работы клиента"));
					Assert.That(browser.Text, Is.StringContaining("Первая"));
					Assert.That(browser.Text, Is.StringContaining("Последняя"));
					Assert.That(browser.Text, Is.StringContaining("Mac адрес"));
				}
			}
		}

		[Test]
		public void EditClientNameTest()
		{
			Open(ClientUrl);
			browser.TextField("Surname").Clear();
			browser.TextField("Surname").AppendText("Иванов");
			browser.TextField("Name").Clear();
			browser.TextField("Name").AppendText("Иван");
			browser.TextField("Patronymic").Clear();
			browser.TextField("Patronymic").AppendText("Иванович");
			browser.Button("SaveButton").Click();

			Client.Refresh();
			Assert.That(Client.Name,
				Is.EqualTo(string.Format("{0} {1} {2}", "Иванов",
					"Иван", "Иванович")));
		}

		[Test, Ignore("Чинить")]
		public void UpdateTest()
		{
			using (var browser = Open("UserInfo/SearchUserInfo.rails?filter.ClientCode=173&filter.Editing=true")) {
				var tariffList = browser.SelectList("ChTariff");
				browser.SelectList("ChTariff").SelectByValue(
					tariffList.Options.First(s => s.Value != tariffList.SelectedOption.Value).Value);
				var statusList = browser.SelectList("ChStatus");
				browser.SelectList("ChStatus").SelectByValue(
					statusList.Options.First(s => s.Value != statusList.SelectedOption.Value).Value);
				browser.Button("SaveButton").Click();
				Assert.That(browser.Text, Is.StringContaining("Данные изменены"));
			}
		}

		[Test]
		public void RequestGraphTest()
		{
			using (var browser = Open("UserInfo/RequestGraph.rails")) {
				Assert.That(browser.Text, Is.StringContaining("Настройки"));
				browser.Button("naznach_but_1").Click();
				Assert.That(browser.Text, Is.StringContaining("Настройки"));
				browser.Button("print_button").Click();
				Assert.That(browser.Text, Is.StringContaining("Время"));
			}
		}

		[Test]
		public void UserWriteOffsTest()
		{
			using (var browser = Open(ClientUrl)) {
				browser.Button("userWriteOffButton").Click();
				Assert.That(browser.Text, Is.StringContaining("Значение должно быть больше нуля"));
				Assert.That(browser.Text, Is.StringContaining("Введите комментарий"));
				browser.TextField("userWriteOffComment").AppendText("Тестовый комментарий");
				browser.TextField("userWriteOffSum").AppendText("50");
				browser.Button("userWriteOffButton").Click();
				Assert.That(browser.Text, Is.StringContaining("Списание ожидает обработки"));
			}
		}

		[Test]
		public void Reset_client()
		{
			var brigad = new Brigad("test");
			session.Save(brigad);
			var connectGraph = new ConnectGraph(Client, DateTime.Now, brigad);
			session.Save(connectGraph);
			Close();

			Open(ClientUrl);
			Click("Сохранить");
			Assert.IsNotNull(session.Get<Client>(Client.Id).ConnectGraph);
			Click("Сбросить");

			Close();

			Open(ClientUrl);
			Assert.IsNull(session.Get<Client>(Client.Id).ConnectGraph);
			Click("Сохранить");
			AssertText("Назначить в график");
			Click("Назначить в график");
			browser.RadioButton(Find.ByName("graph_button")).Checked = true;
			Click("Назначить");
			AssertText("Информация по клиенту");
			AssertText("Сбросить");
		}

		[Test(Description = "Тестирует добавление адреса при редактировании абонента"), Ignore]
		public void AddAddress()
		{
			var region = new RegionHouse {
				Name = "Новый регион" + DateTime.Now
			};
			Save(region);
			Open(ClientUrl);
			Click("Создать новый");
			var streetName = "улица" + DateTime.Now;
			browser.TextField("house_Street").Value = streetName;
			browser.TextField("house_Number").Value = "1";
			browser.TextField("house_Case").Value = "1";
			browser.SelectList("house_Region_Id").SelectByValue(region.Id.ToString());
			var alertDialogHandler = new AlertDialogHandler();
			using (new UseDialogOnce(browser.DialogWatcher, alertDialogHandler)) {
				browser.Link(Find.ByText("Зарегистрировать")).Click();
				alertDialogHandler.WaitUntilExists();
				alertDialogHandler.OKButton.Click();
				browser.WaitForComplete();
			}
			AssertText("Личная информация");
			Assert.IsTrue(browser.SelectList("houses_select")
				.AllContents
				.Contains(String.Format("{0} {1} {2}", streetName, 1, 1)));
			var house = session.Query<House>().First(h => h.Street == streetName);
			Assert.That(house.Region.Name, Is.EqualTo(region.Name));
		}
	}
}