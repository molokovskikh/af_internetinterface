using System.Linq;
using System.Web;
using Inforoom2.Models;
using Inforoom2.Test.Functional.Personal;
using Inforoom2.Test.Infrastructure;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.Home
{
	public class CallMeBackFixtureUnregistered : BaseFixture
	{
		[Test, Description("Проверка на необходимость ввода капчи в запросе на обратный звонок у незарегистрированного пользователя, при отсуствии капчи")]
		public void CallMeBackTicket()
		{
			Open("Faq");
			browser.FindElementByCssSelector(".call").Click();
			var name = browser.FindElementByCssSelector("input[id=callMeBackTicket_Name]");
			var phone = browser.FindElementByCssSelector("input[id=callMeBackTicket_PhoneNumber]");
			var comment = browser.FindElementByName("callMeBackTicket.Text");
			name.SendKeys("Ёся Петров");
			phone.SendKeys("2222222222");
			comment.SendKeys("my bad question");
			browser.FindElementByCssSelector(".contacting").Click();
			var callMeBackTicket = DbSession.Query<CallMeBackTicket>().FirstOrDefault(c => c.PhoneNumber == "2222222222"); 
			Assert.IsNull(callMeBackTicket); 
		}
		//TODO: написать тест для Проверка на необходимость ввода капчи в запросе на обратный звонок у незарегистрированного пользователя, при *наличии* капчи
	}
}