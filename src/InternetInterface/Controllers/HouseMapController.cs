using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
    [Layout("Main")]
    [FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
    public class HouseMapController : ARSmartDispatcherController 
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

       public void EditHouse(uint House)
        {
            PropertyBag["house"] = Models.House.Find(House);
            PropertyBag["Entrances"] = Entrance.Queryable.Where(e => e.House.Id == House).ToList();
            PropertyBag["Switches"] = Models.NetworkSwitches.FindAll().Where(n => !string.IsNullOrEmpty(n.Name));
        }

        [AccessibleThrough(Verb.Post)]
       public void EditHouse([ARDataBind("house")]House house, [ARDataBind("Entrances")]Entrance[] enterances)
        {
            house.Save();
            foreach (var enterance in Entrance.Queryable.Where(e => e.House.Id == house.Id).ToList())
            {
                enterance.Delete();
            }
            var enCount = 0;
            foreach (var enterance in enterances)
            {
                enCount++;
                var _switch = Models.NetworkSwitches.Queryable.Where(n => n.Id == enterance.Switch.Id).ToList();
                new Entrance {
                                 Cable = enterance.Cable,
                                 House = house,
                                 Number = enCount,
                                 Strut = enterance.Strut,
                                 Switch = _switch.Count > 0 ? _switch.First() : null
                             }.Save();
            }
            EditHouse(house.Id);
            //var items = (Entrance[])BindObject(ParamStore.Form, typeof(Entrance[]), "Entrances", AutoLoadBehavior.Always);
        }

        public void FindHouse()
        {
            PropertyBag["SelectEn"] = new List<string> {
                                                           " = ",
                                                           " > ",
                                                           " < "
                                                       };
            PropertyBag["SelectName"] = new List<string> {
                                                           "Равно",
                                                           "Больше",
                                                           "Меньше"
                                                       };
        }

        public void HouseFindResult(string adress, int SubscriberCount, int PenetrationPercent, int PassCount, DateTime startDate, DateTime endDate,
            string SubscriberCount_Oper, string PenetrationPercent_Oper, string PassCount_Oper)
        {
            /*var result = House.FindAll().ToList();
            if (!string.IsNullOrEmpty(adress))
                result = result.Where(r => r.Street.Contains(adress)).ToList();*/
            IList<House> result = new List<House>();
            ARSesssionHelper<House>.QueryWithSession(session => {
                var where = string.Empty;
                if (!string.IsNullOrEmpty(adress))
                    where += "and h.Street like :houseAdress ";
                if (PassCount != 0)
                    where += string.Format(" and h.PassCount {0} :PassCount", PassCount_Oper);
                if ((startDate != DateTime.MinValue) && (endDate != DateTime.MinValue))
                    where += " and h.LastPassDate >= :startDate and h.LastPassDate <= :endDate";
                if (where != string.Empty)
                    where = " WHERE " + where.Remove(0, 4);
                where += " group by h.id ";
                var having = string.Empty;
                if (SubscriberCount != 0)
                    having += string.Format(" and Count(pc.id) {0} :SubscriberCount", SubscriberCount_Oper);
                if (PenetrationPercent != 0)
                    having += string.Format(" and Count(pc.id)/h.ApartmentCount {0} :PenetrationPercent", PenetrationPercent_Oper);
                if (having != string.Empty)
                    having = " HAVING " + having.Remove(0, 4);
                var sqlStr =
                    @"select h.*  from internet.Houses h 
                join internet.PhysicalClients pc on pc.HouseObj = h.id " +
                    where + having;
                var query = session.CreateSQLQuery(sqlStr).AddEntity(typeof (House));
                if (!string.IsNullOrEmpty(adress))
                    query.SetParameter("houseAdress", '%' + adress + '%');
                if (SubscriberCount != 0)
                    query.SetParameter("SubscriberCount", SubscriberCount);
                if (PenetrationPercent != 0)
                    query.SetParameter("PenetrationPercent", PenetrationPercent);
                if (PassCount != 0)
                    query.SetParameter("PassCount", PassCount);
                if ((startDate != DateTime.MinValue) && (endDate != DateTime.MinValue))
                {
                    query.SetParameter("startDate", startDate);
                    query.SetParameter("endDate", endDate);
                }
                result = query.List<House>();
                return result;
            });
            PropertyBag["Houses"] = result;
            FindHouse();
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