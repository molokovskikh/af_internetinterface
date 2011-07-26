using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
    [ActiveRecord("Services", Schema = "Internet", Lazy = true)]
    public class Service : ValidActiveRecordLinqBase<Service>
    {
        [PrimaryKey]
        public virtual uint Id { get; set; }

        [Property]
        public virtual string Name { get; set; }

        [Property]
        public virtual decimal Price { get; set; }

        public static Service GetByName(string name)
        {
            return Queryable.Where(s => s.Name == name).FirstOrDefault();
        }
    }
}