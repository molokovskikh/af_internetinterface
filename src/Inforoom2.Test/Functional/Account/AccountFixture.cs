using System.Linq;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace Inforoom2.Test.Functional.Account
{
	/// <summary>
	/// Базовая фикстура для контроллера авторизации
	/// </summary>
	class AccountFixture : BaseFixture
	{
		public IWebElement Login;
		public IWebElement Password;
		public Client Client;

		/// <summary>
		/// Функция заполнения логина и пароля
		/// </summary>
		[SetUp]
		public void Setup()
		{
			Open();
			Client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("нормальный клиент"));
			Login = browser.FindElementByCssSelector("input[id=username]");
			Password = browser.FindElementByCssSelector("input[id=password]");
			Login.SendKeys(Client.Id.ToString());
			Password.SendKeys(DefaultClientPassword);
		}
	}
}