using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.AdminAccount
{
	/// <summary>
	/// Авторизация пользователя на сайте
	/// </summary>
	internal class LoginFixture : AdminAccountFixture
	{
		/// <summary>
		/// Успешная авторизация пользователя
		/// </summary>
		[Test, Description("Успешная авторизация администратора")]
		public void Authorization()
		{
			Css("#username").SendKeys(Employee.Login);
			Css("#password").SendKeys("1234");
			Css(".btn-login").Click();
			AssertText(Employee.Name);
		}
	}
}