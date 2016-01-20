using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using Inforoom2.Controllers;
using Inforoom2.Helpers;
using Inforoom2.Models;
using log4net.Appender;
using NHibernate;
using NHibernate.Context;
using NHibernate.Mapping;
using NHibernate.Util;

namespace Inforoom2.Components
{
	/// <summary>
	/// Аттрибут для натягивания данных форм на модели.
	/// </summary>
	public class EntityBinderAttribute : CustomModelBinderAttribute
	{
		//список разрешенных полей
		private string paramsInclude { get; set; }
		//список запрещенных полей
		private string paramsExclude { get; set; }

		/// <summary>
		/// Атрибут байндера
		/// </summary>
		/// <param name="include">список разрешенных полей</param>
		/// <param name="exclude">список запрещенных полей</param>
		public EntityBinderAttribute(string include = "", string exclude = "")
		{
			paramsInclude = include;
			paramsExclude = exclude;
		}

		public override IModelBinder GetBinder()
		{
			var Include = paramsInclude.ToLower().Split(',').Where(s => s.Replace(" ", "").Length != 0).ToArray();
			var Exclude = paramsInclude.ToLower().Split(',').Where(s => s.Replace(" ", "").Length != 0).ToArray();
			return new EntityBinder(Include, Exclude);
		}
	}

	//TODO:подумать над обработкой ошибок, вызванных некорректным HTML

	//TODO:подумать, как избежать инъекции лишних значений в Байндер
	/// <summary>
	/// Объект, который натягивает данные из форм на объекты
	/// </summary>
	public class EntityBinder : IModelBinder
	{
		//флаг отменяющий проверку "разрешенных полей"
		public static bool EnableBinderProtection = false;
		//название тэга, добавляемого байндером в html-документ
		public const string BinderPropertyHtmlName = "ListOfPermitted";
		//список разрешенных полей
		private string[] paramsInclude { get; set; }
		//список запрещенных полей
		private string[] paramsExclude { get; set; }
		//результирующий список разрешенных полей
		private List<string> propertiesToBind { get; set; }
		//сессия хибера
		private ISession dbSession { get; set; }
		//база шифрования
		private const string passwordBase = "ea8hpymwixz43gk96wqh";
		//флаг отменяющий генерацию шифруемого списка полей
		private bool enableAutoProtection { get; set; }

		public EntityBinder(string[] include, string[] exclude, bool enableAutoProtection = true)
		{
			paramsInclude = include;
			paramsExclude = exclude;
			this.enableAutoProtection = enableAutoProtection;
			propertiesToBind = new List<string>();
			propertiesToBind.AddRange(paramsInclude);
		}

