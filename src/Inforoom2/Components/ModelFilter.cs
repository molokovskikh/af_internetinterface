using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Helpers;
using Microsoft.VisualBasic.CompilerServices;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Engine;
using NHibernate.Impl;
using NHibernate.Linq;
using NHibernate.Loader;
using NHibernate.Loader.Criteria;
using NHibernate.Mapping;
using NHibernate.Mapping.ByCode.Impl.CustomizersImpl;
using NHibernate.Persister.Entity;
using NHibernate.Validator.Util;

namespace Inforoom2.Components
{
	/// <summary>
	///  Необходим для обращения к полям класса, реализующего функционал, 
	///  упрощающий постраничную навигацию, фильтрацию и сортировку, без указания шаблона.
	/// </summary>
	public interface IModelFilter
	{
		int PagesCount { get; }					 // Кол-во страниц
		int Page { get; }						 // Текущая страница
		int TotalItems { get; }					 // Общее количество записей, удовлетворяющих запросу
		int ItemsPerPage { get; }				 // Кол-во записей на страницу   
		string OrderByColumn { get; }			 // Наименование поля, по которому будет сортироваться таблица
		bool AscOrder { get; }					 // Направление сортировки
		string SearchParam { get; }				 // Поле, по которому будет производится фильтрация
		string SearchText { get; }				 // Значения фильтра
		string UrlBase { get; }					 // Адрес основной
		string UrlForColumns { get; }			 // Адрес для колонок
		string UrlPagePrevious { get; }			 // Адрес предыдущей страницы
		string UrlPageNext { get; }				 // Адрес следующей страницы
		string UrlRootBase { get; }				 // Адрес корневой


		// Наименование параметров в адресе
		string UrlParamPrefix { get; }
		string UrlSimpleSearch { get; }
		string UrlSearchDirection { get; }
		string UrlCurrentPage { get; }
		string UrlItemsPerPage { get; }
		string UrlOrderByColumn { get; }
	}
	/// <summary>
	///  Реализует функционал, упрощающий постраничную навигацию, фильтрацию и сортировку.
	/// </summary>
	/// <typeparam name="TModel"></typeparam>
	public class ModelFilter<TModel> : IModelFilter
	{
		private ISession dbSession = MvcApplication.SessionFactory.OpenSession();

		protected NameValueCollection Params;
		protected List<string> FilterNames;
		protected ICriteria Criteria;
		protected IList<TModel> Models;

		[Description("Кол-во страниц")]
		public int PagesCount {
			get
			{
				return Math.Abs(TotalItems / ItemsPerPage) + (TotalItems % ItemsPerPage > 0 ? 1 : 0);
			} private set{} 
		}

		[Description("Текущая страница")]
		public int Page { get; private set; }

		private int? _totalItems;
		[Description("Общее количество записей, удовлетворяющих запросу")]
		public int TotalItems {
			get
			{
				if (!_totalItems.HasValue) {
					Criteria.SetFirstResult(0);
					Criteria.SetMaxResults(1000000);
					var res =  Criteria.SetProjection(Projections.RowCount()).UniqueResult();
					_totalItems = int.Parse(res.ToString());
				}
				return _totalItems.Value;
			}
			private set{}
		}

		[Description("Кол-во записей на страницу ")]
		public int ItemsPerPage { get; private set; }

		[Description("Наименование поля, по которому будет сортироваться таблица")]
		public string OrderByColumn { get; private set; }

		[Description("Направление сортировки")]
		public string SearchParam { get; private set; }

		[Description("Поле, по которому будет производится фильтрация")]
		public string SearchText { get; private set; }

		[Description("Значения фильтра")]
		public bool AscOrder { get; private set; }

		[Description("Адрес основной")]
		public string UrlBase { get; private set; }

		[Description("Адрес корневой")]
		public string UrlRootBase { get; private set; }

		[Description("Адрес для колонок")]
		public string UrlForColumns { get; private set; }

		[Description("Адрес предыдущей страницы")]
		public string UrlPagePrevious
		{
			get
			{
				return Page <= 1 ? "" : "href=" + UrlBase +
					(UrlBase.Contains("?") ? "&" : "?") + UrlCurrentPage + "=" + (Page - 1);
			}
		}

		[Description("Адрес следующей страницы")]
		public string UrlPageNext
		{
			get
			{
				return Page >= PagesCount ? "" : "href=" + UrlBase +
					(UrlBase.Contains("?") ? "&" : "?") + UrlCurrentPage + "=" + (Page + 1);
			}
		}

