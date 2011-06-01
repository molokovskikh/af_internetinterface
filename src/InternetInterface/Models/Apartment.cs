using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
    [ActiveRecord("Apartment", Schema = "internet", Lazy = true)]
    public class Apartment : ActiveRecordLinqBase<ConnectGraph>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [BelongsTo]
        public virtual Entrance Entrance { get; set; }


    }
}