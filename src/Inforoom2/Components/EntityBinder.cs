using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
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
		public override IModelBinder GetBinder()
		{
			return new EntityBinder();
		}
	}

	//TODO:подумать над обработкой ошибок, вызванных некорректным HTML

	//TODO:подумать, как избежать инъекции лишних значений в Байндер
	/// <summary>
	/// Объект, который натягивает данные из форм на объекты
	/// </summary>
	public class EntityBinder : IModelBinder
	{
		protected static ISession Session;

		/// <summary>
		/// Устанавливает сессию. 
		/// Так как использование двух сессий является опасным, следует вызывать эту функцию там где создается сессия для контроллеров.
		/// </summary>
		/// <param name="dbsession">Сессия Nhibernate</param>
		public static void SetSession(ISession dbsession)
		{
			Session = dbsession;
		}

		/// <summary>
		/// Интерфейс MVC, для кастомного байндинга атрибутов.
		/// </summary>
		/// <param name="controllerContext">Контекст контроллера</param>
		/// <param name="bindingContext">Контекст байндинга</param>
		/// <returns></returns>
		public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
		{
			var modelName = bindingContext.ModelName;
			var type = bindingContext.ModelType;

			//Так как мы удаляем использованные элементы из коллекции, во время байндинга
			//Форму необходимо скопировать
			var collection = new NameValueCollection(controllerContext.HttpContext.Request.Form);
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
			foreach (var key in keys)
			{
				var split = key.Split('.');
				if (split.Length > 1)
				{
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
				return Session.Get(entityType, int.Parse(values["id"]));
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

			if (instance==null)
				return null;

			while (values.HasKeys())
			{
				var key = values.Keys.First() as string;
				object newValue;
				//Получаем имя поля из параметров
				var propName = GetPropertyNameFromKey(key);
				//Получаем поле
				var property = instance.GetType().GetProperty(propName);

				if (property.PropertyType.IsSubclassOf(typeof(BaseModel))) {
					//Если это модель то мапим модель
					var subvalues = SliceValues(values, property.Name);
					newValue = MapModel(subvalues, property.PropertyType);
					property.SetValue(instance, newValue, new object[] { });
					if (values[key] != null) throw new Exception("Не удается назначить поле! \n Вероятная причина: значени в свойстве name тэга обрабатываемого представления *view* указывает на объект ( на модель '" + key + "' ), а не на его поле!");
				}
				else if (property.PropertyType.IsGenericType && property.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)))
				{
					//Если поле это коллекция, то получаем набор параметров для каждого элемента и закидываем их в список
					//Получаем тип элемента коллекции
					var subType = property.PropertyType.GetGenericArguments().FirstOrDefault();
					//Получаем поле объекта
					var collection = (IList)property.GetValue(instance, new object[] { });
					//Получаем словарь с коллекциями значений для элементов списка
					var subCollections = SliceMultipleValues(values, property.Name);

					//Есть 2 варианта использования байндера: изменение коллекции или добавления элемента
					if (subCollections.ContainsKey("-1"))
						AppendToCollection(collection, subType, subCollections["-1"]);
					else
						ChangeCollection(collection, subType, subCollections); 
				}
				else
				{
					//Если это простое поле, то просто присваиваем его специальной функцией
					//Которая хорошо конвертирует строки в значения полей модели
					newValue = values[key];
					values.Remove(key);
					SetValue(instance, property, newValue); 
				}
			
			}
			return instance;
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
			foreach (var pair in subCollections)
			{
				//Получаем индекс элемента
				var index = pair.Key;
				//Получаем коллекцию коллекций
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
		/// Элемент у которого в изначальном списке не указан индекс (то есть мы хотели его добавить в конец) получает индекс -1.
		/// </returns>
		private Dictionary<string, NameValueCollection> SliceMultipleValues(NameValueCollection values, string name)
		{
			var dictionary = new Dictionary<string, NameValueCollection>();
			while (values.AllKeys.Any(i => i.Contains(name)))
			{
				var key = values.AllKeys.First(i => i.Contains(name));
				var index = key[name.Length + 1];
				int indexVal;
				var isRealIndex = int.TryParse(index.ToString(), out indexVal);
				var subname = isRealIndex ? name + "[" + index + "]" : name + "[]";
				var subcollection = SliceValues(values, subname);
				indexVal = isRealIndex ? indexVal : -1;
				dictionary.Add(indexVal.ToString(), subcollection);
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
			if (targetType == typeof(Boolean))
				propertyVal = propertyVal.ToString().ToLower().Contains("true");
			else if (targetType == typeof(DateTime))
			{
				//Пустые даты приводятся в минимальному значению
				DateTime date;
				if (!DateTime.TryParse(propertyVal.ToString(), out date))
					date = DateTime.MinValue;
				propertyVal = date;
			}
			else if (targetType.BaseType == typeof(Enum))
			{
				//Значения перечислений задаются через их строчное обозначение
				propertyVal = Enum.Parse(targetType, propertyVal.ToString());
			}
			try
			{
				//
				propertyVal = Convert.ChangeType(propertyVal, targetType);
				propertyInfo.SetValue(inputObject, propertyVal, null);
			}
			catch (Exception e)
			{

			}

		}

		/// <summary>
		/// Проверка, является ли тип Nullable типом
		/// </summary>
		/// <param name="type">Тип</param>
		/// <returns>True, если это Nullable тип</returns>
		private static bool IsNullableType(Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}
	}
}