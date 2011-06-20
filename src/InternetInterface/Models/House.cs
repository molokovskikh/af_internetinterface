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
        public virtual int? Case { get; set; }

        [Property, ValidateNonEmpty("Ввкдите количество квартир"), ValidateInteger("Это поле должно быть число")]
        public virtual int ApartmentCount { get; set; }

        [Property]
        public virtual DateTime? LastPassDate { get; set; }

        [Property]
        public virtual int PassCount { get; set; }

        [Property]
        public virtual int CompetitorCount { get; set; }

        [HasMany(ColumnKey = "House", OrderBy = "Number")]
        public virtual IList<Apartment> Apartments { get; set; }

        [HasMany(ColumnKey = "House", OrderBy = "Number")]
        public virtual IList<Entrance> Entrances { get; set; }

        [HasMany(ColumnKey = "House", OrderBy = "BypassDate")]
        public virtual IList<BypassHouse> Bypass { get; set; }

        public virtual Apartment GetApartmentWithNumber(int num)
        {
            var apartment = Apartments.Where(a => a.Number == num).ToList();
            if (apartment.Count != 0)
                return apartment.First();
            return null;
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
    }
}