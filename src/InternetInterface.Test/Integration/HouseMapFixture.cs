using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.MonoRail.TestSupport;
using InternetInterface.Controllers;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
    [TestFixture]
    class HouseMapFixture : BaseControllerTest
    {
        public HouseMapFixture()
        {
            WatinFixture.ConfigTest();
        }

        [Test]
        public void ViewTest()
        {
            using (new SessionScope())
            {
                var mapController = new HouseMapController();
                PrepareController(mapController);
                mapController.ViewHouseInfo();
            }
        }
    }
}
