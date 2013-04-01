using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common.Tools;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Web;
using WatiN.Core;
using WatiN.Core.Native.Windows;

namespace InternetInterface.Test.Functional
{
	public class LawyerPersonFixture : WatinFixture2
	{
		private Client client;

		[SetUp]
		public void Setup()
		{
			client = ClientHelper.CreateLaywerPerson();
			client.LawyerPerson.Balance = -100000;
			session.Save(client);
		}

		[Test]
		public void Activate_disable_block()
		{
			Open(client.Redirect());
			Click("Управление услугами");
			Click("Отключить блокировки");
			Click("Активировать");
			AssertText("Услуга \"Отключить блокировки\" активирована");
			Click("Управление услугами");
			Click("Отключить блокировки");
			Click("Деактивировать");
			AssertText("Услуга \"Отключить блокировки\" деактивирована");
		}

		[Test(Description = "Проверяет создание нового заказа с точкой подключения")]
		public void AddOrderWithEndPointTest()
		{
			var package = new PackageSpeed {
				Description = "10мб",
				Speed = 10
			};
			session.Save(package);
			var commutator = new NetworkSwitch("Тестовый коммутатор", session.Query<Zone>().First());
			session.Save(commutator);
			Open(client.Redirect());
			Click("Добавить заказ");
			browser.SelectList("SelectSwitches").SelectByValue(commutator.Id.ToString());
			browser.TextField("Port").AppendText("1");
			browser.TextField(Find.ByName("ConnectSum")).AppendText("100");
			browser.TextField(Find.ByName("order.Number")).TypeText("99");
			AddOrderService();
			Click("Сохранить");
			var order = session.QueryOver<Orders>().Where(o => o.Client == client).SingleOrDefault();
			Assert.That(order.EndPoint.Port, Is.EqualTo(1));
			Assert.That(order.EndPoint.PayForCon.Sum, Is.EqualTo(100));
			Assert.That(order.Number, Is.EqualTo(99));
			Assert.That(order.OrderServices.Count, Is.EqualTo(1));
			Assert.That(order.OrderServices[0].IsPeriodic, Is.True);
			Assert.That(order.OrderServices[0].Description, Is.EqualTo("Тестовый заказ"));
			Assert.That(order.OrderServices[0].Cost, Is.EqualTo(1000));
		}


		[Test(Description = "Проверяет создание нового заказа без точки подключения")]
		public void AddOrderWithoutEndPointTest()
		{
			Open(client.Redirect());
			Click("Добавить заказ");
			browser.TextField(Find.ByName("order.Number")).TypeText("99");
			browser.CheckBox(Find.ByName("withoutEndpoint")).Checked = true;
			AddOrderService();

			Click("Сохранить");
			var order = session.QueryOver<Orders>().Where(o => o.Client == client).SingleOrDefault();
			Assert.That(order.EndPoint, Is.Null);
			Assert.That(order.Number, Is.EqualTo(99));
			Assert.That(order.OrderServices.Count, Is.EqualTo(1));
			Assert.That(order.OrderServices[0].IsPeriodic, Is.True);
			Assert.That(order.OrderServices[0].Description, Is.EqualTo("Тестовый заказ"));
			Assert.That(order.OrderServices[0].Cost, Is.EqualTo(1000));
		}

		[Test(Description = "Проверяет обязательность заполнения поля с датой начала заказа")]
		public void AddOrderWithNullBeginDateTest()
		{
			Open(client.Redirect());
			Click("Добавить заказ");
			browser.TextField(Find.ByName("order.Number")).TypeText("99");
			browser.TextField("order_BeginDate").Value = "";
			browser.CheckBox(Find.ByName("withoutEndpoint")).Checked = true;
			AddOrderService();
			Click("Сохранить");
			AssertText("Это поле необходимо заполнить.");
		}

		private void AddOrderService()
		{
			Click("Добавить");
			browser.TextField(Find.ByName("order.OrderServices[1].Description")).AppendText("Тестовый заказ");
			browser.TextField(Find.ByName("order.OrderServices[1].Cost")).AppendText("1000");
			browser.CheckBox(Find.ByName("order.OrderServices[1].IsPeriodic")).Checked = true;
		}

