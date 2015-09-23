using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Common.Tools;
using ExcelLibrary.SpreadSheet;
using Inforoom2.Controllers;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Engine;
using NHibernate.Impl;
using NHibernate.Loader.Criteria;
using NHibernate.Persister.Entity;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using NHibernate.Util;
using Remotion.Linq.Clauses;

namespace Inforoom2.Components
{
	/// <summary>
	///  Необходим для обращения к полям класса, реализующего функционал, 
	///  упрощающий постраничную навигацию, фильтрацию и сортировку, без указания шаблона.
	/// </summary>
	public interface IModelFilter
	{
		int PagesCount { get; } // Кол-во страниц
		int Page { get; } // Текущая страница
		int TotalItems { get; } // Общее количество записей, удовлетворяющих запросу
		int ItemsPerPage { get; } // Кол-во записей на страницу
		string Prefix { get; }

		string GetPageUrl(int number);
	}

	/// <summary>
	/// Тип сравнения для фильтрации
	/// </summary>
	public enum ComparsionType
	{
		Lower,
		LowerOrEqual,
		Equal,
		Greater,
		GreaterOrEqueal,
		NotEqual,
		Like,
		IsNull,
		IsNotNull
	}

	/// <summary>
	///  Реализует функционал, упрощающий постраничную навигацию, фильтрацию и сортировку.
	/// </summary>
	/// <typeparam name="TModel">Модель данные которой будет выводиться в таблице</typeparam>
	public class ModelFilter<TModel> : IModelFilter
	{
		[Description("Конструктор запросов к БД")] protected ISession DbSession;

		[Description("Конструктор запросов к БД")] protected BaseController Controller;

		[Description("Параметры фильра. Тут хранятся все параметры.")] protected NameValueCollection Params = new NameValueCollection();

		[Description("Параметры, которые были перезаписаны пользователем")] protected List<string> OverridenParams = new List<string>();

		[Description("Список полей моделей, которые уже были использованы в фильтре. Он необходим, чтобы вывести оставшиеся поля в виде инпутов, чтобы сохранить настройки фильтрации")] protected List<string> PropertiesUsedInFilter = new List<string>();

		[Description("Конструктор запросов к БД")] protected ICriteria Criteria;

		[Description("Модели, полученные по запросу к БД")] protected IList<TModel> Models;

		[Description("Кол-во страниц, которые могут быть отображены")]
		public int PagesCount
		{
			get { return Math.Abs(TotalItems / ItemsPerPage) + (TotalItems % ItemsPerPage > 0 ? 1 : 0); }
			protected set { }
		}

		[Description("Текущая страница")]
		public int Page
		{
			get { return int.Parse(GetParam("page")); }
			protected set { }
		}

		[Description("Список параметров обработан фильтром")]
		public bool ParamsProcessed { get; set; }

		[Description("Общее количество записей, удовлетворяющих запросу")]
		public int TotalItems
		{
			get
			{
				if (!_totalItems.HasValue) {
					var tempCriteria = (ICriteria)Criteria.Clone();
					tempCriteria.SetFirstResult(0);
					tempCriteria.SetMaxResults(1000000);
					var res = tempCriteria.SetProjection(Projections.CountDistinct("Id")).UniqueResult();
					_totalItems = int.Parse(res.ToString());
				}
				return _totalItems.Value;
			}
			protected set { }
		}

		protected int? _totalItems;

		[Description("название маркера 'префикса' url параметра")]
		public string Prefix
		{
			get { return GetParam("filterPrefix"); }
			protected set { }
		}

		[Description("Количество элементов, отображаемых на старнице")]
		public int ItemsPerPage
		{
			get { return int.Parse(GetParam("itemsPerPage")); }
			protected set { }
		}

		///////////////////////

		protected Expression<Func<TModel, object>> ExpressionToGetExportValues { get; set; }
		protected NameValueCollection ExportFields = new NameValueCollection();

		/// <summary>
		/// Счетчик для имен псевдонимов. Необходим для создания уникальности имен.
		/// </summary>
		protected int AliasCounter = 0;

		/// <summary>
		/// Возвращает Url для страницы под определенным номером
		/// </summary>
		/// <param name="number">Номер страницы</param>
		/// <returns></returns>
		public string GetPageUrl(int number)
		{
			return GenerateUrl(new { page = number });
		}

		/// <summary>
		/// Конструктор фильтра
		/// </summary> 
		/// <param name="controller">Текущий контроллер ( на основе которого будут формироваться url-адреса )</param>
		public ModelFilter(BaseController controller)
		{
			Controller = controller;
			DbSession = controller.DbSession;
			//Назначение параметров по-умолчанию
			Params["page"] = "1"; //Текущая страница 
			Params["orderType"] = OrderingDirection.Asc.ToString(); //Тип сортировки (по возрастанию, по убыванию)
			Params["itemsPerPage"] = "100"; //Количество отображаемых объектов на странице
			Params["filterPrefix"] = "mfilter"; //Префикс для параметров Url
			ReadParamsFromRequest();
		}

		/// <summary>
		/// Считывание параметров переданных пользователем
		/// </summary>
		protected void ReadParamsFromRequest()
		{
			var request = Controller.Url.RequestContext.HttpContext.Request.Params;
			foreach (var key in request.AllKeys) {
				if (!key.Contains(Prefix))
					continue;
				var name = key.Replace(Prefix + ".", "");
				Params[name] = request[key];
				OverridenParams.Add(name);
			}
		}

