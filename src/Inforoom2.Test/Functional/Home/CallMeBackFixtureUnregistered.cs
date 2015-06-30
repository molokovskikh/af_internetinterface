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
			name.SendKeys("Иван Петров");
			phone.SendKeys("8556478970");
			comment.SendKeys("my question");
			browser.FindElementByCssSelector(".contacting").Click();
			var callMeBackTicket = DbSession.Query<CallMeBackTicket>().FirstOrDefault(c => c.PhoneNumber == "8556478970"); 
			Assert.IsNull(callMeBackTicket); 
		}
		//TODO: написать тест для Проверка на необходимость ввода капчи в запросе на обратный звонок у незарегистрированного пользователя, при *наличии* капчи
	}
}