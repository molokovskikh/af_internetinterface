using NUnit.Framework;

namespace Inforoom2.Test.Functional.Account
{
	/// <summary>
	/// Авторизация пользователя на сайте
	/// </summary>
	internal class AccountLoginFixture : AccountFixture
	{
		/// <summary>
		/// Успешная авторизация пользователя и вывод логина авторизованного пользователя
		/// </summary>
		[Test, Description("Успешная авторизация пользователя")]
		public void AuthorizationClient()
		{
			Css(".click").Click();
			AssertText("Бонусные программы");
			AssertText(Client.Surname);
		}

		/// <summary>
		/// При авторизации ввели неправильно логин
		/// </summary>
		[Test, Description("Незаполненно поле логин")]
		public void AuthorizationClientNoLogin()
		{
			Login.Clear();
			browser.FindElementByCssSelector(".click").Click();
			AssertText("Неправильный логин или пароль");
		}

		[Test, Description("Незаполненно поле пароль")]
		public void AuthorizationClientNoPassword()
		{
			Password.Clear();
			browser.FindElementByCssSelector(".click").Click();
			AssertText("Неправильный логин или пароль");
		}

		[Test, Description("Неправильный логин")]
		public void AuthorizationClientWrongLogin()
		{
			Login.SendKeys("1962738");
			browser.FindElementByCssSelector(".click").Click();
			AssertText("Неправильный логин или пароль");
		}

		[Test, Description("Неправильный пароль")]
		public void AuthorizationClientWrongPassword()
		{
			Password.SendKeys("1962738as");
			browser.FindElementByCssSelector(".click").Click();
			AssertText("Неправильный логин или пароль");
		}
	}
}