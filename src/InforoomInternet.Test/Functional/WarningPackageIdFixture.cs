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
	public class WarningPackageIdFixture : WatinFixture2
	{
		private Lease _lease;
		private ClientEndpoint _clientEndpoint;
		private Client _client;
		private IpPool _pool;
		private PhysicalClient _physicalClient;
		private Internet _internet;
		private IpTv _ipTv;
		private Tariff _tariff;

		[SetUp]
		public void SetUp()
		{
			_pool = new IpPool { IsGray = true };
			_physicalClient = new PhysicalClient();
			_client = new Client();
			_client.PhysicalClient = _physicalClient;
			_clientEndpoint = new ClientEndpoint();
			_clientEndpoint.Client = _client;
			_lease = new Lease(_clientEndpoint);
			_lease.Pool = _pool;
			_internet = new Internet { HumanName = "internet" };
			_ipTv = new IpTv { HumanName = "iptv" };
			_tariff = new Tariff("testTariff", 100);
			session.SaveMany(_internet, _ipTv, _tariff);
			_client.ClientServices.Add(new ClientService(_client, _internet));
			_client.ClientServices.Add(new ClientService(_client, _ipTv));
			_physicalClient.Tariff = _tariff;
			session.SaveMany(_pool, _physicalClient, _client, _clientEndpoint, _lease);
		}

		[TearDown]
		public void TearDown()
		{
			session.DeleteMany(_lease, _clientEndpoint, _client, _physicalClient, _pool, _tariff, _ipTv, _internet);
		}

		[Test]
		public void No_start_work()
		{
			Open("Main/WarningPackageId");
			AssertText("Для начала работы внесите первый платеж.");
		}

		[Test]
		public void Block_greathet_zero()
		{
			_physicalClient.Balance = 10;
			_client.Disabled = true;
			_client.BeginWork = DateTime.Now;
			session.SaveOrUpdate(_client);
			Open("Main/WarningPackageId");
			AssertText("Доступ в интернет заблокирован за неуплату. Сумма на вашем лицевом счете недостаточна для разблокировки");
		}

		[Test]
		public void Block_leases_zero()
		{
			_physicalClient.Balance = -10;
			_client.Disabled = true;
			_client.BeginWork = DateTime.Now;
			session.SaveOrUpdate(_client);
			Open("Main/WarningPackageId");
			AssertText("Ваша задолженность за оказанные услуги составляет");
		}

		[Test]
		public void Access_connect_if_white_pool_unknown()
		{
			_pool.IsGray = false;
			session.SaveOrUpdate(_pool);
			Open("Main/WarningPackageId");
			Thread.Sleep(1000);
			AssertText("Ждите, идет подключение к интернет");
		}

		[Test]
		public void No_client_test()
		{
			_lease.Endpoint = null;
			session.SaveOrUpdate(_lease);
			Open("Main/WarningPackageId");
			AssertText("Чтобы пользоваться услугами интернет необходимо оставить заявку на подлючение, либо авторизоваться, если вы уже подключены.");
		}
	}
}
