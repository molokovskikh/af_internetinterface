using Inforoom2.Test.Infrastructure;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.Personal
{
	/// <summary>
	/// Подписка на уведомления
	/// </summary>
	internal class PersonalNotificationsFixture : PersonalFixture
	{
		/// <summary>
		/// Успешная подписка на уведомления
		/// </summary>
		[Test, Description("Успешная подписка на уведомления")]
		public void Notifications()
		{
			Open("Personal/Notifications");
			var phone = browser.FindElementByCssSelector("input[id=contact_ContactString]");
			phone.SendKeys("8556478974");
			browser.FindElementByCssSelector(".button").Click();
			AssertText("Вы успешно подписались на смс рассылку");
		}

		/// <summary>
		/// Подписаться на уведомления не удалось,номер телефона не ввели
		/// </summary>
		[Test, Description("Не введен номер телефона")]
		public void NotificationsNot()
		{
			Open("Personal/Notifications");
			var phone = browser.FindElementByCssSelector("input[id=contact_ContactString]");
			phone.SendKeys("");
			browser.FindElementByCssSelector(".button").Click();
			AssertText("Введите номер телефона");
		}

		/// <summary>
		/// Подписаться на уведомления не удалось,номер телефона ввели неверно
		/// </summary>
		[Test, Description("Неправильно введен номер телефона")]
		public void NotificationsWrong()
		{
			Open("Personal/Notifications");
			var phone = browser.FindElementByCssSelector("input[id=contact_ContactString]");
			phone.SendKeys("4663767478239463");
			browser.FindElementByCssSelector(".button").Click();
			AssertText("Номер телефона введен неправильно");
		}

		/// <summary>
		/// Подписаться на уведомления не удалось,номер телефона ввели неверно
		/// </summary>
		[Test, Description("Неправильно введен номер телефона")]
		public void NotificationsWrongTwo()
		{
			Open("Personal/Notifications");
			var phone = browser.FindElementByCssSelector("input[id=contact_ContactString]");
			phone.SendKeys("964-589-5414");
			browser.FindElementByCssSelector(".button").Click();
			AssertText("Номер телефона введен неправильно");
		}
	}
}