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
		private static HTMLGenerator html;

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
		public static HtmlString HiddenForModelList<TModel, TProperty>(this HtmlHelper helper, TModel model, Expression<Func<TModel, TProperty>> expression, int skipId = 0)
			where TProperty : IEnumerable
		{
			string expr = expression.ToString();
			var func = expression.Compile();
			var list = func(model) as IList;
			if(list == null)
				throw new Exception("При создании скрытогой верстки для списка моделей, был передан вовсе не список");
			var builder = new StringBuilder();

				var field = expr.After(").");
				if(string.IsNullOrEmpty(field))
					throw new Exception("При создании скрытогой верстки для списка моделей не удается разыменовать имя поля-списка. Наиболее вероятная ошибка - указание 'i => i.{поле списка}' вместо 'i => {Переменная модели}.{поле списка}'. Сравни свой код с тем, откуда ты копировал.");
				var name = field + "[-1].Id";
				
				builder.Append(string.Format("<input type='hidden' name='{0}' value='{1}' />", name, skipId));

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
		/// <param name="firstEmptyElementAdd">Добавить первым элементом пустое значение</param>
		/// <returns>HTML выподающий список</returns>
		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			Func<TModel, object> htmlAttributes, object selectTagAttributes, string selectedValueId, bool firstEmptyElementAdd = false)
			where TModel : BaseModel
		{
			// свойства тэга Select
			var defaultSelectAttributes = new Dictionary<string, string>() {
				{ "id", "" },
				{ "name", "" }
			};
			// default <select> attributes
			string expr = expression.ToString();
			defaultSelectAttributes["name"] = "name=\"" + expr.After(").") + ".Id\"";
			if (typeof(TProperty).GetInterfaces().Contains(typeof(IEnumerable)))
				defaultSelectAttributes["name"] = "name=\"" + expr.After(").") + "[].Id" + ".Id\"";

			if (modelCollection.Count > 0) {
				defaultSelectAttributes["id"] = "id=\"" + modelCollection.FirstOrDefault().GetType().Name + "DropDown\"";
			}
			var options = new StringBuilder();
			if (firstEmptyElementAdd) {
				options.AppendFormat("<option selected = selected></option>");
			}
			// userInserted <select> attributes 
			if (selectTagAttributes != null) {
				GetPropsValues(selectTagAttributes, defaultSelectAttributes);
			}
			foreach (var model in modelCollection) {
				// свойства тэга Option
				var defaultOptionAttributes = new Dictionary<string, string>() {
					{ "value", "value=\"" + model.Id + "\"" },
					{ "selected", "selected = selected" }
				};
				// default <option> attributes
				if (optionValue != null) {
					defaultOptionAttributes["text"] = optionValue(model);
				}
				// userInserted <option> attributes
				if (htmlAttributes != null) {
					GetPropsValues(htmlAttributes(model), defaultOptionAttributes);
				}

				if (defaultOptionAttributes["value"] == "value=\"" + selectedValueId + "\"") {
					options.AppendFormat("<option {0} {1} >{2}</option>", defaultOptionAttributes["value"],
						string.Join(" ", defaultOptionAttributes.Where(s => s.Key != "value" && s.Key != "text").Select(s => s.Value).ToArray()), defaultOptionAttributes["text"]);
				}
				else {
					options.AppendFormat("<option {0} {1} >{2}</option>", defaultOptionAttributes["value"],
						string.Join(" ", defaultOptionAttributes.Where(s => s.Key != "value" && s.Key != "text" && s.Key != "selected").Select(s => s.Value).ToArray()), defaultOptionAttributes["text"]);
				}
			}
			var selectString = string.Format("<select {0} {1} {2}>{3}</select>", defaultSelectAttributes["id"], defaultSelectAttributes["name"],
				string.Join(" ", defaultSelectAttributes.Where(s => s.Key != "id" && s.Key != "name").Select(s => s.Value).ToArray()), options);

			return new HtmlString(selectString);
		}

		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			Func<TModel, object> htmlAttributes, object selectTagAttributes, int selectedValueId, bool firstEmptyElementAdd = false)
			where TModel : BaseModel
		{
			return DropDownListExtendedFor(helper, expression, modelCollection, optionValue, htmlAttributes, selectTagAttributes,
				selectedValueId.ToString(), firstEmptyElementAdd);
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
		/// <param name="firstEmptyElementAdd">Добавить первым элементом пустое значение</param>
		/// <param name="optionAdditionalAttributes">Условное добавление дополнительных атрибутов тэгу option</param>
		/// <returns>HTML выподающий список</returns>
		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			Func<TModel, object> htmlAttributes, object selectTagAttributes, bool firstEmptyElementAdd = false)
			where TModel : BaseModel
		{
			int selectedId = 0;
			return DropDownListExtendedFor(helper, expression, modelCollection, optionValue, htmlAttributes, selectTagAttributes,
				selectedId, firstEmptyElementAdd);
		}


		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			Func<TModel, object> htmlAttributes, bool firstEmptyElementAdd = false)
			where TModel : BaseModel
		{
			int selectedId = 0;
			return DropDownListExtendedFor(helper, expression, modelCollection, optionValue, htmlAttributes, null,
				selectedId, firstEmptyElementAdd);
		}

		public static HtmlString DropDownListExtendedFor<TModel, TProperty>(this HtmlHelper helper,
			Expression<Func<TModel, TProperty>> expression, IList<TModel> modelCollection, Func<TModel, string> optionValue,
			int selectedValueId, bool firstEmptyElementAdd = false)
			where TModel : BaseModel
		{
			return DropDownListExtendedFor(helper, expression, modelCollection, optionValue, null, null, selectedValueId, firstEmptyElementAdd);
		}


		public static HtmlString ValidationEditor(this HtmlHelper helper, ValidationRunner validation, object obj, string propertyName, object htmlAttributes, HtmlTag htmlTag, HtmlType htmlType, bool isValidated, object forcedValidationAttribute = null)
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

			var error = validation.GetError(obj, propertyName, html, null, isValidated, forcedValidationAttribute);

			if (string.IsNullOrEmpty(error.ToString())) {
				return new HtmlString(html);
			}

			return error;
		}

		public static HtmlString ValidationEditor(this HtmlHelper helper, ValidationRunner validation, object obj, string propertyName, object htmlAttributes, HtmlTag htmlTag, HtmlType htmlType, object forcedValidationAttribute = null)
		{
			return ValidationEditor(helper, validation, obj, propertyName, htmlAttributes, htmlTag, htmlType, false, forcedValidationAttribute);
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

		private static void GetPropsValues(object obj, Dictionary<string, string> currentAttributes)
		{
			var type = obj.GetType();
			var sb = new StringBuilder();
			IList<PropertyInfo> props = new List<PropertyInfo>(type.GetProperties());
			foreach (PropertyInfo prop in props) {
				var attribute = prop.GetValue(obj, null);
				if (currentAttributes.ContainsKey(prop.Name.ToLower())) {
					currentAttributes[prop.Name.ToLower()] = prop.Name + "=" + "\"" + attribute + "\"";
				}
				else {
					currentAttributes.Add(prop.Name.ToLower(), prop.Name + "=" + "\"" + attribute + "\"");
				}
			}
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