using System;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Controllers;
using Inforoom2.Models;
using Inforoom2.Test.Functional.Personal;
using Inforoom2.Test.Infrastructure;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.Home
{
	public class CallMeBackOnActionsFixture : PersonalFixture
	{
		[Test, Description("Проверка возможности запросить обратный звонок на всех actios")]
		public void CallMeBackTicket()
		{
			var ass = Assembly.GetAssembly(typeof(Client));
			var controllers = ass.GetTypes().Where(i => i.IsSubclassOf(typeof(BaseController))).ToList();
			foreach (var controller in controllers) {
				var methods = controller.GetMethods();
				var actions = methods.Where(i => i.ReturnType == typeof(ActionResult)).ToList();
				foreach (var action in actions) {
					if (!Attribute.IsDefined(action, typeof(HttpPostAttribute)) && action.GetParameters().Length == 0
						&& action.Name != "Logout" && controller.Name != "BussinessController" && controller.Name != "TestSpeedController" && action.Name != "Playlist")
					{
						var name = controller.Name.Replace("Controller", "");
						Open("Account/Login");
						Assert.That(browser.PageSource, Is.StringContaining("Вход в личный кабинет"));
						var nameLogin = browser.FindElementByCssSelector(".Account.Login input[name=username]");
						var password = browser.FindElementByCssSelector(".Account.Login input[name=password]");
						nameLogin.SendKeys(Client.Id.ToString());
						password.SendKeys("password");
						browser.FindElementByCssSelector(".Account.Login input[type=submit]").Click();

						Open("{0}/{1}", name, action.Name);
						browser.FindElementByCssSelector(".call").Click();
						var nameClient = browser.FindElementByCssSelector("input[id=callMeBackTicket_Name]");
						var phone = browser.FindElementByCssSelector("input[id=callMeBackTicket_PhoneNumber]");
						var comment = browser.FindElementByName("callMeBackTicket.Text");
						nameClient.SendKeys("Иван Петров");
						phone.SendKeys("8556478970");
						comment.SendKeys("my question");
						browser.FindElementByCssSelector(".wrap .contacting").Click();
						//browser.FindElementByCssSelector("input[class=.wrap contacting]").Click();
						var callMeBackTicket = DbSession.Query<CallMeBackTicket>().FirstOrDefault(c => c.PhoneNumber == "8556478970");
						Assert.NotNull(callMeBackTicket);
						AssertText("Заявка отправлена. В течении дня вам перезвонят.");
					}
				}
			}
		}
	}
}