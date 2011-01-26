using System.Threading;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

namespace InternetInterfaceFixture.Functional
{
	[TestFixture]
	class BrigadFixture :WatinFixture
	{
		[Test]
		public void BrigadTest()
		{
			using (var browser = Open("Brigads/ShowBrigad.rails"))
			{
				Assert.That(browser.Text, Is.StringContaining("ID"));
				Assert.That(browser.Text, Is.StringContaining("Имя"));
				browser.Button(Find.ById("RegisterBrigad")).Click();
				Thread.Sleep(2000);
				browser.TextField(Find.ById("Name")).AppendText("TestBrigad");
				browser.Button(Find.ById("RegisterBrigadButton")).Click();
				Thread.Sleep(2000);
				foreach (var brigad in Brigad.FindAllByProperty("Name", "TestBrigad"))
				{
					brigad.DeleteAndFlush();
				} 
			}
		}
	}
}
