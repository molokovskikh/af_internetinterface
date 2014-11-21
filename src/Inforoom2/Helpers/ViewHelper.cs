﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Inforoom2.Models;
using NHibernate.Linq;
using NHibernate.Linq.Functions;
using NHibernate.Validator.Cfg.Loquacious;

namespace Inforoom2.Helpers
{
	public static class ViewHelper
	{
		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			Func<TModel, object> htmlAttributes, object selectTagAttributes, int selectedValueId)
			where TModel : BaseModel
			where TProperty : BaseModel
		{
			string expr = expression.ToString();
			string propertyInfo = expr.After(").") + ".Id";

			var selectAttributes = new StringBuilder();

			if (selectTagAttributes != null) {
				selectAttributes = GetPropsValues(selectTagAttributes);
			}

			var options = new StringBuilder();
			foreach (var model in modelCollection) {
				string value = string.Empty;
				if (optionValue != null) {
					value = optionValue(model);
				}

				var optionAttributes = new StringBuilder();
				if (htmlAttributes != null) {
					 optionAttributes = GetPropsValues(htmlAttributes(model));
				}
				if (model.Id == selectedValueId) {
					options.AppendFormat("<option value={0} selected = selected {1}>{2}</option>", model.Id,
						optionAttributes.Replace("{", "").Replace("}", ""), value);
				}
				else {
					options.AppendFormat("<option value={0} {1}>{2}</option>", model.Id,
						optionAttributes.Replace("{", "").Replace("}", ""), value);
				}
			}
			string selectId = modelCollection.FirstOrDefault().GetType().Name + "DropDown";

			var selectString = string.Format("<select id='{0}' name='{3}' {2}>{1}</select>", selectId.Replace("Proxy", ""),
				options, selectAttributes, propertyInfo);
			return new HtmlString(selectString);
		}

		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			Func<TModel, object> htmlAttributes, object selectTagAttributes)
			where TModel : BaseModel
			where TProperty : BaseModel
		{
			int selectedId = 0;
			return DropDownListExtendedFor(helper, expression, modelCollection, optionValue, htmlAttributes, selectTagAttributes,
				selectedId);
		}

		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			Func<TModel, object> htmlAttributes)
			where TModel : BaseModel
			where TProperty : BaseModel
		{
			int selectedId = 0;
			return DropDownListExtendedFor(helper, expression, modelCollection, optionValue, htmlAttributes, null,
				selectedId);
		}

		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			int selectedValueId)
			where TModel : BaseModel
			where TProperty : BaseModel
		{
			return DropDownListExtendedFor(helper, expression, modelCollection, optionValue, null, null, selectedValueId);
		}

		private static StringBuilder GetPropsValues(object obj)
		{
			var type = obj.GetType();
			var sb = new StringBuilder();
			IList<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties());
			foreach (PropertyInfo prop in props) {
				var attribute = prop.GetValue(obj, null);
				sb.AppendFormat(prop.Name + "=" + "\"" + attribute + "\"");
			}
			return sb;
		}

		public static string After(this string value, string a)
		{
			int posA = value.LastIndexOf(a);
			if (posA == -1) {
				return "";
			}
			int adjustedPosA = posA + a.Length;
			if (adjustedPosA >= value.Length) {
				return "";
			}
			return value.Substring(adjustedPosA);
		}

		// Хелпер для Enum
		public static MvcHtmlString DropDownEnumListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
			Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
			where TModel : class
		{
			TProperty value = htmlHelper.ViewData.Model == null
				? default(TProperty)
				: expression.Compile()(htmlHelper.ViewData.Model);
			string selected = value == null ? String.Empty : value.ToString();
			return htmlHelper.DropDownListFor(expression, CreateSelectListForEnum(expression.ReturnType, selected),
				htmlAttributes);
		}

		private static IEnumerable<SelectListItem> CreateSelectListForEnum(Type enumType, string selectedItem)
		{
			return (from object item in Enum.GetValues(enumType)
				let fi = enumType.GetField(item.ToString())
				let attribute = fi.GetCustomAttributes(typeof (DisplayAttribute), true).FirstOrDefault()
				let title = attribute == null ? item.ToString() : ((DisplayAttribute) attribute).Name
				select new SelectListItem {
					Value = item.ToString(),
					Text = title,
					Selected = selectedItem == item.ToString()
				}).ToList();
		}
	}
}