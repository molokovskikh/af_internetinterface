using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
    [ActiveRecord("Houses", Schema = "internet", Lazy = true)]
    public class House : ValidActiveRecordLinqBase<House>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [Property]
        public virtual string Street { get; set; }

        [Property, ValidateNonEmpty("Введите номер дома"), ValidateInteger("Это поле должно быть число")]
        public virtual int Number { get; set; }

        [Property("CaseHouse")]
        public virtual string Case { get; set; }

        [Property, ValidateNonEmpty("Ввкдите количество квартир"), ValidateInteger("Это поле должно быть число")]
        public virtual int ApartmentCount { get; set; }

        [Property]
        public virtual DateTime? LastPassDate { get; set; }

        [Property]
        public virtual int PassCount { get; set; }

        //[Property]
        public virtual int CompetitorCount
        {
            get { return Apartments.Where(a => a.Status == null || a.Status.ShortName != "request").Count(); }
        }

        [HasMany(ColumnKey = "House", OrderBy = "Number", Lazy = true)]
        public virtual IList<Apartment> Apartments { get; set; }

        [HasMany(ColumnKey = "House", OrderBy = "Number", Lazy = true)]
        public virtual IList<Entrance> Entrances { get; set; }

        [HasMany(ColumnKey = "House", OrderBy = "BypassDate", Lazy = true)]
        public virtual IList<BypassHouse> Bypass { get; set; }

        public virtual Apartment GetApartmentWithNumber(int num)
        {
            var apartment = Apartments.Where(a => a.Number == num).ToList();
            if (apartment.Count != 0)
                return apartment.First();
            return null;
        }

        public virtual uint GetClientWithApNumber(string num)
        {
            return
                Client.Queryable.Where(c => c.PhysicalClient.HouseObj == this && c.PhysicalClient.Apartment == num).
                    ToList().Select(c => c.Id).FirstOrDefault();
        }

        public virtual int GetSubscriberCount()
        {
            return PhysicalClients.Queryable.Where(p => p.HouseObj == this).Count();
        }

        public virtual BypassHouse GetLastBypass()
        {
            return Bypass.Last();
        }

        public virtual double GetCompetitorsPenetrationPercent()
        {
            if (ApartmentCount == 0)
                return 1;
            return (double)CompetitorCount / ApartmentCount * 100;
        }

        public virtual double GetPenetrationPercent()
        {
            if (ApartmentCount == 0)
                return 1;
            return (double)(PhysicalClients.Queryable.Where(p => p.HouseObj == this).Count()) / ApartmentCount * 100;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Street, Number, !string.IsNullOrEmpty(Case) ? "корп " + Case : string.Empty);
        }
    }
}