		/// <summary>
		/// Генерация Url для фильтра.
		/// </summary>
		/// <param name="overridenParams">Дополнительные параметры. Они будут использованы вместо оригинальных, при совпадении имен.</param>
		/// <returns></returns>
		public string GenerateUrl(object overridenParams = null)
		{
			var uri = new UrlHelper(Controller.HttpContext.Request.RequestContext).Content("~");
			var urlRoot = string.Format("{0}://{1}{2}", Controller.HttpContext.Request.Url.Scheme, Controller.HttpContext.Request.Url.Authority, uri);
			// получение наименования контроллера, при его наличии
			var controllerName = Controller.Url.RequestContext.RouteData.Values.ContainsKey("controller")
				? Controller.Url.RequestContext.RouteData.Values["controller"].ToString() : "";
			// получение наименования действия, при его наличии
			var actionName = Controller.Url.RequestContext.RouteData.Values.ContainsKey("action")
				? Controller.Url.RequestContext.RouteData.Values["action"].ToString() : "";

			//Формируем базу для 
			var url = new StringBuilder(string.Format("{0}/{1}", controllerName, actionName));
			var paramSeparator = "?";

			//Клонируем параметры и добавляем туда дополнительно переданные параметры
			var paramz = Params.Clone();
			if (overridenParams != null) {
				var props = overridenParams.GetType().GetProperties();
				foreach (var prop in props) paramz[prop.Name] = prop.GetValue(overridenParams, new object[] { }).ToString();
			}

			foreach (var key in paramz.AllKeys) {
				url.Append(string.Format("{0}{1}.{2}={3}", paramSeparator, Prefix, key, paramz[key]));
				paramSeparator = "&";
			}

			return urlRoot + url.ToString();
		}

		/// <summary>
		/// Формирование адреса на основе адреса для колонок таблицы, поля , по которому будет сортироваться таблица, и направления сортировки
		/// </summary>
		/// <param name="expression">Поле, по которому будет сортироваться таблица</param>
		/// <param name="ascDefault">Направление сортировки</param>
		/// <param name="orderingDirection"></param>
		/// <returns></returns>
		public string OrderBy(Expression<Func<TModel, object>> expression)
		{
			var fieldName = ExtractFieldNameFromLambda(expression);
			//Получаем направление сортировки
			var param = GetParam("orderType");
			var type = (OrderingDirection)Enum.Parse(typeof(OrderingDirection), param);
			//Меняем тип направления на противоположный от текущего - иначе кнопка будет сортирвать только один раз
			type = type == OrderingDirection.Asc ? OrderingDirection.Desc : OrderingDirection.Asc;
			var url = GenerateUrl(new { orderBy = fieldName, orderType = type });
			return url;
		}

		/// <summary>
		/// Получение поля модели из лямбда выражения
		/// </summary>
		/// <param name="expression">Лямбда выражение</param>
		/// <returns></returns>
		protected string ExtractFieldNameFromLambda(Expression<Func<TModel, object>> expression)
		{
			string name = ""; // Наименование поля, по которому будет сортироваться таблица 

			try {
				var body = (MemberExpression)expression.Body;
				// получаем наименование поля, если оно не обернуто в Convert()
				name = body.ToString().Replace(expression.Parameters[0].ToString() + ".", "");
			}
			catch (Exception) {
				var body = expression.Body;
				// получаем наименование поля из обертки Convert()
				name = body.ToString().Replace("Convert(" + expression.Parameters[0].ToString() + ".", "");
				name = name.Substring(0, name.Length - 1); //Удаляем последнюю скобку
			}
			return name;
		}


		/// <summary>
		/// Формирование критерия на основе простого запроса, с учетом условий сортировки, лимитов или фильтра.
		/// </summary>
		/// <param name="expression">Простое лямбда выражение (поле пренадлежит модели). Сложные лямбды со свойствами могут не прокатить.</param>
		/// <returns>Критерий, который можно дополнить или, выполнив запрос, получить список запрашиваемых моделей.</returns>
		public ICriteria GetCriteria(Expression<Func<TModel, bool>> expression = null)
		{
			//Второй раз создавать критерию нет необходимости
			if (Criteria != null)
				return Criteria;

			// создаем конструктор запросов Nhibernate и добавляем в него простую лямбду, если она имеется
			Criteria = DbSession.CreateCriteria(typeof(TModel));
			if (expression != null)
				Criteria.Add(Restrictions.Where(expression));

			AddFiltersToCriteria(Criteria);
			AddOrderToCriteria(Criteria);
			AddLimitToCriteria(Criteria);
			ParamsProcessed = true;
			return Criteria;
		}

		/// <summary>
		/// Добавление ограничений на выборку. Чтобы отображать только N-noe количество записей
		/// </summary>
		/// <param name="criteria">Конструктор запросов Nhibernate</param>
		protected void AddLimitToCriteria(ICriteria criteria)
		{
			var skip = ItemsPerPage * (Page - 1);
			criteria.SetFirstResult(skip).SetMaxResults(ItemsPerPage);
		}

		/// <summary>
		/// Добавление фильтрации в запрос к БД. Фильтрацией является поиск объектов с определенными полями.
		/// Пример: поиск клиентов у которых фамилия начинается на "Раз".
		/// </summary>
		/// <param name="criteria">Конструктор запросов Nhibernate</param>
		protected void AddFiltersToCriteria(ICriteria criteria)
		{
			//Все модели группируются по Id корневой сущности
			//Потому что главная критерия достает только Id, из-за ограничений группировок Nhibernate
			criteria.SetProjection(Projections.GroupProperty("Id"));
			var filters = ExtractFilters(Params);
			foreach (var key in filters.AllKeys)
				AddFilterToCriteria(criteria, key, filters[key]);
		}

