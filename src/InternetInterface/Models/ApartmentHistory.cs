using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
    [ActiveRecord("ApartmentHistory", Schema = "Internet", Lazy = true)]
    public class ApartmentHistory : ActiveRecordLinqBase<ApartmentHistory>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [BelongsTo]
        public virtual Apartment Apartment { get; set; }

        [Property]
        public virtual string ActionName { get; set; }

        [BelongsTo]
        public virtual Partner Agent { get; set; }

        [Property]
        public virtual DateTime ActionDate { get; set; }
    }
}