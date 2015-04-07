using NUnit.Framework;

namespace Inforoom2.Test.Functional.Account
{
	internal class AccountLogoutFixture : AccountFixture
	{
		[Test, Description("Выход пользователя из ЛК")]
		public void Logout()
		{
			Css(".click").Click();
			AssertText(Client.Surname);
			Css(".logon a").Click();
			AssertNoText(Client.Surname);
		}
	}
}