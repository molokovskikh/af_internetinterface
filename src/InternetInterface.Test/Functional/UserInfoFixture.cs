using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;
using WatiN.Core.Native.Windows;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	class UserInfoFixture : WatinFixture
	{
		public string format;
		public PhysicalClient physicalClient;
		public Client client;
		public ClientEndpoints endPoint;

		public UserInfoFixture()
		{
			using (new SessionScope()) {
				client = ClientHelper.Client();
				physicalClient = client.PhysicalClient;

				client.SaveAndFlush();
				endPoint = new ClientEndpoints {
					Client = client,
				};
				endPoint.SaveAndFlush();
				format =
					string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}&filter.EditingConnect=true&filter.Editing=true",
					              client.Id);
			}
		}

		public string tr(Func<string, string> func)
		{
			return func("");
		}

		[Test]
		public void ChangeBalanceLawyerPersonTest()
		{
			var lp = new Client();
			using (new SessionScope())
			{
				lp = ClientHelper.CreateLaywerPerson();
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

		[Test]
		public void ChangeStatus()
		{
			using (new SessionScope())
			{
				client.Status = Status.Find((uint) StatusType.BlockedAndNoConnected);
				client.Update();
			}
			using (var browser = Open(format))
			{
				Assert.That(browser.SelectList("ChStatus").SelectedItem, Is.EqualTo("Заблокирован"));
				browser.Button("SaveButton").Click();
			}
			using (new SessionScope())
			{
				client.Refresh();
				Assert.That(client.Status.Type, Is.EqualTo(StatusType.BlockedAndNoConnected));
			}
			using (new SessionScope())
			{
				client.Status = Status.Find((uint)StatusType.Worked);
				client.Update();
			}
			using (var browser = Open(format))
			{
				Assert.That(browser.SelectList("ChStatus").SelectedItem, Is.EqualTo("Подключен"));
				browser.SelectList("ChStatus").Select("Заблокирован");
				browser.Button("SaveButton").Click();
				Assert.That(browser.Text, Is.StringContaining("Данные изменены"));
			}
			using (new SessionScope())
			{
				client.Refresh();
				Assert.That(client.Status.Type, Is.EqualTo(StatusType.NoWorked));
			}
		}

		[Test]
		public void ReservTest()
		{
			using (var browser = Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", client.Id)))
			{
				browser.Button("naznach_but").Click();
				browser.RadioButton(Find.ByName("graph_button")).Checked = true;
				browser.Button("reserv_but").Click();
				Assert.IsTrue(browser.Text.Contains("Резерв"));
			}
		}

		[Test]
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

		[Test]
		public void TelephoneTest()
		{
			using (var browser = Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}&filter.EditConnectInfoFlag=True", client.Id))) {
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
				client = Models.Client.Queryable.First(c => c.PhysicalClient != null && Brigad.FindAll().Contains(c.WhoConnected));
				client.AdditionalStatus = null;
				client.Status = Status.Find((uint)StatusType.BlockedAndNoConnected);
				client.UpdateAndFlush();
				format = string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", client.Id);
			}
			using (var browser = Open(format))
			{
				browser.Button("NotPhoned").Click();
				browser.TextField("NotPhoned_textField").AppendText("Тестовое сообщение перезвонить");
				browser.Button("NotPhoned_but").Click();
				using (new SessionScope())
				{
					client.Refresh();
					Assert.That(client.AdditionalStatus.Id, Is.EqualTo((uint)AdditionalStatusType.NotPhoned));
					Assert.That(browser.Text, Is.StringContaining("Тестовое сообщение перезвонить"));
					Assert.That(browser.Text, Is.StringContaining("Неудобно говорить"));
				}
				browser.Button("naznach_but").Click();
				browser.RadioButton(Find.ByName("graph_button")).Checked = true;
				browser.Button("naznach_but_1").Click();
				Thread.Sleep(2000);
				using (new SessionScope())
				{
					client.Refresh();
					Assert.That(client.AdditionalStatus.Id, Is.EqualTo((uint)AdditionalStatusType.AppointedToTheGraph));
					Assert.That(ConnectGraph.Queryable.Where(c => c.Client == client).Count(), Is.EqualTo(1));
				}
				browser.Button(Find.ById("Refused")).Click();
				Thread.Sleep(1000);
				browser.TextField("Refused_textField").AppendText("Тестовое сообщение отказ");
				browser.Button("Refused_but").Click();
				Thread.Sleep(2000);
				using (new SessionScope())
				{
					client.Refresh();
					Assert.That(client.AdditionalStatus.Id, Is.EqualTo((uint)AdditionalStatusType.Refused));
					Assert.That(browser.Text, Is.StringContaining("Тестовое сообщение отказ"));
					Assert.That(browser.Text, Is.StringContaining("Перезвонит сам"));
				}
			}
		}

		[Test]
		public void SaveSwitchForClientTest()
		{
			using (var browser = Open(format))
			{
				var selectList = browser.SelectList(Find.ByName("ConnectInfo.Switch"));
				selectList.SelectByValue(selectList.Options.First(o => o.Text == "Юго-Восточный 10 саt 2950").Value);
				browser.TextField("Port").AppendText("10");
				browser.Button("Submit2").Click();
				Thread.Sleep(500);
				using (new SessionScope())
				{
					endPoint.Refresh();
					Assert.That(endPoint.Port, Is.EqualTo(10));
					Assert.NotNull(endPoint.Switch);
					Assert.That(endPoint.Switch.Name, Is.EqualTo("Юго-Восточный 10 саt  2950"));
				}
			}
		}

		[Test]
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
			using (var browser = Open(format))
			{
				browser.TextField("Surname").Clear();
				browser.TextField("Surname").AppendText("Иванов");
				browser.TextField("Name").Clear();
				browser.TextField("Name").AppendText("Иван");
				browser.TextField("Patronymic").Clear();
				browser.TextField("Patronymic").AppendText("Иванович");
				browser.Button("SaveButton").Click();
				Thread.Sleep(500);
			}
			using (new SessionScope())
			{
				client.Refresh();
				Assert.That(client.Name,
							Is.EqualTo(string.Format("{0} {1} {2}", "Иванов",
													 "Иван", "Иванович")));
			}
		}

		[Test]
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
			using (var browser = Open(format))
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