		/// <summary>
		/// Интерфейс MVC, для кастомного байндинга атрибутов.
		/// </summary>
		/// <param name="controllerContext">Контекст контроллера</param>
		/// <param name="bindingContext">Контекст байндинга</param>
		/// <returns></returns>
		public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
		{
#if DEBUG
			if (controllerContext.HttpContext != null
			    && controllerContext.HttpContext.Request.Form != null) {
				if (controllerContext.HttpContext.Request.Form["binderTestOff"] != null) {
					EntityBinder.EnableBinderProtection = false;
				}
				if (controllerContext.HttpContext.Request.Form["binderTestOn"] != null) {
					EntityBinder.EnableBinderProtection = true;
				}
			}
#else
#endif

			var modelName = bindingContext.ModelName;
			var type = bindingContext.ModelType;
			var baseController = controllerContext.Controller as BaseController;
			if (baseController == null) {
				throw new Exception("В EntityBinder попал контроллер, не наследуемый от BaseController!");
			}
			if (baseController.HttpContext.Request.HttpMethod != "POST") {
				return null;
			}
			dbSession = baseController.DbSession;
			//Так как мы удаляем использованные элементы из коллекции, во время байндинга
			//Форму необходимо скопировать
			var collection = new NameValueCollection(controllerContext.HttpContext.Request.Form);


			if (EnableBinderProtection) {
				//Получаем список допустимых полей (по добавленному байндером тэгу)
				if (enableAutoProtection) GetBinderProps(controllerContext, collection);
				//Отсеиваем исключенные из списка поля 
				propertiesToBind = propertiesToBind.Where(s => !paramsExclude.Any(d => d == s)).ToList();
				if (propertiesToBind.Count == 0) {
					collection.Clear();
				}
				else {
					var listWithForbiddenFields = new List<string>();
					for (int i = 0; i < propertiesToBind.Count; i++) {
						for (int j = 0; j < collection.Count; j++) {
							if (!propertiesToBind.Any(s => s == collection.GetKey(j).ToLower())) {
								var currentKey = collection.GetKey(j);
								if (currentKey != BinderPropertyHtmlName) {
									listWithForbiddenFields.Add(currentKey);
								}
								collection.Remove(currentKey);
								break;
							}
						}
					}

					if (listWithForbiddenFields.Count > 0) {
						var clientId = "";
						if (controllerContext.Controller as Inforoom2Controller != null) {
							var client = ((Inforoom2Controller) controllerContext.Controller).GetCurrentClient();
							clientId = client != null ? client.Id + " " : "";
						}
						var pathUrl = controllerContext.RouteData.Values.ContainsKey("controller")
							? controllerContext.RouteData.Values["controller"].ToString() + "/"
							: "";
						pathUrl += controllerContext.RouteData.Values.ContainsKey("action")
							? controllerContext.RouteData.Values["action"].ToString()
							: "";

						var userIp = controllerContext.Controller as Controller != null
							? ((Controller) controllerContext.Controller).Request.UserHostAddress
							: "";
						var errorMessage =
							string.Format(
								"Пользователь {0}с ip-адресом {1} попытался передать методу '{2}' модель '{3}' с запрещенными полями: '{4}'. Данные поля обработаны не были.",
								clientId, userIp, pathUrl, bindingContext.ModelName, string.Join(",", listWithForbiddenFields)
								);
						EmailSender.SendError(errorMessage);
					}
				}
			}
			else {
				for (int i = 0; i < paramsExclude.Length; i++) {
					for (int j = 0; j < collection.Count; j++) {
						if (paramsExclude.Any(s => s == collection.GetKey(j).ToLower())) {
							var currentKey = collection.GetKey(j);
							collection.Remove(currentKey);
							break;
						}
					}
				}
				for (int i = 0; i < paramsInclude.Length; i++)
				{
					for (int j = 0; j < collection.Count; j++)
					{
						if (!paramsInclude.Any(s => s == collection.GetKey(j).ToLower()))
						{
							var currentKey = collection.GetKey(j);
							collection.Remove(currentKey);
							break;
						}
					}
				}
			}
			//получаем из списка поля модели
			var values = SliceValues(collection, modelName);
			var res = MapModel(values, type);
			return res;
		}

		/// <summary>
		/// Натягивает форму из запроса на модель. Имя модели, отгадывается байндером по именованию.
		/// </summary>
		/// <param name="request">Объект HTTP запроса</param>
		/// <param name="type">Тип модели </param>
		/// <returns></returns>
		public object MapModel(HttpRequestBase request, Type type)
		{
			var form = request.Form;
			var keys = form.AllKeys;
			string name = null;
			foreach (var key in keys) {
				var split = key.Split('.');
				if (split.Length > 1) {
					name = split.First();
					break;
				}
			}
			if (name == null)
				throw new Exception("Не удалось выяснить имя модели");

			var values = SliceValues(new NameValueCollection(request.Form), name);
			var ret = MapModel(values, type);
			return ret;
		}

