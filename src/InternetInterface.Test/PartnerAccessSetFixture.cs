using System;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using log4net.Config;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace InternetInterface.Test
{

	[TestFixture]
	public class PartnerAccessSetFixture : CategorieAccessSet
	{

		[TestFixtureSetUp]
		public void Setup()
		{
			XmlConfigurator.Configure();
			if (!ActiveRecordStarter.IsInitialized)
				ActiveRecordStarter.Initialize(new[] {
					Assembly.Load("InternetInterface"),
					//Assembly.Load("InternetInterface.Test"),
				}, ActiveRecordSectionHandler.Instance);
		}

		[Test]
		public void AccessSearchTest()
		{
			//InithializeContent.partner = Castle.MonoRail
			//Console.WriteLine(AccesPartner(AccessCategoriesType.GetClientInfo));
		}

		[Test]
		public void ShemaTest()
		{
			try
			{
				using (var connection = new MySqlConnection("Data Source=testsql.analit.net;Database=Internet;User ID=system;Password=newpass;Connect Timeout=300;pooling=true;convert zero datetime=yes;Default command timeout=300;Allow User Variables=true;"))
				{
					connection.Open();
					ActiveRecordStarter.DropSchema();
					new MySqlCommand("drop database if exists Internet;create database Internet;", connection).ExecuteNonQuery();
				}
			}
			catch (MySqlException e)
			{
				if (e.Message != "Unable to connect to any of the specified MySQL hosts.")
					throw;
			}
			//ActiveRecordStarter.CreateSchema();
		}
	}
}
