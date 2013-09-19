using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional.Selenium
{
	[TestFixture]
	public class OnLineClients : SeleniumFixture
	{
		[SetUp]
		public void SetUp()
		{
			Open("Switches/OnLineClient?filter.Zone=4");
		}

		[Test]
		public void Base_view_test()
		{
			AssertText("Параметры фильтрации");
			AssertText("Текст для поиска");
			AssertText("Выберите зону для просмотра");
			AssertText("Выберите коммутатор");
			AssertText("Физические лица");
			AssertText("Юридические лица");
			AssertText("Все");
			Click("Показать");
			AssertText("Параметры фильтрации");
		}
	}
}
