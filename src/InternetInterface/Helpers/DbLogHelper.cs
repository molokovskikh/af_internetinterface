using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Helpers
{
    public class DbLogHelper : DbLogHelperBase
    {
        public static void SetupParametersForTriggerLogging()
        {
            ArHelper.WithSession(session => SetupParametersForTriggerLogging(new
            {
                InUser = InithializeContent.partner.Name,
                InHost = HttpContext.Current.Request.UserHostAddress
            },
                session));
        }
    }
}