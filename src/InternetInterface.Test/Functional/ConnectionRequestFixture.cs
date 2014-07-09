using System;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class ConnectionRequestFixture : HeadlessFixture
	{
		[Test]
		public void Create_request()
		{
			Open();
			Click("Регистрация");
			AssertText("Регистрация новой заявки на подключение");
			Input("request_ApplicantName", "Ребкин Вадим Леонидович");
			Input("request_ApplicantPhoneNumber", "8-473-606-20-00");
			Input("request_Street", "Суворова");
			Input("request_House", "1");
			Input("request_Apartment", "1");
			ClickButton("Сохранить");
			AssertText("Сохранено");
		}
	}
}