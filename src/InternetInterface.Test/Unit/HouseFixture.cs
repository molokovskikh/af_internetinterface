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
            var house = new House { Apartments = new List<Apartment>() };
            house.Apartments.Add(new Apartment { House = house });
            house.Apartments.Add(new Apartment { House = house });
            house.Apartments.Add(new Apartment { House = house });
            /*foreach (var en in house.Apartments)
            {
                en.Apartments = new List<Apartment>();
                for (int i = 0; i < 10; i++)
                    en.Apartments.Add(new Apartment());
            }*/
            Assert.That(house.GetApartmentCount(), Is.EqualTo(30));
        }
    }
}
