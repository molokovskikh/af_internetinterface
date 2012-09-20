﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using NUnit.Framework;
using Test.Support.Web;

namespace InforoomInternet.Test.Functional
{
	[TestFixture]
	public class BaseFunctionalFixture : WatinFixture2
	{
		protected Lease Lease;
		protected ClientEndpoint ClientEndpoint;
		protected Client Client;
		protected IpPool Pool;
		protected PhysicalClient PhysicalClient;
		protected Internet Internet;
		protected IpTv IpTv;
		protected Tariff Tariff;

		[SetUp]
		public void SetUp()
		{
			Pool = new IpPool { IsGray = true };
			PhysicalClient = new PhysicalClient();
			Client = new Client();
			Client.PhysicalClient = PhysicalClient;
			ClientEndpoint = new ClientEndpoint();
			ClientEndpoint.Client = Client;
			Lease = new Lease(ClientEndpoint);
			Lease.Pool = Pool;
			Internet = new Internet { HumanName = "internet" };
			IpTv = new IpTv { HumanName = "iptv" };
			Tariff = new Tariff("testTariff", 100);
			session.SaveMany(Internet, IpTv, Tariff);
			Client.ClientServices.Add(new ClientService(Client, Internet));
			Client.ClientServices.Add(new ClientService(Client, IpTv));
			PhysicalClient.Tariff = Tariff;
			session.SaveMany(Pool, PhysicalClient, Client, ClientEndpoint, Lease);
		}

		[TearDown]
		public void TearDown()
		{
			session.DeleteMany(Lease, ClientEndpoint, Client, PhysicalClient, Pool, Tariff, IpTv, Internet);
		}
	}
}