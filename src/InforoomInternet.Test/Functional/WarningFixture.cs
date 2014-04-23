using System;
using NUnit.Framework;

namespace InforoomInternet.Test.Functional
{
	[TestFixture]
	public class WarningFixture : BaseFunctionalFixture
	{
		[Test]
		public void No_start_work()
		{
			Client.Disabled = true;
			Open("Main/Warning");
			AssertText("Для начала работы внесите первый платеж.");
		}

		[Test]
		public void Block_greathet_zero()
		{
			PhysicalClient.Balance = 10;
			Client.Disabled = true;
			Client.BeginWork = DateTime.Now;
			session.Save(Client);
			Open("Main/Warning");
			AssertText("Доступ в интернет заблокирован за неуплату. Сумма на вашем лицевом счете недостаточна для разблокировки");
		}

		[Test]
		public void Block_leases_zero()
		{
			PhysicalClient.Balance = -10;
			Client.Disabled = true;
			Client.BeginWork = DateTime.Now;
			session.Save(Client);
			Open("Main/Warning");
			AssertText("Ваша задолженность за оказанные услуги составляет");
		}
	}
}
