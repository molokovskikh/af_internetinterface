using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public virtual Client Client { get; set; }

        [BelongsTo]
        public virtual Service Service { get; set; }

        [Property]
        public virtual DateTime? BeginWorkDate { get; set; }

        [Property]
        public virtual DateTime? EndWorkDate { get; set; }

        [Property]
        public virtual bool Activated { get; set; }

        public override void Delete()
        {
            if (Service.CanDelete(this))
                base.Delete();
        }

        public virtual void Activate()
        {
            Service.Activate(this);
        }

        public virtual void Deactivate()
        {
            Service.Diactivate(this);
        }
    }
}