		/// <summary>
		/// Обработка html документа 
		/// </summary>
		/// <param name="controller">Контроллер</param>
		/// <param name="html">Документ</param>
		/// <returns></returns>
		public static string HtmlProcessing(Controller controller, string html)
		{
			//получение списка редактируемых в документе полей 
			var nameList = EntityBinder.GetHtmlPropertyValuesByName(html, new string[] {"input", "select"}, "name");
			if (nameList == null || nameList.Count == 0) return html;
			//шифрование списка полей
			var cryptedValue = Cryptographer.EncryptString(String.Join(",", nameList), passwordBase);
			var cryptedValueBase = cryptedValue.Substring(cryptedValue.Length - 7);
			cryptedValue = Cryptographer.EncryptString(cryptedValue, cryptedValueBase);
			cryptedValue = controller.HttpContext.Server.HtmlEncode(cryptedValue + cryptedValueBase);
			//добавление в документ тэга со значением зашифрованного списка полей
			var binderInput = new TagBuilder("input");
			binderInput.MergeAttribute("name", EntityBinder.BinderPropertyHtmlName);
			binderInput.MergeAttribute("value", cryptedValue);
			binderInput.MergeAttribute("type", "hidden");
			html = binderInput.ToString(TagRenderMode.SelfClosing);

			var binderScript = new TagBuilder("script");
			binderScript.MergeAttribute("type", "text/javascript");
			binderScript.InnerHtml = "$(function(){$('form[method=\"post\"]').append($('input[name=\"" +
			                         EntityBinder.BinderPropertyHtmlName +
			                         "\"]').clone());})";
			html += binderScript.ToString(TagRenderMode.Normal);
			return html;
		}


		/// <summary>
		/// Получение полей, доступных для ввода с формы
		/// </summary>
		/// <param name="html">Документ</param>
		/// <param name="tags">Тэги, содержащие значения</param>
		/// <param name="propertyName">Свойство, по которому получаем список полей</param>
		/// <returns></returns>
		public static List<string> GetHtmlPropertyValuesByName(string html, string[] tags, string propertyName)
		{
			if (html == String.Empty || html.IndexOf("<") == -1 || html.IndexOf(">") == -1) return null;
			var nameList = new List<string>();
			string formatSub1 = propertyName + "=\"";
			string formatSub2 = propertyName + "='";

			while (html.IndexOf("<") != -1 || html.IndexOf(">") != -1) {
				var firstIndex = html.IndexOf("<");
				var lastIndex = html.IndexOf(">") + 1;
				var subStr = html.Substring(firstIndex, lastIndex - firstIndex);
				if (tags.Any(s => subStr.IndexOf(s) != -1)) {
					subStr = subStr.Replace(" ", "").ToLower();
					if (subStr.IndexOf(propertyName) != -1) {
						if (subStr.IndexOf(formatSub1) != -1) {
							subStr = subStr.Substring(subStr.IndexOf(formatSub1) + formatSub1.Length);
							subStr = subStr.Substring(0, subStr.IndexOf("\""));
						}
						else {
							if (subStr.IndexOf(formatSub2) != -1) {
								subStr = subStr.Substring(subStr.IndexOf(formatSub2) + formatSub2.Length);
								subStr = subStr.Substring(0, subStr.IndexOf("\'"));
							}
						}
						nameList.Add(subStr);
					}
				}
				html = html.Substring(html.IndexOf(">") + 1);
			}
			return nameList;
		}

		/// <summary>
		///  Генерирование переменной на основе полученных типа и значения
		/// </summary>
		/// <param name="values">Коллекция свойств для модели</param>
		/// <param name="entityType">Тип модели</param> 
		/// <returns>модель</returns>
		protected object GenerateInstance(NameValueCollection values, Type entityType)
		{
			// данное значение возможно при использовании
			// Inforoom2.Helpers.ViewHelper.DropDownListExtendedFor
			// в пустом значении списка
			if (values["id"] == "") return null;

			if (values["id"] == null || int.Parse(values["id"]) == 0) {
				// создание пустой переменной указанного типа
				return Activator.CreateInstance(entityType);
			}
			else {
				// создание переменной указанного типа с переданным значением
				return dbSession.Get(entityType, int.Parse(values["id"]));
			}
		}


