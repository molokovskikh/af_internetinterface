using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
    [ActiveRecord("Entrance", Schema = "internet", Lazy = true)]
    public class Entrance : ActiveRecordLinqBase<ConnectGraph>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [BelongsTo]
        public virtual House House { get; set; }

        public virtual int Number { get; set; }

        public virtual bool Strut { get; set; }

        public virtual bool Cable { get; set; }
    }
}