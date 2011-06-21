using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

namespace InternetInterface.Test.Functional
{
    [TestFixture]
    class HouseMapFixture : WatinFixture 
    {
        [Test]
        public void HouseMap()
        {
            using (new SessionScope())
            {
                using (var browser = Open("HouseMap/FindHouse.rails"))
                {  
                    browser.Button("FindHouseButton").Click();
                    Assert.Greater(browser.Table("find_result_table").TableRows.Count, 0);
                    browser.Link("huise_link_0").Click();
                    //browser.Element(Find.ByName("EnCdsfount"));
                    if (browser.Element(Find.ByName("EnCount")).Exists)
                    {
                        browser.TextField("EnCount").AppendText("4");
                        browser.TextField("ApCount").AppendText("20");
                        browser.TextField("CompetitorCount").AppendText("10");
                        browser.Link("naznach_link").Click();
                    }
                    //Console.WriteLine(browser.Text);
                    Assert.IsTrue(browser.Text.Contains("TV"));
                    Assert.IsTrue(browser.Text.Contains("INT"));
                    Assert.IsTrue(browser.Text.Contains("20"));

                }
            }
        }
    }
}
