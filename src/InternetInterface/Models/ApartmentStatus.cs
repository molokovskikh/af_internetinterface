using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
    [ActiveRecord("ApartmentStatuses", Schema = "Internet", Lazy = true)]
    public class ApartmentStatus :ActiveRecordLinqBase<ApartmentStatus>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [Property]
        public virtual string Name { get; set; }

        [Property]
        public virtual int ActivateDate { get; set; }
    }
}