		/// <summary>
		/// Добавление фильтра к конструктору запросов к БД.
		/// </summary>
		/// <param name="criteria">Конструктор запросов</param>
		/// <param name="key">Параметр фильтра</param>
		/// <param name="value">Значение параметра</param>
		protected void AddFilterToCriteria(ICriteria criteria, string key, string value)
		{
			if (string.IsNullOrEmpty(value))
				return;

			//Сначала мы получаем тип сравнения
			var splat = key.Split('.');
			var comparsionPart = splat[1];
			var comparsionType = (ComparsionType)Enum.Parse(typeof(ComparsionType), comparsionPart);

			//Затем мы получаем имя поля и конструктор запросов для него
			//Воспользоваться изначальным конструктором мы не можем, так как он не умеет интуитивно делать join
			//Также необходимо не забыть сконвертировать значение к правильному типу
			//Ну и не забыть, привести имя параметра к пути к полю модели. В имени параметра может храниться много лишней информации
			var path = StripParamToFieldPath(key);
			criteria = GetJoinedModelCriteria(criteria, path);
			var fieldName = GetFieldName(path);
			var prop = GetModelPropertyInfo(typeof(TModel), path);

			//В зависимости от типа сравнения, задаем фильтру условие
			//проверка на значение
			var val = !(comparsionType == ComparsionType.IsNull || comparsionType == ComparsionType.IsNotNull) ?
				ConvertStringToType(value, prop.PropertyType) : new object();
			//проверка на Null
			if (comparsionType == ComparsionType.IsNull || comparsionType == ComparsionType.IsNotNull) {
				val = comparsionType == ComparsionType.IsNull && value == "1" ||
				      comparsionType == ComparsionType.IsNotNull && value == "0"
					? Restrictions.IsNull(fieldName) : Restrictions.IsNotNull(fieldName);
			}

			//Добавляем фильр к критерии
			switch (comparsionType) {
				case ComparsionType.Lower:
					criteria.Add(Restrictions.Lt(fieldName, val));
					break;
				case ComparsionType.LowerOrEqual:
					criteria.Add(Restrictions.Le(fieldName, val));
					break;
				case ComparsionType.Equal:
					criteria.Add(Restrictions.Eq(fieldName, val));
					break;
				case ComparsionType.GreaterOrEqueal:
					criteria.Add(Restrictions.Ge(fieldName, val));
					break;
				case ComparsionType.Greater:
					criteria.Add(Restrictions.Gt(fieldName, val));
					break;
				case ComparsionType.NotEqual:
					criteria.Add(Restrictions.Not(Restrictions.Eq(fieldName, val)));
					break;
				case ComparsionType.Like:
					criteria.Add(Restrictions.Like(fieldName, string.Format("%{0}%", val)));
					break;
				case ComparsionType.IsNull:
					criteria.Add((AbstractCriterion)val);
					break;
				case ComparsionType.IsNotNull:
					criteria.Add((AbstractCriterion)val);
					break;
			}
		}

		/// <summary>
		/// Приведение строки к определенному типу данных
		/// </summary>
		/// <param name="value">Значение</param>
		/// <param name="propertyType">Тип данных</param>
		/// <returns></returns>
		protected object ConvertStringToType(string value, Type propertyType)
		{
			if (propertyType.FullName.Contains(typeof(string).Name))
				return value;

			if (propertyType.FullName.Contains(typeof(bool).Name)) {
				if (value.Contains("true"))
					return true;
				return false;
			}

			if (propertyType.FullName.Contains(typeof(DateTime).Name))
				return DateTime.Parse(value);

			if (propertyType.FullName.Contains(typeof(Int32).Name))
				return Int32.Parse(value);

			if (propertyType.IsEnum)
				return Enum.Parse(propertyType, value);

			throw new Exception(string.Format("Не получается привести значение '{0}' к типу {1}", value, propertyType.Name));
		}

		/// <summary>
		/// Получение информации о поле модели из пути к полю
		/// </summary>
		/// <param name="type">Тип базовой модели (от которой начинается вложенность)</param>
		/// <param name="fieldPath">Путь к полю</param>
		/// <returns></returns>
		protected PropertyInfo GetModelPropertyInfo(Type type, string fieldPath)
		{
			var split = fieldPath.Split('.');
			if (type.IsGenericType)
				type = type.GetGenericArguments().FirstOrDefault();
			var info = type.GetProperty(split[0]);
			if (split.Count() == 1)
				return info;

			fieldPath = string.Join(".", split, 1, split.Count() - 1);
			return GetModelPropertyInfo(info.PropertyType, fieldPath);
		}

		/// <summary>
		/// Получение конструктора запросов Nhibernate для вложенного поля модели.
		/// </summary>
		/// <param name="criteria">Конструктор запросов для базовой модели</param>
		/// <param name="fieldPath">Путь к полю целевой модели</param>
		/// <returns></returns>
		protected ICriteria GetJoinedModelCriteria(ICriteria criteria, string fieldPath)
		{
			var splat = fieldPath.Split('.');
			//Если нет точки, то это обращение не ко вложенной модели и псевдоним не нужен
			if (splat.Count() == 1)
				return criteria;

			var joinedModelFieldName = splat[0];
			var alias = CreateAliasName(criteria, joinedModelFieldName);
			//Делаем JOIN с другой таблицей и получает его конструктор запросов
			//Join можно делать только 1 раз, так что необходимо проверить не был ли он сделан ранее
			var joined = criteria.GetCriteriaByAlias(alias) ?? criteria.CreateCriteria(joinedModelFieldName, alias, JoinType.LeftOuterJoin);
			criteria = joined;

			if (splat.Count() == 2)
				return criteria;

			var subpath = string.Join(".", splat, 1, splat.Count() - 1);
			return GetJoinedModelCriteria(criteria, subpath);
		}

		/// <summary>
		/// Генерация имени алиаса для пути к полю. Если алиас не нужен, то возвращается NULL
		/// </summary>
		/// <param name="fieldPath"></param>
		/// <returns></returns>
		protected string CreateAliasName(ICriteria criteria, string fieldName)
		{
			var aliasName = criteria.Alias + fieldName;
			return aliasName;
		}

		/// <summary>
		/// Получение параметров, отвечающих за фильтры из списка параметров
		/// </summary>
		/// <param name="collection">Список параметров</param>
		/// <returns></returns>
		protected NameValueCollection ExtractFilters(NameValueCollection collection)
		{
			var result = new NameValueCollection();
			var filterword = "filter.";
			foreach (var key in collection.AllKeys) {
				if (key.Length > filterword.Length && key.Substring(0, filterword.Length) == filterword)
					result.Add(key, collection[key]);
			}
			return result;
		}

