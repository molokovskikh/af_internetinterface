using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Castle.MonoRail.TestSupport;
using InforoomInternet.Controllers;
using NUnit.Framework;

namespace InforoomInternet.Test.Integration
{
	[TestFixture]
	class PrivateOfficeTest : BaseControllerTest 
	{
		private PrivateOfficeController controller;

		[SetUp]
		public void Init()
		{
			controller = new PrivateOfficeController();
			PrepareController(controller);
		}

		[Test]
		public void ProvateOffice()
		{
			using (new SessionScope())
			{
				var filter = new AccessFilter();
				Request.UserHostAddress = "192.168.200.1";
				Assert.IsTrue(filter.Perform(ExecuteWhen.BeforeAction, controller.Context, controller, controller.ControllerContext));
			}
		}
	}
}
