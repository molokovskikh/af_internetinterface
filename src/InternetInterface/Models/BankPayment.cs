using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace InternetInterface.Models
{
    [ActiveRecord("Payments", Schema = "Billing")]
    public class BankPayment : Common.Web.Ui.Helpers.BankPayment
    {
        [BelongsTo(Column = "Id", Table = "LawyerPerson", Cascade = CascadeEnum.SaveUpdate)]
        public override Common.Web.Ui.Helpers.IPayer Payer
        {
            get
            {
                return base.Payer;
            }
            set
            {
                base.Payer = value;
            }
        }
    }
}