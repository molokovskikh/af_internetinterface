using System.Linq;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class InternetInterfaceControllerFixture : ControllerFixture
	{
		protected InternetInterfaceController Controller;

		[ActiveRecord(Schema = "Internet", SchemaAction = "none", Table = "xx")]
		class TestObj
		{
			[OneToOne(PropertyRef = "TestObj")]
			public TestObj link { get; set; }
			[ValidateNonEmpty]
			public string name { get; set; }
		}

		[SetUp]
		public void Setup()
		{
			Controller = new InternetInterfaceController();
			Prepare(Controller);
		}

		[Test]
		public void ValidateDeep()
		{
			//Простые связи hasMany
			var order = new Order();
			var summary = Controller.ValidateDeep(order);
			Assert.True(summary.ErrorsCount > 0);
			order.OrderServices.Add(new OrderService());
			order.OrderServices.First().Cost = 4;
			order.OrderServices.First().Description = "Test";
			summary = Controller.ValidateDeep(order);
			Assert.False(summary.HasError);

			//Проверка учета ленивой загрузки в моделях
			//Если во время дебага в брейкоинтах просматривать ленивые поля, то они будут загруженны
			//и ассерты будут работать не так как ожидалось
			var client = new Client(new PhysicalClient(), new Settings());
			var id = (uint)session.Save(client);
			session.Evict(client);
			session.Evict(client.PhysicalClient);
			client = session.Query<Client>().First(o => o.Id == id);
			Assert.False(NHibernateUtil.IsInitialized(client.PhysicalClient));
			summary = Controller.ValidateDeep(client);
			Assert.False(NHibernateUtil.IsInitialized(client.PhysicalClient));
			var initializeNaturally = client.PhysicalClient.City;
			Assert.True(NHibernateUtil.IsInitialized(client.PhysicalClient));

			//Цикличные ссылки - проверяем StackOverflow
			var obj1 = new TestObj();
			obj1.name = "ds";
			var obj2 = new TestObj();
			obj1.link = obj2;
			obj2.link = obj1;
			summary = Controller.ValidateDeep(obj1);
			Assert.True(summary.HasError);
			obj2.name = "a";
			summary = Controller.ValidateDeep(obj1);
			Assert.False(summary.HasError);
		}
	}
}
