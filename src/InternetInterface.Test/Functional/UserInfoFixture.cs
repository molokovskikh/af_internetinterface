using System;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

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
			using (new SessionScope())
			{
				lp = ClientHelper.CreateLaywerPerson();
				lp.Sale = 5;
				lp.StartNoBlock = DateTime.Now;
				lp.Save();
			}
			using (var browser = Open("Search/Redirect?filter.ClientCode=" + lp.Id))
			{
				browser.TextField("BalanceText").AppendText("1234");
				browser.Button("ChangeBalanceButton").Click();
				Assert.That(browser.Text, Is.StringContaining("1234"));
			}
			lp.Delete();
		}

		[Test, Ignore("Чинить")]
		public void ChangeStatus()
		{
			Client.Status = Status.Find((uint) StatusType.BlockedAndNoConnected);
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

		[Test, Ignore("Чинить")]
		public void ReservTest()
		{
			using (var browser = Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", Client.Id)))
			{
				browser.Button("naznach_but").Click();
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
				using (var browser = Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", validClient.Id)))
				{
					browser.Link(Find.ByClass("button hardwareInfo")).Click();
					Assert.That(browser.Text, Is.StringContaining(validClient.Name));
					Assert.That(browser.Text, Is.StringContaining("FastEthernet"));
				}
			else
				throw new Exception();
		}

		[Test, Ignore("Чинить")]
		public void TelephoneTest()
		{
			using (var browser = Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}&filter.EditConnectInfoFlag=True", Client.Id))) {
				browser.Button(Find.ByName("callButton")).Click();
				Assert.That(browser.Text, Is.StringContaining("Информация по клиенту"));
				browser.Button("addContactButton").Click();
				browser.TextFields.Last(f => f.ClassName == "telephoneField").AppendText("900-9090900");
				browser.Button("SaveContactButton").Click();
				Assert.That(browser.Text, Is.StringContaining("900-9090900"));
				Assert.IsNull(browser.TextFields.LastOrDefault(f => f.ClassName == "telephoneField"));
			}
		}

		[Test, Ignore]
		public void AdditionalStatusTest()
		{
			using (new SessionScope())
			{
				Client = Models.Client.Queryable.First(c => c.PhysicalClient != null && Brigad.FindAll().Contains(c.WhoConnected));
				Client.AdditionalStatus = null;
				Client.Status = Status.Find((uint)StatusType.BlockedAndNoConnected);
				Client.UpdateAndFlush();
				ClientUrl = string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", Client.Id);
			}
			using (var browser = Open(ClientUrl))
			{
				browser.Button("NotPhoned").Click();
				browser.TextField("NotPhoned_textField").AppendText("Тестовое сообщение перезвонить");
				browser.Button("NotPhoned_but").Click();
				using (new SessionScope())
				{
					Client.Refresh();
					Assert.That(Client.AdditionalStatus.Id, Is.EqualTo((uint)AdditionalStatusType.NotPhoned));
					Assert.That(browser.Text, Is.StringContaining("Тестовое сообщение перезвонить"));
					Assert.That(browser.Text, Is.StringContaining("Неудобно говорить"));
				}
				browser.Button("naznach_but").Click();
				browser.RadioButton(Find.ByName("graph_button")).Checked = true;
				browser.Button("naznach_but_1").Click();
				Thread.Sleep(2000);
				using (new SessionScope())
				{
					Client.Refresh();
					Assert.That(Client.AdditionalStatus.Id, Is.EqualTo((uint)AdditionalStatusType.AppointedToTheGraph));
					Assert.That(ConnectGraph.Queryable.Where(c => c.Client == Client).Count(), Is.EqualTo(1));
				}
				browser.Button(Find.ById("Refused")).Click();
				Thread.Sleep(1000);
				browser.TextField("Refused_textField").AppendText("Тестовое сообщение отказ");
				browser.Button("Refused_but").Click();
				Thread.Sleep(2000);
				using (new SessionScope())
				{
					Client.Refresh();
					Assert.That(Client.AdditionalStatus.Id, Is.EqualTo((uint)AdditionalStatusType.Refused));
					Assert.That(browser.Text, Is.StringContaining("Тестовое сообщение отказ"));
					Assert.That(browser.Text, Is.StringContaining("Перезвонит сам"));
				}
			}
		}

		[Test, Ignore("Чинить")]
		public void SaveSwitchForClientTest()
		{
			using (var browser = Open(ClientUrl))
			{
				var selectList = browser.SelectList(Find.ByName("ConnectInfo.Switch"));
				selectList.SelectByValue(selectList.Options.First(o => o.Text == "Юго-Восточный 10 саt 2950").Value);
				browser.TextField("Port").AppendText("10");
				browser.Button("Submit2").Click();
				Thread.Sleep(500);
				using (new SessionScope())
				{
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
			using (new SessionScope())
			{
				var insStatClient =
					Internetsessionslog.Queryable.Where(i => i.EndpointId.Client.PhysicalClient != null).ToList().
						GroupBy(i => i.EndpointId).First(g => g.Count() > 20).Select(
							e => e.EndpointId.Client.Id).First();
				using (
					var browser =
						Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", insStatClient)))
				{
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
			using (var browser = Open("UserInfo/SearchUserInfo.rails?filter.ClientCode=173&filter.Editing=true"))
			{
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
			using (var browser = Open("UserInfo/RequestGraph.rails"))
			{
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
			using (var browser = Open(ClientUrl))
			{
				browser.Button("userWriteOffButton").Click();
				Assert.That(browser.Text, Is.StringContaining("Значение должно быть больше нуля"));
				Assert.That(browser.Text, Is.StringContaining("Введите комментарий"));
				browser.TextField("userWriteOffComment").AppendText("Тестовый комментарий");
				browser.TextField("userWriteOffSum").AppendText("50");
				browser.Button("userWriteOffButton").Click();
				Assert.That(browser.Text, Is.StringContaining("Списание ожидает обработки"));
			}
		}
	}
}
