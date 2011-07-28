using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
    [ActiveRecord("ClientServices", Schema = "Internet", Lazy = true)]
    public class ClientService : ValidActiveRecordLinqBase<ClientService>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [BelongsTo]
        public virtual Clients Client { get; set; }

        [BelongsTo]
        public virtual Service Service { get; set; }

        [Property]
        public virtual DateTime? BeginWorkDate { get; set; }

        [Property]
        public virtual DateTime? EndWorkDate { get; set; }
    }
}