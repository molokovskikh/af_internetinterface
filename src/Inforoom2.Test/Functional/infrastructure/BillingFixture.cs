using System;
using System.Linq;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;
using Client = Inforoom2.Models.Client;

namespace Inforoom2.Test.Functional.infrastructure
{
	[TestFixture]
	public class BillingFixture : BaseFixture
	{
		[Test, Description("Проверка учета скидки по факту списания у клиента 'c тарифом, игнорирующим скидку' ")]
		public void ClientIgnoreDiscount()
		{
			var client = DbSession.Query<Client>().FirstOrDefault(s => s.PhysicalClient.Patronymic == "c тарифом, игнорирующим скидку");
			var PrevBalance = client.Balance;
			client.PaidDay = false;
			DbSession.Update(client);
			// Списание денег
			var bill = GetBilling();
			bill.ProcessWriteoffs();
			DbSession.Refresh(client.PhysicalClient);
			// Получаем количество дней списания так, как это сделано в InternetInterface.Models.Client.GetInterval()
			var DaysInMonthLikeInBilling = (((DateTime)client.RatedPeriodDate).AddMonths(1) - (DateTime)client.RatedPeriodDate).Days + client.DebtDays;
			// 300m - Сумма из плана, процедура GeneratePlans в класса BaseFixture
			Assert.AreEqual(Math.Round(PrevBalance - 100m / DaysInMonthLikeInBilling, 2), client.Balance);
		}
	}
}