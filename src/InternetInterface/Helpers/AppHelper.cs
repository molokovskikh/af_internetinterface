﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Models.Access;
using NHibernate;

namespace InternetInterface.Helpers
{
	public class AppHelper : Common.Web.Ui.Helpers.AppHelper
	{
		public AppHelper(IEngineContext engineContext) : base(engineContext)
		{
			RegisterEditor();
		}

		public AppHelper()
		{
			RegisterEditor();
		}

		public override bool HavePermission(string controller, string action)
		{
			return AccessRules.GetAccessName(action).Count(CategorieAccessSet.AccesPartner) > 0
				|| InitializeContent.Partner.HavePermissionTo(controller, action);
		}

		public void RegisterEditor()
		{
			Editors.Add(typeof(Period), (name, value, options) => {
				var period = (Period)value;
				if (period == null)
					return null;

				return "<label style='padding:2px'>Год</label>"
					+ GetEdit(name + ".Year", typeof(int), period.Year, options)
						+ "<label style='padding:2px'>Месяц</label>"
							+ GetEdit(name + ".Interval", typeof(Interval), period.Interval, options);
			});
		}

		protected override string GetBuiltinEdit(string name, Type valueType, object value, object options, PropertyInfo property)
		{
			if (name.EndsWith(".Year")) {
				if (valueType == typeof(int))
					return helper.Select(name, Period.Years);
				else if (valueType == typeof(int?)) {
					var items = new[] { "Все" }.Concat(Period.Years.Select(y => y.ToString())).ToArray();
					return helper.Select(name, items);
				}
			}
			return base.GetBuiltinEdit(name, valueType, value, options, property);
		}

		public override string GetQuerystring(string key, string direction)
		{
			if (uriParams.Contains("SortBy") && uriParams.Contains("Direction")) {
				var sortByPos = uriParams.IndexOf("SortBy");
				var newxtAndPos = uriParams.IndexOf('&', sortByPos);
				uriParams = uriParams.Remove(sortByPos - 8, newxtAndPos - sortByPos + 8);
				var DirectionPos = uriParams.IndexOf("Direction");
				newxtAndPos = uriParams.IndexOf('&', DirectionPos);
				if (newxtAndPos == -1)
					newxtAndPos = uriParams.Length;
				uriParams = uriParams.Remove(DirectionPos - 8, newxtAndPos - DirectionPos + 8);
			}
			if ((ControllerContext.PropertyBag["filter"] is ISortableContributor)) {
				var sortableContributor = ControllerContext.PropertyBag["filter"] as ISortableContributor;
				if (sortableContributor != null) {
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

		public IDictionary Merge(object src, object dst)
		{
			var srcDict = ToDict(src);
			var dstDict = ToDict(dst);
			foreach (var key in srcDict.Keys) {
				if (!dstDict.Contains(key)) {
					dstDict.Add(key, srcDict[key]);
				}
			}
			return dstDict;
		}

		private IDictionary ToDict(object src)
		{
			if (src is IDictionary)
				return (IDictionary)src;
			if (src is IUrlContributor)
				return ((IUrlContributor)src).GetQueryString();
			return new Dictionary<object, object>();
		}

		public override string LinkTo(object item)
		{
			if (item == null)
				return "";
			if (NHibernateUtil.GetClass(item) == typeof(Client)) {
				var client = ((Client)item);
				var action = "SearchUserInfo";
				if (client.GetClientType() == ClientType.Legal)
					action = "LawyerPersonInfo";
				return LinkTo(client.Name, "UserInfo", action, new Dictionary<string, object> {
					{ "filter.ClientCode", client.Id }
				});
			}
			return base.LinkTo(item);
		}

		public string LinkToTitled(object item, object title)
		{
			if (item == null)
				return "";
			if (NHibernateUtil.GetClass(item) == typeof(Client)) {
				var client = ((Client)item);
				var action = "SearchUserInfo";
				if (client.GetClientType() == ClientType.Legal)
					action = "LawyerPersonInfo";
				return LinkTo(title != null ? title.ToString() : client.Name, "UserInfo", action, new Dictionary<string, object> {
					{ "filter.ClientCode", client.Id }
				});
			}
			return base.LinkTo(item, title, null);
		}
	}
}