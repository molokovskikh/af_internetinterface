using System;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class TariffFixture : SeleniumFixture
	{
		[Test]
		public void Edit_tariff()
		{
			var tariff = new Tariff(Guid.NewGuid().ToString(), 100);
			session.Save(tariff);
			var region = session.Query<RegionHouse>().FirstOrDefault(r => r.Name == "Воронеж");
			Open();
			Click("Администрирование");
			AssertText("Тарифы");
			Click("Тарифы");

			AssertText(tariff.Name);
			Click(tariff.Name);
			AssertText("Редактирование тарифа");
			Css("#tariff_Region_Id").SelectByValue(region.Id.ToString());
			ClickButton("Сохранить");
			AssertText("Сохранено");

			session.Refresh(tariff);
			Assert.AreEqual(region, tariff.Region);
		}
	}
}