		[Description("название маркера 'префикса' url параметра")]
		public string UrlParamPrefix
		{
			get { return "mfilter"; }
		}

		[Description("название маркера 'поиска' url параметра")]
		public string UrlSimpleSearch
		{
			get { return "search"; }
		}

		[Description("название маркера 'направления сортировки' url параметра")]
		public string UrlSearchDirection
		{
			get { return "dirrection"; }
		}

		[Description("название маркера 'текущей страницы' url параметра")]
		public string UrlCurrentPage
		{
			get { return "page"; }
		}

		[Description("название маркера 'эл-тов на страницу' url параметра")]
		public string UrlItemsPerPage
		{
			get { return "per"; }
		}

		[Description("название маркера 'поля сортировки' url параметра")]
		public string UrlOrderByColumn
		{
			get { return "orderBy"; }
		}

		private const string urlController = "controller";
		private const string urlAction = "action";

		/// <summary>
		///   Упрощение постраничной навигации, фильтрации и сортировки 
		/// </summary> 
		/// <remarks>
		/// <para><c>Пример запросов:</c> </para> 
		/// <para>?mfilter.page = текущая страница</para> 
		/// <para>?mfilter.per = кол-во элементов на страницу</para> 
		/// <para>и т.д. ( см. класс ModelFilter )</para> 
		/// </remarks>
		/// <param name="controller">Текущий контроллер ( на основе которого будут формироваться url-адреса )</param>
		/// <param name="itemsPerPage">Количесто записей на страницу ( по умолчанию 10 )</param>
		/// <param name="urlBasePrefix">Префикс url, по умолчанию пустой (url начинается с контроллера), </param>
		/// <param name="orderDirrection">Сортировка по умолчанию </param>
		public ModelFilter(Controller controller, int itemsPerPage = 100, string urlBasePrefix = "", string orderByColumn = "", bool orderDirrection = true)
		{
			// получение наименования контроллера, при его наличии
			var controllerName = controller.Url.RequestContext.RouteData.Values.ContainsKey(urlController)
				? controller.Url.RequestContext.RouteData.Values[urlController].ToString() : "";
			// получение наименования действия, при его наличии
			var actionName = controller.Url.RequestContext.RouteData.Values.ContainsKey(urlAction)
				? controller.Url.RequestContext.RouteData.Values[urlAction].ToString() : "";

			// заполнение своиств значениями по умолчанию
			OrderByColumn = orderByColumn;
			SearchParam = "";
			SearchText = "";
			UrlBase = "";
			UrlRootBase = urlBasePrefix;
			PagesCount = 0;
			TotalItems = 0;
			Page = 1; // min 1  
			ItemsPerPage = itemsPerPage;
			AscOrder = true;
			Params = new NameValueCollection();
			FilterNames = new List<string>();

			var request = controller.Url.RequestContext.HttpContext.Request.Params;
			foreach (var key in request.AllKeys) {
				if(!key.Contains(UrlParamPrefix))
					continue;
				var name = key.Replace(UrlParamPrefix + ".", "");
				Params[name] = request[key];
			}

			var dic = controller.Url.RequestContext.HttpContext.Request.Params;
			if ((!dic.AllKeys.Any(s=>s==UrlParamPrefix + "." + UrlOrderByColumn)) && orderDirrection == false)
			{
				AscOrder = false;
			}
			for (int i = 0; i < dic.Count; i++)
			{
				if (dic.Keys[i] == UrlParamPrefix + "." + UrlCurrentPage)
				{
					Page = Convert.ToInt32(dic[i]);
				}
				if (dic.Keys[i] == UrlParamPrefix + "." + UrlItemsPerPage)
				{
					ItemsPerPage = Convert.ToInt32(dic[i]);
				}
				if (dic.Keys[i] == UrlParamPrefix + "." + UrlSearchDirection)
				{
					AscOrder = false;
				}
				if (dic.Keys[i] == UrlParamPrefix + "." + UrlOrderByColumn)
				{
					OrderByColumn = dic[i];
				}
				if (dic.Keys[i].Contains(UrlParamPrefix + "." + UrlSimpleSearch))
				{
					SearchParam = dic.Keys[i];
					SearchText = dic[i];
				}
			}

			string url_params = "";
			for (int i = 0; i < controller.Request.QueryString.Count; i++)
			{
				// относим к параметрам по умолчанию все параметры, кроме тех, что добавляет постраничная навигация, сортировка колонок и фильтр
				if (!controller.Request.QueryString.AllKeys[i].Contains(UrlParamPrefix)
					|| controller.Request.QueryString.AllKeys[i] == UrlParamPrefix + "." + UrlItemsPerPage)
				{
					url_params += (url_params == "" ? "?" : "&") + controller.Request.QueryString.AllKeys[i] + "=" + controller.Request.QueryString[i];
				}
				// относим к параметрам для листалки все параметры, кроме тех, что добавляет сама постраничная навигация 
				if (controller.Request.QueryString.AllKeys[i] != UrlParamPrefix + "." + UrlCurrentPage)
				{
					UrlBase += (UrlBase == "" ? "?" : "&") + controller.Request.QueryString.AllKeys[i] + "=" + controller.Request.QueryString[i];
				}
			}
			UrlForColumns = (urlBasePrefix != "" ? "" : "/") + controllerName + "/" + actionName; // получаем основной адрес
			UrlBase = urlBasePrefix + UrlForColumns + UrlBase;									  // добавляем основной адрес к полученным для листалки параметрам
			UrlForColumns += url_params;								      // формируем адрес для колонок таблицы из основного адреса и параметров по умолчанию
		}

