using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
    [ActiveRecord(Schema = "Internet", Table = "StaticIps")]
    public class StaticIp : ActiveRecordLinqBase<StaticIp>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [BelongsTo]
        public virtual Client Client { get; set; }

        [Property]
        public virtual string Ip { get; set; } 
        
        [Property]
        public virtual string Gateway { get; set; } 
        
        [Property]
        public virtual string Mask { get; set; }
    }
}