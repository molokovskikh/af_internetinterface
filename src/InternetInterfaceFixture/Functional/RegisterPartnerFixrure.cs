using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using InternetInterface.Models;
using InternetInterfaceFixture.Helpers;
using log4net.Config;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using WatiN.Core;

namespace InternetInterfaceFixture.Functional
{
	[TestFixture]
	class RegisterPartnerFixrure : WatinFixture
	{
		/*[TestFixtureSetUp]
		public void Setup()
		{
			XmlConfigurator.Configure();
			if (!ActiveRecordStarter.IsInitialized)
				ActiveRecordStarter.Initialize(new[] {
					Assembly.Load("InternetInterface"),
					Assembly.Load("InternetInterfaceFixture"),
				}, ActiveRecordSectionHandler.Instance);
		}*/
		[Test]
		public void MigrationTets()
		{
			ActiveRecordStarter.GenerateCreationScripts(@"..\\..\\..\\SQL_Migrate_ActiveRecordGenerator.sql");
		}

		[Test]
		public void TestAccessDependence()
		{
			Console.WriteLine(AccessCategoriesType.SendDemand.ToString());
			AccessCategoriesType res;
			Console.WriteLine(AccessCategoriesType.TryParse("SendDemand", out res));
			Console.WriteLine(res);
		}

		[Test]
		public void TAccessDependence()
		{
			AccessDependence.SetAccessDependence();
			AccessDependence.GenerateAddList(AccessDependence.accessDependence, "2");
			AccessDependence.GenerateDeleteList(AccessDependence.accessDependence, "7");
			/*Console.WriteLine(AccessDependence.toAdd);
			Console.WriteLine(AccessDependence.toDelete);*/
		}

		[Test]
		public void TestRegistr()
		{
			using (var browser = Open("Register/RegisterPartner"))
			{
				browser.TextField(Find.ById("FIO")).AppendText("TestFIO");
				browser.TextField(Find.ById("EMail")).AppendText("Test@Mail.ru");
				browser.TextField(Find.ById("TelNum")).AppendText("8-111-111-11-11");
				browser.TextField(Find.ById("Adress")).AppendText("earch");
				var rnd = new Random();
				browser.TextField(Find.ById("Login")).AppendText("Login"+rnd.Next(100));
				browser.CheckBox(Find.ById("GCI")).Checked = true;
				browser.CheckBox(Find.ById("RC")).Checked = true;
				browser.CheckBox(Find.ById("CD")).Checked = true;
				browser.Button(Find.ById("RegisterPartnerButton")).Click();
				Assert.That(browser.Text, Is.StringContaining("Регистрация пользователя прошла успешно"));
				//browser.Text(Find.ById("")).
				/*browser.Link(Find.ByText("Мониторинг работы клиентов")).Click();
				Assert.That(browser.Text, Is.StringContaining("Мониторинг работы клиентов"));
				Assert.That(browser.SelectList(Find.ByName("filter")).SelectedOption.Text, Is.EqualTo("Список необновляющихся копий"));*/
			}
		}
	}
}
