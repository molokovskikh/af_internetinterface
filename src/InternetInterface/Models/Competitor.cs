using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
    [ActiveRecord("Competitor", Schema = "internet", Lazy = true)]
    public class Competitor : ActiveRecordLinqBase<ConnectGraph>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [Property]
        public virtual string Name { get; set; }

        [BelongsTo]
        public virtual CompetitorType Type { get; set; }
    }
}