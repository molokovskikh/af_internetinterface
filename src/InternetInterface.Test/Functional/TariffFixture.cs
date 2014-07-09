using System;
using System.Linq;
using Headless;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class TariffFixture : HeadlessFixture
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
			Select("tariff_Region_Id", region.Id);
			ClickButton("Сохранить");
			AssertText("Сохранено");

			session.Refresh(tariff);
			Assert.AreEqual(region, tariff.Region);
		}

		private void Select(string id, object value)
		{
			page.Find<HtmlList>().ById(id).Select(value.ToString());
		}
	}
}