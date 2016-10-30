﻿using System;
using InternetInterface.Models;
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

		[Test, Ignore("Функционал перенесен, условия отработки изменились (флаг Disabled, при BlockedForRepair, выставлять не нужно )")]
		public void Block_for_repair()
		{
			Client.SetStatus(StatusType.BlockedForRepair, session);
			Open("Main/Warning");
			AssertText("Доступ в интернет заблокирован из-за проведения работ по сервисной заявке, на время работ тарификация остановлена");
			Click("Продолжить");
			session.Refresh(Client);
			Assert.AreEqual(StatusType.Worked, Client.Status.Type);
		}

		[Test]
		public void No_passport_data()
		{
			Client.BeginWork = DateTime.Now.AddDays(-10);
			var endpoint = new ClientEndpoint() {IsEnabled = true, Client = Client };
			Client.Endpoints.Add(endpoint);
			session.Save(endpoint);
			Client.ShowBalanceWarningPage = true;
			PhysicalClient.Balance = 500;
			session.Save(Client);
			Open("Main/Warning");
			AssertText("При регистрации в сети Инфорум Вами были указаны некорректные паспортные данные.");
		}
	}
}
