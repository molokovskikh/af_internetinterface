using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
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

        [Property]
        public virtual int Number { get; set; }

        [Property]
        public virtual int? Case { get; set; }

        [Property]
        public virtual int ApartmentCount { get; set; }

        [Property]
        public virtual DateTime? LastPassDate { get; set; }

        [Property]
        public virtual int PassCount { get; set; }

        [HasMany(ColumnKey = "House", OrderBy = "Number")]
        public virtual IList<Entrance> Entrances { get; set; }

        public virtual int GetApartmentCount()
        {
            return Entrances.SelectMany(e => e.Apartments).Count();
        }
    }
}