		[Test(Description = "Проверяет редактирование заказа")]
		public void EditOrderTest()
		{
			var order = new Orders {
				BeginDate = SystemTime.Now().AddDays(1),
				EndDate = SystemTime.Now().AddDays(10),
				Client = client,
				Number = 1
			};
			session.Save(order);
			var orderService = new OrderService {
				Order = order,
				Description = "Тестовая услуга",
				Cost = 13,
				IsPeriodic = true
			};
			session.Save(orderService);
			Open(client.Redirect());
			AssertText("заказ № 1");
			Click("<Подключенные услуги(1):");
			AssertText("Периодичная");
			AssertText("Тестовая услуга");
			browser.Button("EditButton" + order.Id).Click();
			browser.TextField(Find.ByName("order.Number")).TypeText("99");
			browser.CheckBox(Find.ByName("order.OrderServices[0].IsPeriodic")).Checked = false;
			Click("Сохранить");
			session.Clear();
			var savedOrder = session.QueryOver<Orders>().Where(o => o.Client == client).SingleOrDefault();
			Assert.That(savedOrder.Number, Is.EqualTo(99));
			Assert.That(savedOrder.EndPoint, Is.Null);
			Assert.That(savedOrder.OrderServices[0].IsPeriodic, Is.False);
		}

		[Test(Description = "Проверяет, что кнопка редактирования недоступна для не нового заказа")]
		public void EditDisableForNotNewOrder()
		{
			var order = new Orders {
				Client = client
			};
			session.Save(order);
			Open(client.Redirect());
			AssertText("заказ № " + order.Number);
			var button = browser.Buttons.Where(b => b.Id == "EditButton" + order.Id);
			Assert.That(button.Count(), Is.EqualTo(0));
		}

		[Test(Description = "Проверяет закрытие заказа")]
		public void CloseOrderTest()
		{
			var order = new Orders {
				Client = client,
				EndDate = SystemTime.Now().AddDays(7)
			};
			session.Save(order);
			Open(client.Redirect());
			AssertText("заказ № " + order.Number);
			browser.Button("closeOrderButton" + order.Id).Click();
			browser.WaitUntilContainsText("Вы уверены");
			Click("Закрыть");
			session.Clear();
			var savedOrder = session.QueryOver<Orders>().Where(o => o.Client == client).SingleOrDefault();
			Assert.That(savedOrder.Disabled, Is.True);
		}

		[Test(Description = "Проверяет корректное удаление услуги")]
		public void DeleteOrderServiceTest()
		{
			var order = new Orders {
				Client = client,
				BeginDate = SystemTime.Now().AddDays(1),
				EndDate = SystemTime.Now().AddDays(7)
			};
			session.Save(order);
			for(int i = 1; i < 4; i++) {
				var orderService = new OrderService {
					Order = order,
					Description = "Услуга" + i,
					Cost = i
				};
				session.Save(orderService);
			}
			Open(client.Redirect());
			AssertText("заказ № " + order.Number);
			AssertText("<Подключенные услуги(3)");
			browser.Button("EditButton" + order.Id).Click();
			browser.Link(Find.ByText("Удалить")).Click();
			Click("Сохранить");
			session.Clear();
			var savedOrder = session.QueryOver<Orders>().Where(o => o.Client == client).SingleOrDefault();
			Assert.That(savedOrder.OrderServices.Count, Is.EqualTo(2));
		}

		[Test(Description = "Проверяет отображение заказа в архиве")]
		public void ArchiveOrderTest()
		{
			var order = new Orders {
				Client = client,
				EndDate = SystemTime.Now().AddDays(7),
				Disabled = true,
				Number = 666
			};
			session.Save(order);
			order = new Orders {
				Client = client,
				BeginDate = SystemTime.Now().AddDays(-7),
				EndDate = SystemTime.Now().AddDays(-1),
				Disabled = true,
				Number = 777
			};
			session.Save(order);
			Open(client.Redirect());
			Click("Архив заказов");
			AssertText("заказ № 666");
			AssertText("заказ № 777");
		}
	}
}