		/// <summary>
		/// Добавление сортировки в запрос к Бд. Чтобы можно было отсортировать по каким-либо полям модели.
		/// </summary>
		/// <param name="criteria">Конструктор запросов Nhibernate</param>
		protected void AddOrderToCriteria(ICriteria criteria)
		{
			//Получаем поле по которому сортируем
			var path = GetParam("orderBy");
			if (path == null)
				return;

			//Получаем путь к полю модели
			criteria = GetJoinedModelCriteria(criteria, path);
			var fieldName = GetFieldName(path);
			//Направление сортировки
			var orderType = (OrderingDirection)Enum.Parse(typeof(OrderingDirection), GetParam("orderType"));
			if (orderType == OrderingDirection.Asc)
				criteria.AddOrder(Order.Asc(fieldName));
			else
				criteria.AddOrder(Order.Desc(fieldName));
		}

		/// <summary>
		/// Получение имени поля из пути к полю
		/// </summary>
		/// <param name="path">Путь к полю</param>
		/// <returns></returns>
		protected string GetFieldName(string path)
		{
			var splat = path.Split('.');
			var field = splat[splat.Count() - 1];
			return field;
		}


		/// <summary>
		/// Получение параметра объекта
		/// </summary>
		/// <param name="name">Имя параметра</param>
		/// <returns></returns>
		public string GetParam(string name)
		{
			return Params[name];
		}

		/// <summary>
		/// Ручной фильтр. Задачет имя параметра, который необходимо будет передать на сервер
		/// </summary>
		/// <param name="name">Имя параметра</param>
		/// <param name="type">Тип HTML контрола</param>
		/// <param name="additional">Дополнительные параметры</param>
		/// <returns></returns>
		public HtmlString FormFilterManual(string name, HtmlType type, object additional = null)
		{
			var attrs = new Dictionary<string, string>();
			if (Params[name] != null)
				attrs["value"] = Params[name];

			attrs["name"] = Prefix + "." + name;
			attrs["class"] = "form-control";
			PropertiesUsedInFilter.Add(name);
			var html = GenerateHtml(type, attrs, ComparsionType.Equal, additional);
			var ret = new HtmlString(html);
			return ret;
		}

		/// <summary>
		/// Элемент фильтра
		/// </summary>
		/// <param name="expression">фильтруемый параметр</param>
		/// <param name="type">Тэг</param>
		/// <param name="comparsionType">Тип сравнения</param>
		/// <param name="htmlAttributes">Свойства тэга</param>
		/// <param name="additional">Опциональные настройки</param>
		/// <returns></returns>
		public HtmlString FormFilter(Expression<Func<TModel, object>> expression, HtmlType type, ComparsionType comparsionType, object htmlAttributes = null, object additional = null)
		{
			var name = ExtractFieldNameFromLambda(expression);
			var attrs = ObjectToDictionary(htmlAttributes);
			var inputName = string.Format("{0}.filter.{1}.{2}", Prefix, comparsionType, name);

			if (!attrs.ContainsKey("name"))
				attrs["name"] = inputName;
			if (!attrs.ContainsKey("class"))
				attrs["class"] = "form-control";

			//Подставляем предыдущее отправленное значение, если оно есть
			//Из него надо выдрать префикс, так как при получении параметров префикс удаляется
			var paramName = inputName.Replace(Prefix + ".", "");
			if (Params[paramName] != null)
				attrs["value"] = Params[paramName];

			PropertiesUsedInFilter.Add(paramName);
			var html = GenerateHtml(type, attrs, comparsionType, additional);
			var ret = new HtmlString(html);
			return ret;
		}

		/// <summary>
		/// Создает необходимые инпуты для фильтрации, при помощи Get формы.
		/// </summary>
		/// <returns></returns>
		public HtmlString GenerateInputs()
		{
			if (Params.Count == 0)
				return new HtmlString("");
			var builder = new StringBuilder();
			var keys = Params.AllKeys;

			foreach (var key in keys) {
				if (!PropertiesUsedInFilter.Contains(key))
					builder.Append(string.Format("<input style='display: none' name='{0}.{1}' value='{2}' />", Prefix, key, Params[key]));
			}

			var html = new HtmlString(builder.ToString());
			return html;
		}

		/// <summary>
		/// Генерирует HTML код для различных контролов
		/// </summary>
		/// <param name="type">Тип контрола</param>
		/// <param name="o">Словарь с html аттрибутами</param>
		/// <param name="comparsionType"></param>
		/// <param name="additional">Дополнительные параметры - могут принимать любую форму. Необходимы для некоторых типов элементов.</param>
		/// <returns></returns>
		protected string GenerateHtml(HtmlType type, Dictionary<string, string> o, ComparsionType comparsionType, object additional = null)
		{
			o["class"] = " form-control " + o["class"];
			//заполняем выбранное значение, если оно есть
			string selectedValue = null;
			if (o.ContainsKey("value"))
				selectedValue = o["value"];

			if (type == HtmlType.Date) {
				o["class"] = " datepicker " + o["class"];
				o["date-format"] = "dd.nn.yyyy";
				o["data-provide"] = "datepicker-inline";
				return string.Format("<input {0} />", GetPropsValues(o));
			}

			if (type == HtmlType.text) {
				return string.Format("<input type='text' {0} />", GetPropsValues(o));
			}

			if (type == HtmlType.checkbox) {
				o["class"] = "c-pointer " + o["class"];
				var selectedPart = selectedValue != null && selectedValue.Contains("true") ? " checked=checked" : "";
				return string.Format("<input type='checkbox' value = 'true' {0} {1}/><input type='hidden' value='false' {0} >", GetPropsValues(o).Replace("form-control", ""), selectedPart);
			}

			if (type == HtmlType.Dropdown) {
				List<string> values;
				var attrName = o["name"];

				var customValueList = additional as NameValueCollection;
				if (customValueList != null)
					values = customValueList.AllKeys.Select(i => string.Format("<option {2} value='{0}'>{1}</option>", i, customValueList[i], selectedValue == i ? "selected='selected'" : "")).OrderBy(s => s).ToList();
				else
					values = TryToGetDropDownValueList(comparsionType, attrName, selectedValue);

				return string.Format("<select {0}>{1}</select>", GetPropsValues(o), string.Join("\n", values));
			}
			return "";
		}