		/// <summary>
		/// Формирование адреса на основе адреса для колонок таблицы, поля , по которому будет сортироваться таблица, и направления сортировки
		/// </summary>
		/// <param name="expression">Поле, по которому будет сортироваться таблица</param>
		/// <param name="ascDefault">Направление сортировки</param>
		/// <returns></returns>
		public string OrderBy(Expression<Func<TModel, object>> expression, bool ascDefault = true)
		{
			string name = ""; // Наименование поля, по которому будет сортироваться таблица 

			try
			{
				var body = (MemberExpression)expression.Body;
				// получаем наименование поля, если оно не обернуто в Convert()
				name = body.ToString().Replace(expression.Parameters[0].ToString() + ".", "");
			}
			catch (Exception)
			{
				var body = expression.Body;
				// получаем наименование поля из обертки Convert()
				name = body.ToString().Replace("Convert(" + expression.Parameters[0].ToString() + ".", "").Replace(")", "");
			}

			string ReturnUrl = UrlForColumns;
			if (ReturnUrl.IndexOf("?") == -1)
			{
				ReturnUrl = ReturnUrl + "?";
			}
			else
			{
				ReturnUrl = ReturnUrl + "&";
			}

			ReturnUrl += (name == OrderByColumn ? AscOrder ? UrlParamPrefix + "." + UrlSearchDirection + "=desc&" : ""
				: ascDefault ? "" : UrlParamPrefix + "." + UrlSearchDirection + "=desc&")
				+ UrlParamPrefix + "." + UrlOrderByColumn + "=" + name; // дополняем возвращаемый адрес условием сортировки

			return UrlRootBase+ReturnUrl;
		}
		/// <summary>
		///  Добавление условия фильтра в критерий
		/// </summary>
		/// <param name="criteria">критерий</param>
		/// <param name="key">параметр фильтра из url</param>
		/// <param name="value">значение фильтра</param> 
		/// <param name="usedAliasList">используемы в запросе псевдонимы</param> 
		public void AddSearchToCriteria(ICriteria criteria, string key, string value, Dictionary<string, string> usedAliasList)
		{
			string searchByAlias = "";			// псевдоним, по которому будет проходить фильтрация
			var keyPieces = key.Split('.');		// массив элементов параметра адреса из url 
			if (keyPieces.Length < 5)
			{
				throw new Exception("ModelFilter.AddSearchToCriteria: Недостаточно элементов в параметре фильтрации!");
			}
			string searchType = keyPieces[2];	// тип условия фильтрации like, equal,..

			string searchProperty = "";			// фильтруемого поле 
			string searchPropertyTypeName = ""; // название типа фильтруемого поля
			// последовательно получаем из адреса наименование типа параметра и его полное наименование в модели
			bool startPropType = false;
			bool startPropName = false;
			for (int i = 3; i < keyPieces.Count(); i++)
			{
				if (startPropType && keyPieces[i] != "@name")
				{
					searchPropertyTypeName += searchPropertyTypeName == string.Empty ? keyPieces[i] : "." + keyPieces[i];
				}
				if (startPropName)
				{
					searchProperty += searchProperty == string.Empty ? keyPieces[i] : "." + keyPieces[i];
				}

				if (keyPieces[i] == "@type")
				{
					startPropType = true;
				}
				if (keyPieces[i] == "@name")
				{
					startPropType = false;
					startPropName = true;
				}
			}

			var searchPropertyType = Type.GetType(searchPropertyTypeName);		// получаем тип по найденному наименованию типа

			object searchValue = Convert.ChangeType(value, searchPropertyType); // заварачиваем сконвертированное значение параметра

			if (searchPropertyType == null)
			{
				throw new Exception("ModelFilter.AddSearchToCriteria: Не определен тип параметра фильтрации!");
			}

			if (searchProperty.Contains("."))
			{	// если свойство принадлежит дочерней модели

				var props = searchProperty.Split('.');
				var indexOfLastProp = searchProperty.LastIndexOf("." + props[props.Length - 1]);
				// если значение псевдонима отсуствует в словаре, то добавляем новый псевдоним
				if (!usedAliasList.ContainsKey(searchProperty.Substring(0, indexOfLastProp > 0 ? indexOfLastProp : 0)))
				{
					usedAliasList.Add(searchProperty.Substring(0, indexOfLastProp > 0 ? indexOfLastProp : 0), "_searchFor" + props[props.Length - 1]);
					criteria.CreateAlias(searchProperty.Substring(0, indexOfLastProp > 0 ? indexOfLastProp : 0), "_searchFor" + props[props.Length - 1]);
					// получаем название параметра с псевдонимом  
					searchByAlias = "_searchFor" + props[props.Length - 1] + "." + props[props.Length - 1];
				}
				else
				{
					searchByAlias = usedAliasList[(searchProperty.Substring(0, indexOfLastProp > 0 ? indexOfLastProp : 0))] +
						"." + props[props.Length - 1]; // иначе - название параметра с псевдонимом из словаря
				}
			}
			else
			{
				searchByAlias = searchProperty; // если свойство не принадлежит дочерней модели
			}
			// добавляем условия фильтра в критерий на основе типа условия, псевдонима и значения параметра
			switch (searchType)
			{
				case "like":
					if (searchValue is string)
					{
						criteria.Add(NHibernate.Criterion.Restrictions.Like(searchByAlias, "%" + searchValue + "%"));
					}
					else
					{
						criteria.Add(NHibernate.Criterion.Restrictions.Like(searchByAlias, searchValue));
					}
					break;
				case "eq":
					criteria.Add(NHibernate.Criterion.Restrictions.Eq(searchByAlias, searchValue));
					break;
				default:
					throw new Exception("ModelFilter.AddSearchToCriteria: Тип запроса фильтрации не известен!");
					break;
			}

		}

