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
		}

		[Test]
		public void TAccessDependence()
		{
			/*AccessDependence.SetAccessDependence();
			AccessDependence.GenerateAddList(AccessDependence.accessDependence, "2");
			AccessDependence.GenerateDeleteList(AccessDependence.accessDependence, "7");*/
			/*Console.WriteLine(AccessDependence.toAdd);
			Console.WriteLine(AccessDependence.toDelete);*/
		}

		[Test]
		public void TestRegistrPartner()
		{
			using (var browser = Open("Register/RegisterPartner.rails"))
			{
				Assert.That(browser.Text, Is.StringContaining("Форма регистрации"));
				Assert.That(browser.Text, Is.StringContaining("Фамилия Имя Отчество"));
				Assert.That(browser.Text, Is.StringContaining("EMail"));
				Assert.That(browser.Text, Is.StringContaining("Номер телефона"));
				Assert.That(browser.Text, Is.StringContaining("Адрес"));
				Assert.That(browser.Text, Is.StringContaining("Логин"));
				Assert.That(browser.Text, Is.StringContaining("Опции доступа"));
				var AccessCats = AccessCategories.FindAll().Where(p => p.Id != 9);
				foreach (var cat in AccessCats)
				{
					Assert.That(browser.Text, Is.StringContaining(cat.Name));
				}
				browser.TextField(Find.ById("FIO")).AppendText("TestFIO");
				browser.TextField(Find.ById("EMail")).AppendText("Test@Mail.ru");
				browser.TextField(Find.ById("TelNum")).AppendText("8-111-111-11-11");
				browser.TextField(Find.ById("Adress")).AppendText("earch");
				var rnd = new Random();
				var loginPrefix = rnd.Next(100);
				browser.TextField(Find.ById("Login")).AppendText("Login" + loginPrefix);
				browser.CheckBox(Find.ById("GCI")).Checked = true;
				browser.CheckBox(Find.ById("RC")).Checked = true;
				browser.CheckBox(Find.ById("CD")).Checked = true;
				browser.Button(Find.ById("RegisterPartnerButton")).Click();
				Assert.That(browser.Text, Is.StringContaining("Информация для партнера: TestFIO"));
				Assert.That(browser.Text, Is.StringContaining("EMailTest@Mail.ru"));
				Assert.That(browser.Text, Is.StringContaining("Номер телефона8-111-111-11-11"));
				Assert.That(browser.Text, Is.StringContaining("Адрес earch"));
				Assert.That(browser.Text, Is.StringContaining("ЛогинLogin" + loginPrefix));
				foreach (var partner in Partner.FindAllByProperty("Login", "Login"+loginPrefix).ToList())
				{
					partner.DeleteAndFlush();
				}
			}
		}
	}
}