		/// <summary>
		/// Попытка автоматически получить значения для выпадающего списка
		/// </summary>
		/// <param name="comparsionType"></param>
		/// <param name="attrName">Имя значения, которое будет использовано в аттрибует name у html тега</param>
		/// <param name="selectedValue">Значение, которое было выбрано пользователем</param>
		/// <returns></returns>
		protected List<string> TryToGetDropDownValueList(ComparsionType comparsionType, string attrName, string selectedValue = null)
		{
			var propPath = StripParamToFieldPath(attrName);
			var prop = GetModelPropertyInfo(typeof(TModel), propPath);
			if (prop.PropertyType.IsEnum)
				return GetEnumDropDownValues(prop.PropertyType, selectedValue);
			if (comparsionType == ComparsionType.IsNull || comparsionType == ComparsionType.IsNotNull)
				return GetDefaultDropDownValues(selectedValue);
			return GetModelDropDownValues(prop, selectedValue);
		}

		private List<string> GetDefaultDropDownValues(string selectedValue)
		{
			var values = new List<string>();
			var seletedString = selectedValue == null ? "selected = 'selected'" : "";
			values.Add(string.Format("<option {0} value=''> </option>", seletedString));
			seletedString = selectedValue == 1.ToString() ? "selected = 'selected'" : "";
			values.Add(string.Format("<option {0} value='1'>Да</option>", seletedString));
			seletedString = selectedValue == 0.ToString() ? "selected = 'selected'" : "";
			values.Add(string.Format("<option {0} value='0'>Нет</option>", seletedString));
			return values;
		}

		/// <summary>
		/// Создание верстки для выпадающего списка из перечисления
		/// </summary>
		/// <param name="prop">Свойство модели для которого необходимо сделать перечисление</param>
		/// <param name="selectedValue">Значение, которое было выбрано пользователем</param>
		/// <returns></returns>
		protected List<string> GetModelDropDownValues(PropertyInfo prop, string selectedValue = null)
		{
			//Для булевых типов проще сразу вернуть результат без запросов к БД
			if (prop.PropertyType == typeof(bool))
				return GetBooleanDropDownValues(selectedValue);

			var values = new List<string>();
			var model = prop.DeclaringType;
			var criteria = DbSession.CreateCriteria(model.Name);
			//todo Тут проще будет сделать группировку, но сейчас время поджимает
			criteria.SetResultTransformer(new DistinctRootEntityResultTransformer());

			var models = criteria.List();

			foreach (var obj in models) {
				var value = prop.GetValue(obj, new object[] { });
				if (value == null || value.GetType().Name.ToLower().IndexOf("proxy") != -1) {
					continue;
				}
				var selected = value.ToString() == selectedValue ? "selected='selected'" : "";
				values.Add(string.Format("<option {0} value='{1}'>{2}</option>", selected, value, value));
			}
			values = values.OrderBy(s => s).ToList();
			values.Insert(0, "<option value=''> </option>");
			return values;
		}

		/// <summary>
		/// Создание верстки для выпадающего списка для булевского значения в поле модели
		/// </summary>
		/// <param name="selectedValue">Значение, которое было выбрано пользователем</param>
		/// <returns></returns>
		protected List<string> GetBooleanDropDownValues(string selectedValue = null)
		{
			var selectedTrue = "true" == selectedValue ? "selected='selected'" : "";
			var selectedFalse = "false" == selectedValue ? "selected='selected'" : "";
			var values = new List<string>();
			values.Add("<option value=''> </option>");
			values.Add(String.Format("<option {0} value='true'>Да</option>", selectedTrue));
			values.Add(String.Format("<option {0} value='false'>Нет</option>", selectedFalse));
			return values;
		}

		/// <summary>
		/// Создание верстки для выпадающего списка из перечисления
		/// </summary>
		/// <param name="type">Тип перечисления</param>
		/// <param name="selectedValue">Значение, которое было выбрано пользователем</param>
		/// <returns></returns>
		protected List<string> GetEnumDropDownValues(Type type, string selectedValue = null)
		{
			var values = new List<string>();
			var names = type.GetEnumNames();
			foreach (var name in names) {
				var value = Enum.Parse(type, name);
				var description = value.GetDescription();
				var selected = value.ToString() == selectedValue ? "selected='selected'" : "";
				values.Add(string.Format("<option {0} value='{1}'>{2}</option>", selected, value, description));
			}
			values = values.OrderBy(s => s).ToList();
			values.Insert(0, "<option value=''> </option>");
			return values;
		}

		/// <summary>
		/// Вытаскивание пути к полю модели из параметра фильтра.
		/// То есть удаление всех данных фильтра из названия GET параметра
		/// </summary>
		/// <param name="name">Get параметр вида mfilter.filter.PhysicalClient.Services.First().ServiceType.Name</param>
		/// <returns></returns>
		protected string StripParamToFieldPath(string name)
		{
			var badValues = new List<string>();
			badValues.Add(Prefix);
			badValues.Add("First()");
			badValues.Add("filter");
			var enumValues = Enum.GetNames(typeof(ComparsionType));
			badValues.AddRange(enumValues);

			var newValues = new List<string>();
			var splat = name.Split('.');
			foreach (var item in splat) {
				if (!badValues.Contains(item))
					newValues.Add(item);
			}
			var ret = string.Join(".", newValues);
			return ret;
		}

		/// <summary>
		/// Конвертация словаря, в строку c html аттрибутами
		/// </summary>
		/// <param name="obj">Словарь, создержащий названия и значения аттрибутов</param>
		/// <returns>Строка вида "name1='value1' name2='value2'"</returns>
		protected string GetPropsValues(Dictionary<string, string> obj)
		{
			var sb = new StringBuilder();
			foreach (var key in obj.Keys) sb.Append(string.Format(" {0}='{1}'", key, obj[key]));
			return sb.ToString();
		}

