using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
    [TestFixture]
    class HouseFixture
    {
        [Test]
        public void ApartamentCountTest()
        {
            var house = new House{Entrances = new List<Entrance>()};
            house.Entrances.Add(new Entrance { House = house });
            house.Entrances.Add(new Entrance { House = house });
            house.Entrances.Add(new Entrance { House = house });
            foreach (var en in house.Entrances)
            {
                en.Apartments = new List<Apartment>();
                for (int i = 0; i < 10; i++)
                    en.Apartments.Add(new Apartment());
            }
            Assert.That(house.GetApartmentCount(), Is.EqualTo(30));
        }
    }
}
