using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
    [Layout("Main")]
    [FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
    public class HouseMapController : SmartDispatcherController
    {
        public void NetworkSwitches(int id)
        {
            PropertyBag["id"] = id;
            PropertyBag["Switches"] = Models.NetworkSwitches.FindAll().Where(n => !string.IsNullOrEmpty(n.Name));
            CancelLayout();
        }

        public void ViewHouseInfo()
        {
            PropertyBag["Houses"] = House.FindAll().OrderBy(h => h.Street);
            PropertyBag["SelectedHouse"] = House.FindFirst().Id;
        }

        public void BasicHouseInfo(uint id)
        {
            PropertyBag["Editing"] = House.Find(id).ApartmentCount == 0 ? true : false;
            PropertyBag["sHouse"] = House.Find(id);
            CancelLayout();
        }

        [return: JSONReturnBinder]
        public string Register()
        {
            var street = Request.Form["Street"];
            var number = Request.Form["Number"];
            var _case = Request.Form["Case"];
            int res;
            var errors = string.Empty;
            if (!Int32.TryParse(number, out res))
                errors += "Неправильно введен номер дома" + res;
            if (string.IsNullOrEmpty(errors))
                new House {Street = street, Number = Int32.Parse(number), Case = Int32.Parse(_case)}.Save();
            return errors;
        }

        [return: JSONReturnBinder]
        public bool SaveHouseMap()
        {
            var SelectHouse = Request.Form["SelectHouse"];
            var NetSwitch = Request.Form["NetSwitch[]"].Split(new[] {','});
            var Strut = Request.Form["Strut[]"].Split(new[] {','});
            var Cable = Request.Form["Cable[]"].Split(new[] {','});
            var apCount = Request.Form["ApCount"];
            var house = House.Find(Convert.ToUInt32(SelectHouse));
            house.ApartmentCount = Convert.ToInt32(apCount);
            house.Update();
            for (int i = 0; i < NetSwitch.Length; i++)
            {
                new Entrance {
                                 Cable = Convert.ToBoolean(Cable[i]),
                                 House = house,
                                 Number = i + 1,
                                 Strut = Convert.ToBoolean(Strut[i]),
                                 Switch = Convert.ToInt32(NetSwitch[i]) > 0 ? Models.NetworkSwitches.Find(Convert.ToUInt32(NetSwitch[i])) : null
                             }.Save();
            }
            return true;
        }
    }
}