		/// <summary>
		/// Формирование критерия на основе простого запроса, с учетом условий сортировки, лимитов или фильтра.
		/// </summary>
		/// <param name="expression">Простое лямбда выражение (поле пренадлежит модели)</param>
		/// <returns>Критерий, который можно дополнить или, выполнив запрос, получить список запрашиваемых моделей.</returns>
		public ICriteria GetCriteria(Expression<Func<TModel, bool>> expression = null)
		{
			if (Criteria != null)
				return Criteria;

			string orderByAlias = OrderByColumn;	// получение поля для сортировки у основной модели  

			TotalItems = expression == null ? dbSession.Query<TModel>().Count() : dbSession.Query<TModel>().Where(expression).Count();

			// расчет лимитов
			var skip = ItemsPerPage * (Page - 1);


			var criteria = dbSession.CreateCriteria(typeof(TModel));		   // создаем критерий

			if (expression != null)
			{
				criteria.Add(NHibernate.Criterion.Restrictions.Where(expression)); // лямбда выражение 
			}

			if (SearchParam == "")
			{
				criteria.SetFirstResult(skip)		// сколько записей пропустить
					.SetMaxResults(ItemsPerPage);	// количество выводимых записей  	
			}
			else
			{
				skip = 0;
				Page = 1;
				PagesCount = 0;
				ItemsPerPage = TotalItems;
			}

			// используемые псевдонимы, нужно хранить, т.к. добавления разных псевдонимов на одно и тоже поле недопустимо
			Dictionary<string, string> UsedAliasList = new Dictionary<string, string>();
			if (!string.IsNullOrWhiteSpace(SearchParam) && !string.IsNullOrWhiteSpace(SearchText))
			{
				AddSearchToCriteria(criteria, SearchParam, SearchText, UsedAliasList);		//добавление условия фильтра
			}
													//добавление условия сортировки
			if (OrderByColumn.Contains("."))		// если поле принадлежит дочерней модели 
			{
				var props = OrderByColumn.Split('.');
				var indexOfLastProp = OrderByColumn.LastIndexOf("." + props[props.Length - 1]);
				if (!UsedAliasList.ContainsKey(OrderByColumn.Substring(0, indexOfLastProp > 0 ? indexOfLastProp : 0)))
				{
					UsedAliasList.Add(OrderByColumn.Substring(0, indexOfLastProp > 0 ? indexOfLastProp : 0), "_order");
					criteria.CreateAlias(OrderByColumn.Substring(0, indexOfLastProp > 0 ? indexOfLastProp : 0), "_order"); // добавляем псевдоним 
					orderByAlias = "_order." + props[props.Length - 1];	 // получаем название параметра с псевдонимом
				}
				else
				{
					orderByAlias = UsedAliasList[OrderByColumn.Substring(0, indexOfLastProp > 0 ? indexOfLastProp : 0)] +
						"." + props[props.Length - 1]; ; // получаем название параметра с псевдонимом
				}
			}

			if (OrderByColumn != "")
			{			   // если свойство принадлежит дочерней модели 
				if (AscOrder)
				{
					criteria.AddOrder(NHibernate.Criterion.Order.Asc(orderByAlias));  // Сортировка по полю связи 
				}
				else
				{
					criteria.AddOrder(NHibernate.Criterion.Order.Desc(orderByAlias)); // Сортировка по полю связи  
				}
			}

			Criteria = criteria;
			return criteria; // возврат критерия;
		}