		/// <summary>
		/// Рекурсивно Натягивает данные из списка на модель. Каждый элемент в списке считается частью модели.
		/// Если в списке будут поля, которых нет в модели или во вложенных в нее объектах, то произойдет ошибка.
		/// </summary>
		/// <param name="values">Коллекция свойств для модели</param>
		/// <param name="entityType">Тип модели</param>
		/// <returns></returns>
		public object MapModel(NameValueCollection values, Type entityType)
		{
			var instance = GenerateInstance(values, entityType);

			if (instance == null)
				return null;

			while (values.HasKeys()) {
				var key = values.Keys.First() as string;
				object newValue;
				//Получаем имя поля из параметров
				var propName = GetPropertyNameFromKey(key);
				//Получаем поле
				var property = instance.GetType().GetProperty(propName);

				if (property.PropertyType.IsSubclassOf(typeof (BaseModel))) {
					//Если это модель то мапим модель
					var oldValue = property.GetValue(instance, new object[] {}) as BaseModel;
					var idKey = property.Name + ".Id";
					//Если нет идентификатора вложенной модели, то мы его создаем в параметрах
					//То есть если не было дано никаких специальных указаний, что модель надо затереть или подгрузить другую
					if (oldValue != null && values[idKey] == null)
						values[idKey] = oldValue.Id.ToString();
					var subvalues = SliceValues(values, property.Name);
					newValue = MapModel(subvalues, property.PropertyType);
					property.SetValue(instance, newValue, new object[] {});
					if (values[key] != null)
						throw new Exception(
							"Не удается назначить поле! \n Вероятная причина: значени в свойстве name тэга обрабатываемого представления *view* указывает на объект ( на модель '" +
							key + "' ), а не на его поле!");
				}
				else if (property.PropertyType.IsGenericType && property.PropertyType.GetInterfaces().Contains(typeof (IEnumerable))) {
					//Если поле это коллекция, то получаем набор параметров для каждого элемента и закидываем их в список
					//Получаем тип элемента коллекции
					var subType = property.PropertyType.GetGenericArguments().FirstOrDefault();
					//Получаем поле объекта
					var collection = (IList) property.GetValue(instance, new object[] {});
					//Получаем словарь с коллекциями значений для элементов списка
					var subCollections = SliceMultipleValues(values, property.Name);

					//Есть 3 варианта использования байндера: изменение коллекции, удаление элементов или добавления элемента в конец
					if (subCollections.ContainsKey("add"))
						AppendToCollection(collection, subType, subCollections["add"]);
					else if (subCollections.Keys.Any(i => int.Parse(i) < 0))
						RemoveFromCollection(collection, subCollections);
					else
						ChangeCollection(collection, subType, subCollections);
				}
				else {
					//Если это простое поле, то просто присваиваем его специальной функцией
					//Которая хорошо конвертирует строки в значения полей модели
					newValue = values[key];
					values.Remove(key);
					SetValue(instance, property, newValue);
				}
			}
			return instance;
		}

