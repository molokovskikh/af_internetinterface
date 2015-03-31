using System;
using System.Linq;
using Inforoom2.Test.Functional.infrastructure;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Client = Inforoom2.Models.Client;

namespace Inforoom2.Test.Functional
{
	[TestFixture]
	public class ClientWriteoffsFixture : BaseFixture
	{
		/// <summary>
		/// Корректное и успешное заполнение заявки
		/// </summary>
		[Test, Description("Успешное платеж без скидки"),Ignore("Переделать, переписать комменты и перемстить в правильную папку")]
		public void GeneratePaymentsAndWriteoffs()
		{

			var client = DbSession.Query<Client>().FirstOrDefault(s => s.PhysicalClient.Patronymic == "без валидной скидки");
			var PrevBalance = client.Balance;
			client.PaidDay = false;
			DbSession.Update(client);

			var bill = GetBilling();
			bill.ProcessWriteoffs();
			DbSession.Refresh(client.PhysicalClient);
			/// 300m - Сумма из плана, процедура GeneratePlans в класса BaseFixture
			Assert.AreEqual( Math.Round(PrevBalance-100m / DateTime.Now.DaysInMonth(),2) , client.Balance);
		}

	}
}