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

        [Property("`Case`")]
        public virtual int? Case { get; set; }

        [Property, ValidateNonEmpty("Введите номер дома"), ValidateInteger("Это поле должно быть число")]
        public virtual int ApartmentCount { get; set; }

        [Property]
        public virtual DateTime? LastPassDate { get; set; }

        [Property]
        public virtual int PassCount { get; set; }

        [HasMany(ColumnKey = "House", OrderBy = "Number")]
        public virtual IList<Apartment> Apartments { get; set; }

        [HasMany(ColumnKey = "House", OrderBy = "Number")]
        public virtual IList<Entrance> Entrances { get; set; }

        public virtual Apartment GetApartmentWithNumber(int num)
        {
            var apartment = Apartments.Where(a => a.Number == num).ToList();
            if (apartment.Count != 0)
                return apartment.First();
            return null;
        }
    }
}