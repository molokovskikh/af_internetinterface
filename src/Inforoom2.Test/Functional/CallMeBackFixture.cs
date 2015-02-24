using System.Linq;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional
{
	public class CallMeBackFixture : BaseFixture
	{
		[Test, Description("Проверка возможности запросить обратный звонок, так же проверяется forwarding")]
		public void CallMeBackTicket()
		{
			Open("Faq");
			browser.FindElementByCssSelector(".call").Click();
			var name = browser.FindElementByCssSelector("input[id=callMeBackTicket_Name]");
			var phone = browser.FindElementByCssSelector("input[id=callMeBackTicket_PhoneNumber]");
			name.SendKeys("Иван Петров");
			phone.SendKeys("855647897");
			browser.FindElementByCssSelector(".contacting").Click();
			var callMeBackTicket = DbSession.Query<CallMeBackTicket>().FirstOrDefault(c => c.PhoneNumber == "855647897");
			Assert.NotNull(callMeBackTicket);
			var count = DbSession.Query<CallMeBackTicket>().ToList().Count;
			Assert.That(count,Is.EqualTo(1),"Создается только одна заявка за раз");
			AssertText("Задать вопрос:");
		}
	}
}