		/// <summary>
		/// Получение списка моделей.
		/// </summary>
		/// <returns>Список моделей</returns>
		public IList<TModel> GetItems()
		{
			if (Models == null)
				Execute();
			return Models;
		}

		/// <summary>
		/// Выполнение SQL кода и заполнение списка моделей
		/// </summary>
		/// <returns></returns>
		public void Execute()
		{
			var criteria = GetCriteria();

			var list = criteria.List();
			//из-за ограничений группировок и возможных join'ов мы получаем только идентификаторы моделей
			//а потом отдельным запросом забираем модели. Вот так!
			var realCriteria = DbSession.CreateCriteria(typeof(TModel));
			AddOrderToCriteria(realCriteria);
			Models = realCriteria.Add(Restrictions.In("Id", list)).List<TModel>();
		}

		/// <summary>
		/// Получение SQL кода запроса на поиск моделей
		/// </summary>
		/// <returns></returns>
		public string GetSql(ICriteria criteria)
		{
			CriteriaImpl c = (CriteriaImpl)criteria;
			SessionImpl s = (SessionImpl)c.Session;
			ISessionFactoryImplementor factory = s.Factory;
			String[] implementors = factory.GetImplementors(c.EntityOrClassName);
			CriteriaLoader loader = new CriteriaLoader((IOuterJoinLoadable)factory.GetEntityPersister(implementors[0]),
				factory, c, implementors[0], s.EnabledFilters);
			var str = loader.ToString();
			return str;
		}

		/// <summary>
		/// Настройка сортировки.
		/// </summary>
		/// <param name="column">Поле, по которому нужно сортировать</param>
		/// <param name="direction">Направление сортировки</param>
		public void SetOrderBy(string column, OrderingDirection direction = OrderingDirection.Asc)
		{
			Params["orderBy"] = column;
			Params["orderType"] = direction.ToString();
		}

		/// <summary>
		/// Назначение количества объектов, выводимых на странице.
		/// </summary>
		/// <param name="number">Количество объектов</param>
		public void SetItemsPerPage(int number)
		{
			Params["itemsPerPage"] = number.ToString();
		}

		/// <summary>
		/// Назначение префикса для параметров фильтра. 
		/// К примеру, если на странице будет 2 фильтра
		/// </summary>
		/// <param name="prefix">Название префикса</param>
		public void SetPrefix(string prefix)
		{
			Params["filterPrefix"] = prefix;
		}

		/// <summary>
		/// Генерация кнопки отправки формы для фильтра
		/// </summary>
		/// <param name="attributes">Анонимный объект заполненый HTML аттрибутами</param>
		/// <param name="generateInputs">Создавать скрытые инпуты с оставльными значениями параметров, чтобы не терялись параметры фильтра при повторной фильтрации</param>
		/// <returns></returns>
		public HtmlString SubmitButton(object attributes = null, bool generateInputs = true)
		{
			var hiddenInputs = generateInputs ? GenerateInputs().ToString() : "";
			var dic = ObjectToDictionary(attributes);
			dic["type"] = "submit";
			dic["value"] = "Поиск";
			dic["class"] = "btn btn-success";
			var htmlAttributs = GetPropsValues(dic);

			var ret = string.Format("{0} <input {1} />", hiddenInputs, htmlAttributs);
			return new HtmlString(ret);
		}

		/// <summary>
		/// Генерация кнопки для экспорта в Excel. Кнопка должна сопровождаться вызовом методов IsExportRequested() и ExportToExcel() на стороне контроллера.
		/// </summary>
		/// <param name="attributes">Анонимный объект заполненый HTML аттрибутами</param>
		/// <returns></returns>
		public HtmlString ExportButton(object attributes = null)
		{
			var dic = ObjectToDictionary(attributes);
			dic["type"] = "submit";
			dic["name"] = Prefix + ".export";
			dic["value"] = "Печать";
			var htmlAttributs = GetPropsValues(dic);

			var ret = string.Format("<input {0} />", htmlAttributs);
			return new HtmlString(ret);
		}

		/// <summary>
		/// Генерация кнопки отчистки фильтров
		/// </summary>
		/// <param name="currentUrl">Url путь к странице</param>
		/// <param name="attributes">Анонимный объект заполненый HTML аттрибутами</param>
		/// <returns></returns>
		public HtmlString CleanButton(object attributes = null)
		{
			string currentUrl = "/";
			if (Controller.ControllerContext.HttpContext.Request.Url != null) {
				currentUrl = Controller.ControllerContext.HttpContext.Request.Url.AbsolutePath;
			}
			var dic = ObjectToDictionary(attributes);
			var htmlAttributs = GetPropsValues(dic);
			var ret = string.Format("<a href='{0}' {1}>Отчистить</a>", currentUrl, htmlAttributs);
			return new HtmlString(ret);
		}

		/// <summary>
		/// Проверяет, нажал ли пользователь на кнопку экспорта
		/// </summary>
		/// <returns></returns>
		public bool IsExportRequested()
		{
			var export = GetParam("export");
			return !string.IsNullOrEmpty(export);
		}

		/// <summary>
		/// Конвертация анонимного объекта в список
		/// </summary>
		/// <param name="obj">Анонимный объект вида { Name = "value" }</param>
		/// <returns></returns>
		protected Dictionary<string, string> ObjectToDictionary(object obj)
		{
			var dic = new Dictionary<string, string>();
			if (obj == null)
				return dic;

			var props = obj.GetType().GetProperties();
			foreach (var prop in props) {
				var value = prop.GetValue(obj, new object[] { });
				dic.Add(prop.Name, value.ToString());
			}
			return dic;
		}

