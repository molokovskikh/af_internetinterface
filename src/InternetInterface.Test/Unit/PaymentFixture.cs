using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class PaymentFixture
	{
		[Test]
		public void Sum_to_literal()
		{
			var payment = new Payment();
			payment.Sum = 1000;
			Assert.AreEqual("1000 (Одна тысяча) рублей 00 копеек", payment.SumToLiteral());
		}
	}
}