		/// <summary>
		/// Получение параметра объекта
		/// </summary>
		/// <param name="name">Имя параметра</param>
		/// <returns></returns>
		public string GetParam(string name)
		{
			return this.Params[name];
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
			var attrs = new Dictionary<string,string>();
			if(Params[name]!= null)
				attrs["value"] = Params[name];

			attrs["name"] = UrlParamPrefix + "." + name;
			attrs["class"] = "form-control";
			FilterNames.Add(name);
			var html = GenerateHtml(type, attrs, additional);
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

			foreach (var key in keys)
				if(!FilterNames.Contains(key))
					builder.Append(string.Format("<input style='display: none' name='{0}.{1}' value='{2}' />", UrlParamPrefix, key, Params[key]));

			var html = new HtmlString(builder.ToString());
			return html;
		}

		/// <summary>
		/// Генерирует HTML код.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="o">Словарь с html аттрибутами</param>
		/// <param name="additional">Дополнительные параметры</param>
		/// <returns></returns>
		private string GenerateHtml(HtmlType type, Dictionary<string, string> o, object additional = null)
		{
			if (type == HtmlType.Date) {
				o["class"] = " datepicker " + o["class"];
				o["date-format"] = "dd.nn.yyyy";
				o[" data-provide"] = "datepicker-inline";
				return string.Format("<input {0} />", GetPropsValues(o));
			}
				

			if(type == HtmlType.Dropdown) {
				var list = additional as NameValueCollection;
				var values = list.AllKeys.Select(i => string.Format("<option {2} value='{0}'>{1}</option>",i,list[i],o.ContainsKey("value") && i == o["value"] ? "selected='selected'" : "")).ToList();
				return string.Format("<select {0}>{1}</select>",GetPropsValues(o),string.Join("\n",values));
			}
			return "";
		}

		/// <summary>
		/// Конвертация словаря, в строку c html аттрибутами
		/// </summary>
		/// <param name="obj">Словарь, создержащий названия и значения аттрибутов</param>
		/// <returns>Строка вида "name1='value1' name2='value2'"</returns>
		private string GetPropsValues(Dictionary<string, string> obj)
		{
			var sb = new StringBuilder();
			foreach (var key in obj.Keys)
			{
				sb.Append(string.Format(" {0}='{1}'", key, obj[key]));
			}
			return sb.ToString();
		}

		/// <summary>
		/// Получение списка моделей.
		/// </summary>
		/// <returns>Список моделей</returns>
		public IList<TModel> GetItems()
		{
			if(Models == null)
				Execute();
			return Models;
		}

		/// <summary>
		/// Выполнение SQL кода и заполнение списка моделей
		/// </summary>
		/// <returns></returns>
		public void Execute()
		{
			Models = Criteria.List<TModel>();
		}

		/// <summary>
		/// Получение SQL кода запроса на поиск моделей
		/// </summary>
		/// <returns></returns>
		public string GetSql()
		{
			CriteriaImpl c = (CriteriaImpl)Criteria;
			SessionImpl s = (SessionImpl)c.Session;
			ISessionFactoryImplementor factory = s.Factory;
			String[] implementors = factory.GetImplementors(c.EntityOrClassName);
			CriteriaLoader loader = new CriteriaLoader((IOuterJoinLoadable)factory.GetEntityPersister(implementors[0]),
				factory, c, implementors[0], s.EnabledFilters);
			var str = loader.ToString();
			return str;
		}
	}
}