		/// <summary>
		/// Поля которые будут выводится на экспорт в документ.
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		private List<string> GetExportPaths(object model)
		{
			List<string> paths = new List<string>();
			for (int i = 0; i < ExportFields.Count; i++) paths.Add(ExportFields[i]);
			return paths;
		}

		/// <summary>
		/// Получение поля модели из лямбда выражения
		/// </summary>
		/// <param name="criteria">Критерия</param>
		/// <param name="expression">Лямбда выражение</param>
		/// <param name="complexLinq">Сложное лямбда выражение (с вычислением значений, условий)</param>
		/// <returns></returns>
		public void SetExportFields(Expression<Func<TModel, object>> expression, bool complexLinq = false)
		{
			var criteria = GetCriteria();
			//удаление лимитов, получение данных для экспорта в файл Excel-
			criteria.SetFirstResult(0).SetMaxResults(1000000);
			//получение списка моделей
			var orderList = criteria.List();
			var realCriteria = DbSession.CreateCriteria(typeof(TModel));
			AddOrderToCriteria(realCriteria);
			Models = realCriteria.Add(Restrictions.In("Id", orderList)).List<TModel>();

			//взврат лимитов
			criteria.SetFirstResult(ItemsPerPage * (Page - 1)).SetMaxResults(ItemsPerPage);
			if (Models == null || Models.Count == 0) {
				return;
			}
			if (complexLinq) {
				ExpressionToGetExportValues = expression;
				return;
			} 
			//получаем первую модель 
			var modelForParams = Models[0];
			//обработка выражения
			var parametre = expression.Parameters[0].ToString();
			var stringExpressionBody = expression.Body.ToString();
			int subStringStart = stringExpressionBody.IndexOf("(") + 1;
			int subStringLength = (stringExpressionBody.IndexOf(")") - subStringStart);
			var stringExpressionCut = stringExpressionBody.Substring(subStringStart, subStringLength);
			stringExpressionCut = stringExpressionCut.Replace(" " + parametre + ".", "");
			stringExpressionCut = stringExpressionCut.Replace(" ", "");
			var arrayExpressionCut = stringExpressionCut.Split(',');
			ExportFields.Clear();
			//добавление полей выгрузки
			arrayExpressionCut.Each(s => {
				if (s.IndexOf("=") != -1) {
					var temp = s.ToString().Split('=');
					temp[0] = temp[0] == parametre ? modelForParams.GetDescription() : temp[0];
					var r = temp[1].Split('.').LastOrDefault(last => last == temp[0]);
					temp[0] = r == null ? temp[0] :
						temp[1].IndexOf(".") != -1 ? GetModelPropertyName(modelForParams, temp[1]) : modelForParams.GetDescription(temp[1]);
					ExportFields.Add(temp[0].Replace("_", " ").Trim(), temp[1]);
				}
			});
		}


		/// <summary>
		/// Получение значения объекта. Модель - Id, список - длина, по-умолчанию - ToString()
		/// </summary>
		/// <param name="itemToReturn">Значение</param>
		/// <returns></returns>
		private string getValueByType(object itemToReturn)
		{
			if (itemToReturn as IList != null) {
				return (itemToReturn as IList).Count.ToString();
			}
			if (itemToReturn as BaseModel != null) {
				return (itemToReturn as BaseModel).Id.ToString();
			}
			return itemToReturn != null ? itemToReturn.ToString() : "";
		}

		/// <summary>
		/// Получение значение поля по его наименованию
		/// </summary>
		/// <param name="model">Модель</param>
		/// <param name="fieldName">Наименование поля с полной дирректорией к нему. пример: 'PhysicalClient.Address.House.Number' - Номер дома у модели 'Client' </param>
		/// <returns>Значение поля</returns>
		private string GetModelPropertyName(object model, string fieldName)
		{
			var split = fieldName.Split('.');
			if (split.Count() == 1) {
				return model.GetDescription(fieldName);
			}

			fieldName = string.Join(".", split, 1, split.Count() - 1);
			return GetModelPropertyName(model.GetType().GetProperty(split[0]).GetValue(model, null), fieldName);
		}

		/// <summary>
		/// Получение значение поля по его наименованию
		/// </summary>
		/// <param name="model">Модель</param>
		/// <param name="fieldName">Наименование поля с полной дирректорией к нему. пример: 'PhysicalClient.Address.House.Number' - Номер дома у модели 'Client' </param>
		/// <returns>Значение поля</returns>
		private string GetModelPropertyValue(object model, string fieldName)
		{
			var split = fieldName.Split('.');
			if (model.GetType().GetProperty(split[0]) == null) {
				return getValueByType(model);
			}
			if (model.GetType().GetProperty(split[0]) != null && model.GetType().GetProperty(split[0]).GetValue(model, null) == null) {
				return "";
			}
			if (split.Count() == 1) {
				var itemToReturn = model.GetType().GetProperty(split[0]) != null ? model.GetType().GetProperty(split[0]).GetValue(model, null) : model;
				return getValueByType(itemToReturn);
			}

			fieldName = string.Join(".", split, 1, split.Count() - 1);
			return GetModelPropertyValue(model.GetType().GetProperty(split[0]).GetValue(model, null), fieldName);
		}

		/// <summary>
		/// Получение описания поля
		/// </summary>
		/// <param name="model">Модель</param>
		/// <param name="fieldName">Наименование поля с полной дирректорией к нему. пример: 'PhysicalClient.Address.House.Number' - Номер дома у модели 'Client' </param>
		/// <returns>Описания поля</returns>
		private string GetModelFieldName(string fieldName)
		{
			for (int i = 0; i < ExportFields.Count; i++) {
				if (ExportFields[i] == fieldName) {
					return ExportFields.GetKey(i);
				}
			}
			return fieldName;
		}

