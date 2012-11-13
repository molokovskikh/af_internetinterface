using System.Threading;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class BrigadFixture : global::Test.Support.Web.WatinFixture2
	{
		[Test, Ignore]
		public void BrigadTest()
		{
			Open("Brigads/ShowBrigad.rails");
			Assert.That(browser.Text, Is.StringContaining("ID"));
			Assert.That(browser.Text, Is.StringContaining("Имя"));
			browser.Button(Find.ById("RegisterBrigad")).Click();
			Thread.Sleep(2000);
			browser.TextField(Find.ById("Name")).AppendText("TestBrigad");
			browser.Button(Find.ById("RegisterBrigadButton")).Click();
			Thread.Sleep(2000);
			foreach (var brigad in Brigad.FindAllByProperty("Name", "TestBrigad")) {
				brigad.DeleteAndFlush();
			}
		}

		[Test]
		public void ReportOnWork()
		{
			Open("Brigads/ReportOnWork");
			AssertText("Параментры фильтрации");
			AssertText("Статистика подключений");
			AssertText("Всего");
			AssertText("Не состоявшихся");
			Click("Показать");
			AssertText("Параментры фильтрации");
		}
	}
}