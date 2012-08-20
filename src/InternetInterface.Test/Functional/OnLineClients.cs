using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Test.Support.Web;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class OnLineClients : WatinFixture2
	{
		[SetUp]
		public void SetUp()
		{
			Open("Switches/OnLineClient?filter.Zone=4");
		}

		[Test]
		public void Base_view_test()
		{
			AssertText("Параментры фильтрации");
			AssertText("Текст для поиска");
			AssertText("Выберите зону для просмотра");
			AssertText("Выберите коммутатор");
			AssertText("Физические лица");
			AssertText("Юридические лица");
			AssertText("Все");
			Click("Показать");
			AssertText("Параментры фильтрации");
		}
	}
}