		/// <summary>
		/// Получение списка для экспорта в таблицу Excel
		/// </summary>
		/// <returns></returns>
		public List<string[]> ExportToList()
		{
			// инициализация списка
			var propertiesToExport = new List<string[]>();
			// формирование шапки таблицы по первой моделе в списке
			var pathsToExportForNames = GetExportPaths(Models[0]);
			var nameValuesToExport = new string[pathsToExportForNames.Count];
			for (int j = 0; j < nameValuesToExport.Length; j++) nameValuesToExport[j] = GetModelFieldName(pathsToExportForNames[j]);
			propertiesToExport.Add(nameValuesToExport);
			// заполнение таблицы значениями
			for (int i = 0; i < Models.Count; i++) {
				var pathsToExport = GetExportPaths(Models[i]);
				var valuesToExport = new string[pathsToExport.Count];
				for (int j = 0; j < valuesToExport.Length; j++) valuesToExport[j] = GetModelPropertyValue(Models[i], pathsToExport[j]);
				propertiesToExport.Add(valuesToExport);
			}
			// возврат сформированного списка
			return propertiesToExport;
		}

		/// <summary>
		/// Формирование Excel документа 
		/// </summary>
		/// <returns>Excel документ</returns>
		public void ExportToExcelFile(System.Web.HttpContextBase context, string excelDocumentName = "excel_document")
		{
			if (Models == null || Models.Count == 0) {
				return;
			}
			const int pixelsFont = 11;
			const int pixelsForColumnWidth = pixelsFont * 34;
			byte[] fileToreturn = new byte[0];
			var propertiesToExport = ExpressionToGetExportValues != null ? ExportToListByLinq() : ExportToList();
			if (propertiesToExport.Count > 0) {
				//создаем новый xls файл 
				var workbook = new Workbook();
				var worksheet = new Worksheet("First Sheet");

				for (int i = 0; i < propertiesToExport[0].Length; i++) worksheet.Cells.ColumnWidth[(ushort)i] = 0;
				// проходим строки
				for (int i = 0; i < propertiesToExport.Count; i++) {
					// столбцы
					for (int j = 0; j < propertiesToExport[i].Length; j++) {
						//записываем значения в ячейки
						worksheet.Cells[i, j] = new Cell(propertiesToExport[i][j]);
						worksheet.Cells.ColumnWidth[(ushort)j] = worksheet.Cells.ColumnWidth[(ushort)j] < propertiesToExport[i][j].Length
							? (ushort)propertiesToExport[i][j].Length : worksheet.Cells.ColumnWidth[(ushort)j];
					}
				}
				for (int i = 0; i < propertiesToExport[0].Length; i++) {
					worksheet.Cells.ColumnWidth[(ushort)i] = (ushort)(worksheet.Cells.ColumnWidth[(ushort)i] * pixelsForColumnWidth);
					worksheet.Cells[0, i].Style = new CellStyle() { Font = new Font("Arial", pixelsFont) { Bold = true } };
				}

				workbook.Worksheets.Add(worksheet);
				using (var ms = new MemoryStream()) {
					workbook.Save(ms);
					fileToreturn = ms.ToArray();
				}
			}
			else {
				new Exception("Не заданы поля выборки! *(для этого используеться метод 'SetExportFields')");
			}
			context.Response.ContentType = "MS-Excel/xls";
			context.Response.AppendHeader("Content-Disposition", "attachment; filename=" + excelDocumentName + ".xlsx");
			context.Response.BinaryWrite(fileToreturn);
			context.Response.Flush();
			context.Response.Close();
		}


		/// <summary>
		/// Получение списка для экспорта в таблицу Excel
		/// </summary>
		/// <returns></returns>
		public List<string[]> ExportToListByLinq()
		{
			// инициализация списка
			var propertiesToExport = new List<string[]>();
			var exp = ExpressionToGetExportValues.Compile();
			var valModel = exp(Models[0]);
			var tpModel = valModel.GetType();
			var nameValuesToExport = tpModel.GetProperties().Select(s => s.Name).ToList();
			propertiesToExport.Add(nameValuesToExport.Select(s => s.Replace("_", " ").Trim()).ToArray());
			// заполнение таблицы значениями
			for (int i = 0; i < Models.Count; i++) {
				var valuesToExport = new string[nameValuesToExport.Count];
				for (int j = 0; j < nameValuesToExport.Count; j++) {
					var valRe = exp(Models[i]);
					var tp = valRe.GetType();
					var pinfo = tp.GetProperty(nameValuesToExport[j]);
					valuesToExport[j] = pinfo.GetValue(valRe, null).ToString();
					;
				}
				propertiesToExport.Add(valuesToExport);
			}
			// возврат сформированного списка
			return propertiesToExport;
		}


		/// <summary>
		///  Фильтрация проведена пользователем (в запросе присуствуют условия фильтрации)
		/// </summary>
		public bool IsExecutedByUser()
		{
			var request = Controller.Url.RequestContext.HttpContext.Request.Params;
			var isUsed = request.AllKeys.Any(s => s.Contains(Prefix));
			return isUsed;
		}

		/// <summary>
		/// Проверка наличия параметра в списке параметров фильтрации
		/// </summary>
		public bool ParamExists(string parametreName)
		{
			return Params.AllKeys.FirstOrDefault(s => s == parametreName) != null;
		}

		/// <summary>
		/// Добавление параметра в список параметров фильтрации
		/// </summary>
		public void ParamSet(string parametreName, string parametreValue)
		{
			if (ParamsProcessed) {
				throw new Exception("Невозможно добавить параметр: параметры уже были обработаны фильтром! *Добавление возможно только до метода GetCriteria().");
			}
			Params[parametreName] = parametreValue;
		}

		/// <summary>
		/// Получение параметра из списка параметров фильтрации
		/// </summary>
		public string ParamGet(string parametreName)
		{
			return Params[parametreName];
		}

		/// <summary>
		/// Удаление параметра из списка параметров фильтрации
		/// </summary>
		public void ParamDelete(string parametreName)
		{
			if (ParamsProcessed) {
				throw new Exception("Невозможно удалить параметр: параметры уже были обработаны фильтром! *Удаление возможно только до метода GetCriteria().");
			}
			Params.Remove(parametreName);
		}
	}
}