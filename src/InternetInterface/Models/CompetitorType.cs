using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
    [ActiveRecord("CompetitorType", Schema = "internet", Lazy = true)]
    public class CompetitorType : ActiveRecordLinqBase<ConnectGraph>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [Property]
        public virtual string Name { get; set; }
    }
}