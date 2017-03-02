using CassiniDev;
using NUnit.Framework;
using Test.Support.Selenium;

namespace Inforoom2.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{
		private Server _webServer;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			//Все опасные функции, должны быть вызванны до этого момента, так как исключения в сетапе
			//оставляют невысвобожденные ресурсы браузера и веб сервера
			MySeleniumFixture.GlobalSetup();
			_webServer = MySeleniumFixture.StartServer();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			MySeleniumFixture.GlobalTearDown();
			_webServer.ShutDown();
		}
	}
}