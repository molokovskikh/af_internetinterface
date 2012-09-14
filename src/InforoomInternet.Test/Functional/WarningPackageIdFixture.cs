using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using NUnit.Framework;
using Test.Support.Web;
using WatiN.Core.Native.Windows;

namespace InforoomInternet.Test.Functional
{
	[TestFixture]
	public class WarningPackageIdFixture : BaseFunctionalFixture
	{
		[Test]
		public void No_start_work()
		{
			Open("Main/WarningPackageId");
			AssertText("Для начала работы внесите первый платеж.");
		}

		[Test]
		public void Block_greathet_zero()
		{
			PhysicalClient.Balance = 10;
			Client.Disabled = true;
			Client.BeginWork = DateTime.Now;
			session.SaveOrUpdate(Client);
			Open("Main/WarningPackageId");
			AssertText("Доступ в интернет заблокирован за неуплату. Сумма на вашем лицевом счете недостаточна для разблокировки");
		}

		[Test]
		public void Block_leases_zero()
		{
			PhysicalClient.Balance = -10;
			Client.Disabled = true;
			Client.BeginWork = DateTime.Now;
			session.SaveOrUpdate(Client);
			Open("Main/WarningPackageId");
			AssertText("Ваша задолженность за оказанные услуги составляет");
		}

		[Test]
		public void Access_connect_if_white_pool_unknown()
		{
			Pool.IsGray = false;
			session.SaveOrUpdate(Pool);
			Open("Main/WarningPackageId");
			Thread.Sleep(1000);
			AssertText("Ждите, идет подключение к интернет");
		}

		[Test]
		public void No_client_test()
		{
			Lease.Endpoint = null;
			session.SaveOrUpdate(Lease);
			Open("Main/WarningPackageId");
			AssertText("Чтобы пользоваться услугами интернет необходимо оставить заявку на подлючение, либо авторизоваться в личном кабинете, если вы уже подключены.");
		}
	}
}
