using System;
using System.Linq;
using Common.Tools;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class LawyerPersonFixture : AcceptanceFixture
	{
		private Client laywerPerson;

		[SetUp]
		public void Setup()
		{
			laywerPerson = ClientHelper.CreateLaywerPerson();
			laywerPerson.LawyerPerson.Balance = -100000;
			session.Save(laywerPerson);

			defaultUrl = laywerPerson.Redirect();
		}

		private void AddOrderService()
		{
			Click("Добавить");
			browser.FindElementByName("order.OrderServices[1].Description").SendKeys("Тестовый заказ");
			browser.FindElementByName("order.OrderServices[1].Cost").SendKeys("1000");
			var checkbox = browser.FindElementByName("order.OrderServices[1].IsPeriodic");
			if(!checkbox.Selected)
				checkbox.Click();
		}

		[Test]
		public void Activate_disable_block()
		{
			Open(laywerPerson.Redirect());
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
			Open(laywerPerson.Redirect());
			Click("Добавить заказ");
			Css("#SelectSwitches").SelectByValue(commutator.Id.ToString());
			browser.FindElementById("Port").Clear();
			browser.FindElementById("Port").SendKeys("1");
			browser.FindElementByName("order.Number").Clear();
			browser.FindElementByName("order.Number").SendKeys("99");
			AddOrderService();
			Click("Сохранить");
			var order = session.QueryOver<Order>().Where(o => o.Client == laywerPerson).SingleOrDefault();
			Assert.That(order.EndPoint.Port, Is.EqualTo(1));
			Assert.That(order.Number, Is.EqualTo(99));
			Assert.That(order.OrderServices.Count, Is.EqualTo(1));
			Assert.That(order.OrderServices[0].IsPeriodic, Is.True);
			Assert.That(order.OrderServices[0].Description, Is.EqualTo("Тестовый заказ"));
			Assert.That(order.OrderServices[0].Cost, Is.EqualTo(1000));
		}

		[Test(Description = "Проверяет создание нового заказа без точки подключения")]
		public void AddOrderWithoutEndPointTest()
		{
			Open(laywerPerson.Redirect());
			Click("Добавить заказ");
			browser.FindElementByName("order.Number").Clear();
			browser.FindElementByName("order.Number").SendKeys("99");
			var checkbox = browser.FindElementByName("withoutEndpoint");
			if(!checkbox.Selected)
				checkbox.Click();

			AddOrderService();

			Click("Сохранить");
			var order = session.QueryOver<Order>().Where(o => o.Client == laywerPerson).SingleOrDefault();
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
			Open(laywerPerson.Redirect());
			Click("Добавить заказ");
			browser.FindElementByName("order.Number").Clear();
			browser.FindElementByName("order.Number").SendKeys("99");
			browser.FindElementById("order_BeginDate").Clear();
			var checkbox = browser.FindElementByName("withoutEndpoint");
			if(!checkbox.Selected)
				checkbox.Click();

			AddOrderService();
			Click("Сохранить");
			AssertText("Это поле необходимо заполнить.");
		}

		[Test(Description = "Проверяет редактирование заказа")]
		public void EditOrderTest()
		{
			var order = new Order {
				BeginDate = SystemTime.Now().AddDays(1),
				EndDate = SystemTime.Now().AddDays(10),
				Client = laywerPerson,
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
			Open(laywerPerson.Redirect());

			AssertText("заказ № 1");
			Click("<Подключенные услуги(1):");
			AssertText("Периодичная");
			AssertText("Тестовая услуга");
			Css("#EditButton" + order.Id).Click();
			browser.FindElementByName("order.Number").Clear();
			browser.FindElementByName("order.Number").SendKeys("99");

			var checkbox = browser.FindElementByName("order.OrderServices[0].IsPeriodic");
			if(checkbox.Selected)
				checkbox.Click();
			Click("Сохранить");
			session.Clear();

			var savedOrder = session.QueryOver<Order>().Where(o => o.Client == laywerPerson).SingleOrDefault();
			Assert.That(savedOrder.Number, Is.EqualTo(99));
			Assert.That(savedOrder.EndPoint, Is.Null);
			Assert.That(savedOrder.OrderServices[0].IsPeriodic, Is.False);
		}

		[Test(Description = "Проверяет, что кнопка редактирования недоступна для не нового заказа")]
		public void EditDisableForNotNewOrder()
		{
			var order = new Order {
				Client = laywerPerson,
				BeginDate = DateTime.Now.AddMonths(-2)
			};
			session.Save(order);
			Open(laywerPerson.Redirect());
			AssertText("заказ № " + order.Number);
			Assert.That(IsPresent("#EditButton" + order.Id), Is.False);
		}

		[Test(Description = "Проверяет отображение заказа в архиве")]
		public void ArchiveOrderTest()
		{
			var order = new Order {
				Client = laywerPerson,
				EndDate = SystemTime.Now().AddDays(7),
				Disabled = true,
				Number = 666
			};
			session.Save(order);
			order = new Order {
				Client = laywerPerson,
				BeginDate = SystemTime.Now().AddDays(-7),
				EndDate = SystemTime.Now().AddDays(-1),
				Disabled = true,
				Number = 777
			};
			session.Save(order);
			Open(laywerPerson.Redirect());
			Click("Архив заказов");
			AssertText("заказ № 666");
			AssertText("заказ № 777");
		}

		[Test(Description = "Проверяет закрытие заказа"), Ignore("Non Clicable - наверное проблемы с версткой")]
		public void CloseOrderTest()
		{
			var order = new Order {
				Client = laywerPerson,
				EndDate = SystemTime.Now().AddDays(7)
			};
			session.Save(order);
			Open(laywerPerson.Redirect());
			AssertText("заказ № " + order.Number);
			browser.Manage().Window.Maximize();
			Css("#closeOrderButton" + order.Id).Click();
			//WaitForText("Вы уверены");
			Click("Закрыть");
			session.Clear();
			var savedOrder = session.QueryOver<Order>().Where(o => o.Client == laywerPerson).SingleOrDefault();
			Assert.That(savedOrder.Disabled, Is.True);
		}

		[Test(Description = "Проверяет корректное удаление услуги")]
		public void DeleteOrderServiceTest()
		{
			var order = new Order {
				Client = laywerPerson,
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
			Open(laywerPerson.Redirect());
			AssertText("заказ № " + order.Number);
			AssertText("<Подключенные услуги(3)");
			Css("#EditButton" + order.Id).Click();
			browser.Manage().Window.Maximize();
			ClickLink("Удалить");
			Click("Сохранить");
			session.Clear();
			var savedOrder = session.QueryOver<Order>().Where(o => o.Client == laywerPerson).SingleOrDefault();
			Assert.That(savedOrder.OrderServices.Count, Is.EqualTo(2));
		}
	}
}