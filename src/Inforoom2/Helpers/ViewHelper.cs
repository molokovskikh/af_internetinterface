using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.UI.WebControls;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Linq.Functions;
using NHibernate.Validator.Cfg.Loquacious;

namespace Inforoom2.Helpers
{
	public abstract class HTMLGenerator
	{
	}
	public static class ViewHelper
	{ 
		static HTMLGenerator html;


		/// <summary>
		/// Создает список скрытых полей для списочных моделей, кроме указанного элемента.
		/// Используется для того, чтобы удалить 1 элемент из списка в модели.
		/// В байндер подгрузятся все элементы, кроме одного. Соответственно Hibernate удалит эту связь из БД.
		/// </summary>
		/// <typeparam name="TModel">Модель</typeparam>
		/// <typeparam name="TProperty">Свойство список поле-список.</typeparam>
		/// <param name="helper">ViewHelper</param>
		/// <param name="model">Модель</param>
		/// <param name="expression">Экспрессия, возвращающая список в модели: i => tvChannelGroup.TvChannels</param>
		/// <param name="skipId">Идентификатор модели, которую необходимо удалить.</param>
		/// <returns>Верстка для полей</returns>
		public static HtmlString HiddenForModelList<TModel, TProperty>(this HtmlHelper helper,TModel model, Expression<Func<TModel, TProperty>> expression, int skipId = 0)
		where TProperty : IEnumerable
		{
			string expr = expression.ToString();
			var func = expression.Compile();
			var list = func(model) as IList;
			var builder = new StringBuilder();
			for(var i=0; i < list.Count; i++) {
				var name = expr.After(").") + "["+i+"].Id";
				var item = list[i] as BaseModel;
				if (item.Id == skipId)
					continue;
				builder.Append(string.Format("<input type='hidden' name='{0}' value='{1}' />",name,item.Id));
			}
				
			return new HtmlString(builder.ToString());
		}
		/// <summary>
		/// Вывести выподающий список
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <param name="helper"></param>
		/// <param name="expression">Цель</param>
		/// <param name="modelCollection">Источник</param>
		/// <param name="optionValue">Выводимое значение</param>
		/// <param name="htmlAttributes">Описание html атрибутов</param>
		/// <param name="selectTagAttributes">Свойства тэга</param>
		/// <param name="selectedValueId">Выбранный элемент</param>
		/// <returns>HTML выподающий список</returns>
		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			Func<TModel, object> htmlAttributes, object selectTagAttributes, int selectedValueId)
			where TModel : BaseModel
		{
			string expr = expression.ToString();
			string propertyInfo = expr.After(").") + ".Id";
			if (typeof(TProperty).GetInterfaces().Contains(typeof(IEnumerable)))
				propertyInfo = expr.After(").") + "[].Id";

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
				if (model.Id == selectedValueId)
				{
					options.AppendFormat("<option value={0} selected = selected {1}>{2}</option>", model.Id,
						optionAttributes.Replace("{", "").Replace("}", ""), value);
				}
				else {
					options.AppendFormat("<option value={0} {1}>{2}</option>", model.Id,
						optionAttributes.Replace("{", "").Replace("}", ""), value);
				}
			}
			string selectId = string.Empty;
			if (modelCollection.Count > 0) {
				selectId = modelCollection.FirstOrDefault().GetType().Name + "DropDown";
			}