		private void RemoveFromCollection(IList collection, Dictionary<string, NameValueCollection> subCollections)
		{
			foreach (var pair in subCollections) {
				//Получаем коллекцию из коллекции коллекций
				var subcollection = pair.Value;
				//Находим id, модели, которую нужно удалить из коллекции
				//а затем саму модель
				var id = int.Parse(subcollection["Id"]);
				foreach (var model in collection) {
					if ((model as BaseModel).Id == id) {
						collection.Remove(model);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Изменяет списочное поле-коллекцию у модели, заполняет ее новыми значениями.
		/// Остаются только те значения, которые были переданы.
		/// </summary>
		/// <param name="collection">Исходная коллекция</param>
		/// <param name="type">Тип элементов коллекции</param>
		/// <param name="subCollections">Наборы значений для элементов коллекций</param>
		private void ChangeCollection(IList collection, Type type, Dictionary<string, NameValueCollection> subCollections)
		{
			//Очищаем
			collection.Clear();
			foreach (var pair in subCollections) {
				//Получаем коллекцию из коллекции коллекций
				var subcollection = pair.Value;
				//Натягиваем параметры
				var newValue = MapModel(subcollection, type);
				collection.Add(newValue);
			}
		}

		/// <summary>
		/// Добавление элемента в коллекцию
		/// </summary>
		/// <param name="collection">Исходная коллекция</param>
		/// <param name="type">Тип элементов коллекции</param>
		/// <param name="values">Набор значений для нового элемента коллекции</param>
		private void AppendToCollection(IList collection, Type type, NameValueCollection values)
		{
			var newValue = MapModel(values, type);
			collection.Add(newValue);
		}

		/// <summary>
		/// Получает название поля модели из ключа формы.
		/// Очищает имя ключа от вложенных значений и другой разметки.
		/// </summary>
		/// <param name="str">Ключ формы, который хранится в аттрибутте input.name</param>
		private string GetPropertyNameFromKey(string str)
		{
			var key = str.Split('.').First();
			if (key.IndexOf('[') != -1)
				key = key.Substring(0, key.IndexOf('['));
			return key;
		}

		/// <summary>
		/// Получает параметры поля модели из списка. Предполагается что поле модели - это список вложенных моделей.
		/// Параметры, относящиеся к полю, удаляются из изначального списка. 
		/// </summary>
		/// <param name="values">Изначальный список</param>
		/// <param name="name">Имя поля модели</param>
		/// <returns>
		/// Возвращает словарь, элементами, которого являются списки параметров для вложенных моделей.
		/// Индексом, является порядковый номер модели в списке.
		/// 
		/// Элемент у которого в изначальном списке не указан индекс (то есть мы хотели его добавить в конец) получает индекс "add".
		/// </returns>
		private Dictionary<string, NameValueCollection> SliceMultipleValues(NameValueCollection values, string name)
		{
			var dictionary = new Dictionary<string, NameValueCollection>();
			while (values.AllKeys.Any(i => i.Contains(name))) {
				var key = values.AllKeys.First(i => i.Contains(name));
				//получаем значения индекса элемента из верстки
				var index = key.Split('[')[1].Split(']')[0];
				int indexVal;
				var isRealIndex = int.TryParse(index, out indexVal);
				var subname = isRealIndex ? name + "[" + index + "]" : name + "[]";
				var subcollection = SliceValues(values, subname);
				var dictionaryIndex = isRealIndex ? indexVal.ToString() : "add";
				dictionary.Add(dictionaryIndex, subcollection);
			}
			return dictionary;
		}

		/// <summary>
		/// Получает параметры поля модели из списка. Предполагается что поле модели - это вложенная модель.
		/// Параметры, относящиеся к полю, удаляются из изначального списка. 
		/// </summary>
		/// <param name="values">Изначальный список</param>
		/// <param name="name">Имя поля модели</param>
		private NameValueCollection SliceValues(NameValueCollection values, string name)
		{
			name = name.ToLower();
			var collection = new NameValueCollection();
			foreach (var key in values.AllKeys) {
				//Проверяем, является ли ключ объектом с полями
				if (key.ToLower().IndexOf(name + ".") == 0) {
					collection.Add(key.Substring(name.Length + 1), values[key]);
					values.Remove(key);
				}
			}

			return collection;
		}

		/// <summary>
		/// Конверирует значение в тип поля модели и присваивает его.
		/// Используется для простых типов данных и стандартных структур.
		/// </summary>
		/// <param name="inputObject">Объект модели</param>
		/// <param name="propertyInfo">Свойство, которое необходимо назначить</param>
		/// <param name="propertyVal">Значение свойства (как правило строка)</param>
		public void SetValue(object inputObject, PropertyInfo propertyInfo, object propertyVal)
		{
			//Convert.ChangeType does not handle conversion to nullable types
			//if the property type is nullable, we need to get the underlying type of the property
			var targetType = IsNullableType(propertyInfo.PropertyType)
				? Nullable.GetUnderlyingType(propertyInfo.PropertyType)
				: propertyInfo.PropertyType;

			//Для булевых типов, строчное "True" конвертируется в True
			if (targetType == typeof (Boolean))
				propertyVal = propertyVal.ToString().ToLower().Contains("true");
			else if (targetType == typeof (DateTime)) {
				//Пустые даты приводятся в минимальному значению
				DateTime date;
				if (!DateTime.TryParse(propertyVal.ToString(), out date))
					date = DateTime.MinValue;
				propertyVal = date;
			}
			else if (targetType.BaseType == typeof (Enum)) {
				//Значения перечислений задаются через их строчное обозначение
				propertyVal = Enum.Parse(targetType, propertyVal.ToString());
			}

			if (IsNullableType(propertyInfo.PropertyType) && targetType != typeof (String) && propertyVal == "") {
				propertyVal = null;
			}

			try {
				if (propertyVal != null)
					propertyVal = Convert.ChangeType(propertyVal, targetType);
				propertyInfo.SetValue(inputObject, propertyVal, null);
			}
			catch (Exception e) {
			}
		}

		/// <summary>
		/// Проверка, является ли тип Nullable типом
		/// </summary>
		/// <param name="type">Тип</param>
		/// <returns>True, если это Nullable тип</returns>
		private static bool IsNullableType(Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
		}

		/// <summary>
		/// Получение списка полей по значению тэга с формы
		/// </summary>
		/// <param name="controllerContext"></param>
		/// <param name="propCollection"></param>
		private void GetBinderProps(ControllerContext controllerContext, NameValueCollection propCollection)
		{
			//Расшифровка списка полей
			var PropsPermittedToBeBoundEnc = propCollection.AllKeys.FirstOrDefault(s => s == BinderPropertyHtmlName) != null
				? propCollection[BinderPropertyHtmlName]
				: "";
			if (!string.IsNullOrEmpty(PropsPermittedToBeBoundEnc)) {
				PropsPermittedToBeBoundEnc = controllerContext.HttpContext.Server.HtmlDecode(PropsPermittedToBeBoundEnc);
				var cryptedValueBase = PropsPermittedToBeBoundEnc.Substring(PropsPermittedToBeBoundEnc.Length - 7);
				PropsPermittedToBeBoundEnc = PropsPermittedToBeBoundEnc.Substring(0, PropsPermittedToBeBoundEnc.Length - 7);
				PropsPermittedToBeBoundEnc = Cryptographer.DecryptString(PropsPermittedToBeBoundEnc, cryptedValueBase);
				var PropsPermittedToBeBoundDec = Cryptographer.DecryptString(PropsPermittedToBeBoundEnc, passwordBase);
				//Получение массива полей
				var permittedPropArray =
					PropsPermittedToBeBoundDec.ToLower().Split(',').Where(s => s.Replace(" ", "").Length != 0).ToArray();
				//Удаление из массива ненужных полей
				permittedPropArray.ForEach(s =>
				{
					if (!propertiesToBind.Any(f => f == s)) {
						propertiesToBind.Add(s);
					}
				});
			}
			else {
				if (EnableBinderProtection) {
					var pathUrl = controllerContext.RouteData.Values.ContainsKey("controller")
						? controllerContext.RouteData.Values["controller"].ToString() + "/"
						: "";
					pathUrl += controllerContext.RouteData.Values.ContainsKey("action")
						? controllerContext.RouteData.Values["action"].ToString()
						: "";
					var userIp = controllerContext.Controller as Controller != null
						? ((Controller) controllerContext.Controller).Request.UserHostAddress
						: "";
					throw new Exception(
						$"У клиента с ip-адресом {userIp} по запросу '{pathUrl}' произошла ошибка в работе Binder(a): отсутствует тэг '{BinderPropertyHtmlName}'.");
				}
			}
		}
	}
}