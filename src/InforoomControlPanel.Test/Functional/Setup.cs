using CassiniDev;
using Inforoom2.Test;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InforoomControlPanel.Test.Functional
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