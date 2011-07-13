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
	class UserInfoFixture : WatinFixture
	{
        public string format;
	    public PhysicalClients physicalClient;
        public Clients client;
        public ClientEndpoints endPoint;

        public UserInfoFixture()
        {
          using (new SessionScope())
          {
              new Status {Name = "ConnectAndBlocked"}.Save();
                physicalClient = new PhysicalClients {
                                                         Name = "Alexandr",
                                                         Surname = "Zolotarev",
                                                         Patronymic = "Alekseevich",
                                                         Street = "Stud",
                                                         House = "12",
                                                         Apartment = "1",
                                                         Entrance = "2",
                                                         Floor = "2",
                                                         PhoneNumber = "8-900-200-80-80",
                                                         Balance = 0,
                                                         Tariff = Tariff.Queryable.First(),
                                                         CaseHouse = "sdf",
                                                         City = "bebsk",
                                                         Email = "test@test.ru",
                                                         
                                                     };
                physicalClient.SaveAndFlush();
                client = new Clients {
                                         PhysicalClient = physicalClient,
                                         BeginWork = null,
                                         Name =
                                             string.Format("{0} {1} {2}", physicalClient.Surname, physicalClient.Name,
                                                           physicalClient.Patronymic),
                                        Status = Status.FindFirst()
                                     };
                client.SaveAndFlush();
                endPoint = new ClientEndpoints {
                                                   Client = client,
                                               };
                endPoint.SaveAndFlush();
                format = string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}&filter.EditingConnect=true&filter.Editing=true",
                                       client.Id);
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
                Console.WriteLine(browser.Html);
                Assert.IsTrue(browser.Text.Contains("Резерв"));
            }
        }

	    [Test]
        public void AdditionalStatusTest()
        {
            using (new SessionScope())
            {
                client = Clients.Queryable.Where(
    c => c.PhysicalClient != null && Brigad.FindAll().Contains(c.WhoConnected)).
    First();
                client.AdditionalStatus = null;
                client.Status = Status.Find((uint)StatusType.BlockedAndNoConnected);
                client.UpdateAndFlush();
                format = string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", client.Id);
            }
            using (var browser = Open(format))
            {
                //using (new SessionScope())
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
                        GroupBy(i => i.EndpointId).Where(g => g.Count() > 20).First().Select(
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
                Console.WriteLine(format);
                //Console.WriteLine(browser.Html);
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
                    tariffList.Options.Where(s => s.Value != tariffList.SelectedOption.Value).First().Value);
                var statusList = browser.SelectList("ChStatus");
                browser.SelectList("ChStatus").SelectByValue(
                    statusList.Options.Where(s => s.Value != statusList.SelectedOption.Value).First().Value);
                browser.Button("SaveButton").Click();
                Assert.That(browser.Text, Is.StringContaining("Данные изменены"));
            }
        }
	}
}
