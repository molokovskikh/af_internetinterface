using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
    [TestFixture]
    class ServicesFixture
    {
        public ServicesFixture()
        {
            WatinFixture.ConfigTest();
        }

        [Test]
        public void CreateService()
        {
            using (new SessionScope())
            {
                var service = Service.GetByName("DebtWork");
                service.EditClient(new Clients());
                new DebtWork().Save();
            }
        }
    }
}
