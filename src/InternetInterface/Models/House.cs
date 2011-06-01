using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
    [ActiveRecord("House", Schema = "internet", Lazy = true)]
    public class House : ValidActiveRecordLinqBase<ConnectGraph>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        public virtual string Street { get; set; }

        public virtual int Number { get; set; }

        public virtual int CaseHouse { get; set; }

        public virtual int ApartmentCount { get; set; }

        public virtual DateTime LastPassDate { get; set; }

        public virtual int PassCount { get; set; }
    }
}