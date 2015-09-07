using CassiniDev;
using NUnit.Framework;
using Test.Support.Selenium;

namespace Inforoom2.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{
		private Server _webServer;

		[SetUp]
		public void SetupFixture()
		{
			//Все опасные функции, должны быть вызванны до этого момента, так как исключения в сетапе
			//оставляют невысвобожденные ресурсы браузера и веб сервера
			MySeleniumFixture.GlobalSetup();
			_webServer = MySeleniumFixture.StartServer();
		}

		[TearDown]
		public void TeardownFixture()
		{
			MySeleniumFixture.GlobalTearDown();
			_webServer.ShutDown();
		}
	}
}