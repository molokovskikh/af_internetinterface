using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

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
                using (var browser = Open("HouseMap/ViewHouseInfo.rails"))
                {
                    //Console.WriteLine(browser.Text);
                }
            }
        }
    }
}
