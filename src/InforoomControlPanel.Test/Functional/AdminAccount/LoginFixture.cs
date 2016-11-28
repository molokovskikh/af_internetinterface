using System;
using System.Threading;
using Common.Tools;
using Inforoom2.Models;
using NHibernate.Linq;
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
			Css(".entypo-logout.right").Click();
			Css("#username").SendKeys(Employee.Login);
			Css("#password").SendKeys("1234");
			Css(".btn-login").Click();
			AssertText(Employee.Name);
        }
        /// <summary>
        /// Успешная авторизация пользователя
        /// </summary>
        [Test, Description("Проверка завершения сессии (logout) с заданной длительностью")]
        public void AuthorizationForAMinute()
        {
	        //длительность сессии 1 мин
	        try {
		        Employee.SessionDurationMinutes = 1;
		        DbSession.Save(Employee);
		        DbSession.Flush();
		        //перевод времени время, чтоб не ждать минуту
		        SystemTime.Now = () => DateTime.Now.AddSeconds(-40);
		        UpdateDriverSideSystemTime();

		        //авторизация пользователя   
		        Css(".entypo-logout.right").Click();
		        Css("#username").SendKeys(Employee.Login);
		        Css("#password").SendKeys("1234");
		        Css(".btn-login").Click();
		        AssertText(Employee.Name);

		        //обновление страницы (пока не выкинет, не истечет время)
		        for (int i = 0; i < 10; i++) {
			        Open("AdminAccount");
			        if (browser.FindElementsByCssSelector("#username").Count > 0) {
				        break;
			        }
			        Thread.Sleep(TimeSpan.FromSeconds(2));
		        }
		        //сессия должна закончится, пользователя должно переадресовать на страницу авторизации
		        WaitForText("Добро пожаловать. Вам необходимо ввести пароль", 10);

		        //авторизация пользователя  
		        Css("#username").SendKeys(Employee.Login);
		        Css("#password").SendKeys("1234");
		        Css(".btn-login").Click();

		        //из-за разницы во времени, пользователя должно сразу выкидывать (результатная длительность сессии пара секунд)
		        WaitForText("Добро пожаловать. Вам необходимо ввести пароль", 10);
		        //обычное время
		        SystemTime.Now = () => DateTime.Now;
		        UpdateDriverSideSystemTime();

		        //авторизация пользователя  
		        Css("#username").SendKeys(Employee.Login);
		        Css("#password").SendKeys("1234");
		        Css(".btn-login").Click();

		        //обновление страницы (пока не выкинет, а выкинуть не должно)
		        for (int i = 0; i < 5; i++) {
			        Open("AdminAccount");
			        if (browser.FindElementsByCssSelector("#username").Count > 0) {
				        break;
			        }
		        }
		        //пользователь остался авторизованным
		        AssertText(Employee.Name);
	        } finally {
		        //длительность сессии 1 мин
		        Employee.SessionDurationMinutes = 0;
		        DbSession.Save(Employee);
		        DbSession.Flush();
	        }
        }
    }
}