			var selectString = string.Format("<select id='{0}' name='{3}' {2}>{1}</select>", selectId.Replace("Proxy", ""),
				options, selectAttributes, propertyInfo);
			return new HtmlString(selectString);
		}
		/// <summary>
		/// Вывести выподающий список
		/// </summary>
		/// <typeparam name="TModel"></typeparam>
		/// <typeparam name="TProperty"></typeparam>
		/// <param name="helper"></param>
		/// <param name="expression">Цель</param>
		/// <param name="modelCollection">Источник</param>
		/// <param name="optionValue">Выводимое значение</param>
		/// <param name="htmlAttributes">Описание html атрибутов</param>
		/// <param name="selectTagAttributes">Свойства тэга</param>
		/// <returns>HTML выподающий список</returns>
		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			Func<TModel, object> htmlAttributes, object selectTagAttributes)
			where TModel : BaseModel

		{
			int selectedId = 0;
			return DropDownListExtendedFor(helper, expression, modelCollection, optionValue, htmlAttributes, selectTagAttributes,
				selectedId);
		}


		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			Func<TModel, object> htmlAttributes)
			where TModel : BaseModel
		{
			int selectedId = 0;
			return DropDownListExtendedFor(helper, expression, modelCollection, optionValue, htmlAttributes, null,
				selectedId);
		}

		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			int selectedValueId)
			where TModel : BaseModel
		{
			return DropDownListExtendedFor(helper, expression, modelCollection, optionValue, null, null, selectedValueId);
		}


		public static HtmlString ValidationEditor(this HtmlHelper helper, ValidationRunner validation, object obj, string propertyName, object htmlAttributes, HtmlTag htmlTag, HtmlType htmlType, bool isValidated)
		{
			var tag = Enum.GetName(typeof(HtmlTag), htmlTag);
			string type = string.Empty;
			if (htmlType != HtmlType.none) {
				type = Enum.GetName(typeof(HtmlType), htmlType);
			}


			var objName = obj.GetType().Name;
			objName = Char.ToLowerInvariant(objName[0]) + objName.Substring(1);
			var name = objName + "." + propertyName;
			var value = obj.GetType().GetProperty(propertyName).GetValue(obj, null);
			var id = objName + "_" + propertyName;

			var attributes = new StringBuilder();
			if (htmlAttributes != null) {
				attributes = GetPropsValues(htmlAttributes);
			}

			
			string html = string.Empty;
			switch (htmlTag) {
				case HtmlTag.input:
					//Форматируем дату
					if (value is DateTime)
						value = (DateTime)value == DateTime.MinValue ? "" : ((DateTime)value).ToString("dd.MM.yy");

					html = string.Format("<{0} id=\"{1}\" {2} type=\"{3}\" name =\"{4}\" value=\"{5}\">", tag, id, attributes, type, name, value);
					break;
				case HtmlTag.textarea:
					html = string.Format("<{0} name =\"{3}\" {2} rows = \"6\" cols = \"75\">{4}</{5}>", tag, id, attributes, name, value, tag);
					break;
				case HtmlTag.checkbox:
					var val = (bool)value ? "checked" : "";
					html = string.Format("<input type=\"checkbox\" id=\"{0}\" name =\"{2}\" {1} value=\"{3}\"></input>", id, attributes, name, val);
					break;
				case HtmlTag.date:
					//todo Использовать HTMLGenerator
					html = string.Format("<div class=\"input-group\"><input id=\"{0}\" name =\"{2}\" {1} value=\"{3}\" class=\"form-control datepicker\" data-format=\"D, dd MM yyyy\" type=\"text\" /><div class=\"input-group-addon\"><a href=\"#\"><i class=\"entypo-calendar\"></i></a></div></div>", id, attributes, name, value);
					break;
				case HtmlTag.datetime:
					var dobj = value != null ? (DateTime)value : DateTime.Now;
					if (dobj == DateTime.MinValue) {
						dobj = DateTime.Now;
					}
					var date = dobj.Date.ToString().Split(' ')[0];
					var time = dobj.TimeOfDay;
					var datetime = dobj;
					html = string.Format("<div {1} id=\"{0}\"  class=\"date-and-time\"><input  name =\"{2}\" type=\"hidden\" value=\"{5}\" /><input {1} type=\"text\" data-format=\"dd.mm.yyyy\" value=\"{3}\" class=\"form-control datepicker\"><input {1} value=\"{4}\" type=\"text\" data-second-step=\"5\" data-minute-step=\"10\" data-show-meridian=\"false\" data-default-time=\"current\" data-template=\"dropdown\" class=\"form-control timepicker\"></div>", id, attributes, name, date, time, datetime);
					break;
				default:
					throw new NotImplementedException("Html for tag is not implemented");
			}

			var error = validation.GetError(obj, propertyName, html, null, isValidated);

			if (string.IsNullOrEmpty(error.ToString())) {
				return new HtmlString(html);
			}
		
			return error;
		}

		public static HtmlString ValidationEditor(this HtmlHelper helper, ValidationRunner validation, object obj, string propertyName, object htmlAttributes, HtmlTag htmlTag, HtmlType htmlType)
		{
		 	return ValidationEditor(helper, validation, obj, propertyName, htmlAttributes, htmlTag, htmlType, false);
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
				let attribute = fi.GetCustomAttributes(typeof(DisplayAttribute), true).FirstOrDefault()
				let title = attribute == null ? item.ToString() : ((DisplayAttribute)attribute).Name
				select new SelectListItem {
					Value = item.ToString(),
					Text = title,
					Selected = selectedItem == item.ToString()
				}).ToList();
		}

		public static HtmlString Grid<T>(this HtmlHelper helper, IList<T> list)
		{
			return new HtmlString("dsds");
		}
	}


}