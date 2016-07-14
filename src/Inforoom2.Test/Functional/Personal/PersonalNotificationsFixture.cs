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
			DbSession.Save(Client);
			DbSession.Flush();
			Open("Personal/Notifications");
			var phone = browser.FindElementByCssSelector("input[id=contact_ContactString]");
			phone.SendKeys("test@mail.test");
			browser.FindElementByCssSelector(".button").Click();
			AssertText("На указанный почтовый адрес выслано сообщение с подтверждением.");
		}

		/// <summary>
		/// Подписаться на уведомления не удалось,номер телефона не ввели
		/// </summary>
		[Test, Description("Не введен контакт")]
		public void NotificationsNot()
		{
			Open("Personal/Notifications");
			var phone = browser.FindElementByCssSelector("input[id=contact_ContactString]");
			phone.SendKeys("");
			browser.FindElementByCssSelector(".button").Click();
			AssertText("Введите контакт");
		}

		/// <summary>
		/// Подписаться на уведомления не удалось,номер телефона ввели неверно
		/// </summary>
		[Test, Description("Неправильно введен контакт")]
		public void NotificationsWrong()
		{
			Open("Personal/Notifications");
			var phone = browser.FindElementByCssSelector("input[id=contact_ContactString]");
			phone.SendKeys("test@mailtest");
			browser.FindElementByCssSelector(".button").Click();
			AssertText("Адрес email указан неверно");
		}

		/// <summary>
		/// Подписаться на уведомления не удалось,номер телефона ввели неверно
		/// </summary>
		[Test, Description("Неправильно введен контакт")]
		public void NotificationsWrongTwo()
		{
			Open("Personal/Notifications");
			var phone = browser.FindElementByCssSelector("input[id=contact_ContactString]");
			phone.SendKeys("testmail.test");
			browser.FindElementByCssSelector(".button").Click();
			AssertText("Адрес email указан неверно");
		}
	}
}