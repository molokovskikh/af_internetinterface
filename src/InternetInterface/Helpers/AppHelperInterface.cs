using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using InternetInterface.Models.Access;

namespace InternetInterface.Helpers
{
    public class AppHelperInterface : AppHelper
    {
        public override string LinkTo(object item, object title, string action)
        {
            if (item == null)
                return "";

            if (!HavePermission(GetControllerName(item), action))
                return String.Format("<a href='#' class='NotAllowedLink'>{0}</a>", title);

            var clazz = "";
            /*if (item is Address && !((Address)item).Enabled)
                clazz = "DisabledByBilling";*/

            var uri = GetUrl(item, action);
            return String.Format("<a class='{1}' href='{2}'>{0}</a>", title, clazz, uri);
        }

        public override bool HavePermission(string controller, string action)
        {
            return AccessRules.GetAccessName(action).Count(CategorieAccessSet.AccesPartner) > 0;
        }

        public override string GetFutAdr(string url)
        {
            return Context.CurrentControllerContext.Action + url.Remove(0, 1);
        }

        public override string GetQuerystring(string key, string direction)
        {
            return String.Format("filter.SortBy={0}&filter.Direction={1}", key, direction);
        }
    }
}