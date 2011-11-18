using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using InternetInterface.Models.Access;

namespace InternetInterface.Helpers
{
	public class AppHelper : Common.Web.Ui.Helpers.AppHelper
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

		public override string GetQuerystring(string key, string direction)
		{
			if (uriParams.Contains("SortBy") && uriParams.Contains("Direction"))
			{
				var sortByPos = uriParams.IndexOf("SortBy");
				var newxtAndPos = uriParams.IndexOf('&', sortByPos);
				uriParams = uriParams.Remove(sortByPos - 8, newxtAndPos - sortByPos + 8);
				var DirectionPos = uriParams.IndexOf("Direction");
				newxtAndPos = uriParams.IndexOf('&', DirectionPos);
				if (newxtAndPos == -1)
					newxtAndPos = uriParams.Length;
				uriParams = uriParams.Remove(DirectionPos - 8, newxtAndPos - DirectionPos + 8);
			}
			if ((ControllerContext.PropertyBag["filter"] is ISortableContributor))
			{
				var sortableContributor = ControllerContext.PropertyBag["filter"] as ISortableContributor;
				if (sortableContributor != null)
				{
					var curpage =
						sortableContributor.GetParameters()["CurrentPage"];
					uriParams += "&filter.CurrentPage=" + curpage;
				}
			}
			return String.Format("filter.SortBy={0}&filter.Direction={1}", key, direction);
		}

		public override string GetErrorHtmlObject(string errorText)
		{
			var errObj = new StringBuilder();
			errObj.AppendLine("<div class=\"flash\" style=\"margin:0px; padding:0px; height:100%; width:100%;\" >");
			errObj.AppendLine("<div class=\"message error\" style=\"margin:0px; padding:0px;\" >");
			errObj.AppendLine(string.Format("<p>{0}</p>", errorText));
			errObj.AppendLine("</div>");
			errObj.AppendLine("</div>");
			return errObj.ToString();
		}
	}
}