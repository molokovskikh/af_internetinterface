using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
    [ActiveRecord("Apartments", Schema = "internet", Lazy = true)]
    public class Apartment : ActiveRecordLinqBase<Apartment>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [BelongsTo]
        public virtual Entrance Entrance { get; set; }

        [Property]
        public virtual int Number { get; set; }

        [Property]
        public virtual string LastInternet { get; set; }

        [Property]
        public virtual string LastTV { get; set; }

        [Property]
        public virtual string PresentInternet { get; set; }

        [Property]
        public virtual string PresentTV { get; set; }

        [Property]
        public virtual string Comment { get; set; }
    }
}