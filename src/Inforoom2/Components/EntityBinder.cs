using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Context;

namespace Inforoom2.Components
{
	/// <summary>
	/// Converts entity id's to entities
	/// </summary>
	public class EntityBinderAttribute : CustomModelBinderAttribute, IModelBinder
	{
		private static ISession session;
		private readonly string _idName;
		private readonly Type _entityType;
		private readonly bool _relaxed;

		public static void SetSession(ISession dbsession)
		{
			session = dbsession;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="idName">Field name</param>
		/// <param name="entityType">Entity type</param>
		/// <param name="relaxed">If true and the entity is not found, should return null</param>
		public EntityBinderAttribute(string idName, Type entityType, bool relaxed = false)
		{
			_idName = idName;
			_entityType = entityType;
			_relaxed = relaxed;
		}

		public EntityBinderAttribute(Type entityType, bool relaxed = false)
			: this("Id", entityType, relaxed)
		{
		}


		public EntityBinderAttribute(string idName, bool relaxed = false)
			: this(idName, null, relaxed)
		{
		}

		public EntityBinderAttribute(bool relaxed = false)
			: this("Id", relaxed)
		{
		}

		/// <summary>
		/// Retrieves the associated model binder.
		/// </summary>
		/// <returns>
		/// A reference to an object that implements the <see cref="T:System.Web.Mvc.IModelBinder"/> interface.
		/// </returns>
		public override IModelBinder GetBinder()
		{
			return this;
		}

		/// <summary>
		/// Натягивает форму из запроса на модель
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public object MapModel(HttpRequestBase request)
		{
			var entityType = _entityType;
			object instance = Activator.CreateInstance(entityType);
			var props = instance.GetType().GetProperties();
			if (props.Count() != 0) {
				foreach (var propertyInfo in props) {
					var propertyName = propertyInfo.Name;
					var propertyValue = request.Form.Get(entityType.Name.ToLower() + "." + propertyName);
					if (!string.IsNullOrEmpty(propertyValue)) {
						SetValueToModelProperty(instance, propertyName, propertyValue, propertyInfo, null);
					}
				}
			}
			return instance;
		}

		/// <summary>
		/// Binds the model to a value by using the specified controller context and binding context.
		/// </summary>
		/// <returns>
		/// The bound value.
		/// </returns>
		/// <param name="controllerContext">The controller context.</param><param name="bindingContext">The binding context.</param>
		/// <exception cref="HttpException"><c>HttpException</c>.</exception>
		public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
		{
			HttpRequestBase request = controllerContext.HttpContext.Request;
			// поиск Id модели на форме
			var fieldName = bindingContext.ModelName + ".Id";
			var result = bindingContext.ValueProvider.GetValue(fieldName);
			if (FieldNotFoundOrValueIsEmpty(result))
				fieldName = _idName;
			result = bindingContext.ValueProvider.GetValue(fieldName);
			if (FieldNotFoundOrValueIsEmpty(result)) {
				/*if (_relaxed)*/
				//throw new MissingFieldException("Could not find the request parameter: " + fieldName);
			}

			var entityType = _entityType ?? bindingContext.ModelType;
			if (entityType.IsArray) {
				var realType = entityType.GetElementType();
				var instances = from idString in (string[])result.RawValue
					select session.Get(realType, GetId(idString, fieldName));
				return instances.ToArray();
			}
			else {
				var id = 0;
				if (result != null)
					// если Id модели найден ищем данные модели в БД
					// если не найден - ошибка
					// если он равен 0, создаем новую модель
					id = GetId(result, fieldName);
				var instance = id == 0 ? Activator.CreateInstance(entityType) : session.Get(entityType, id);

				// получаем все свойства модели
				var props = instance.GetType().GetProperties();
				if (props.Count() != 0) {
					// Молучаем вложенные модели и списки *для родительской модели - МОДЕЛЬ ОДНА*
					GetModelProperties(instance, request.Form, entityType.Name);
					// пробегаемся по всем свойствам модели получая их значения с формы
					foreach (var propertyInfo in props) {
						var propertyName = propertyInfo.Name;
						var propertyValue = request.Form.Get(entityType.Name.ToLower() + "." + propertyName);
						var propertyValue2 = request.Form.Get(entityType.Name.ToLower() + "proxy." + propertyName);
						if (propertyValue != null) {
							SetValueToModelProperty(instance, propertyName, propertyValue, propertyInfo, session);
						}
						else if (propertyValue2 != null) {
							SetValueToModelProperty(instance, propertyName, propertyValue2, propertyInfo, session);
						}
						var objectId = request.Form.Get(entityType.Name.ToLower() + "." + propertyName + ".Id");
						var objectId2 = request.Form.Get(entityType.Name.ToLower() + "proxy." + propertyName + ".Id");
						if (!string.IsNullOrEmpty(objectId)) {
							SetValueToModelProperty(instance, propertyName, objectId, propertyInfo, session);
						}
						else if (objectId2 != null) {
							SetValueToModelProperty(instance, propertyName, objectId2, propertyInfo, session);
						}
					}
				}

				bindingContext.ModelState.AddModelError("null",
					new HttpException(404, string.Format("Could not find {0} ({1}: {2}", entityType, fieldName, id)));
				return instance;
			}
		}

		/// <summary>
		/// Получение данных вложенных моделей
		/// </summary>
		/// <param name="model">текущая модель</param>
		/// <param name="form">источник данных (форма)</param>
		/// <param name="parent">родительский элемент, от которого будет начинаться поиск (модель)</param>
		/// <returns></returns>
		public object GetModelProperties(object model, NameValueCollection form, string parent)
		{
			var propType = model.GetType();
			var props = model.GetType().GetProperties();

			foreach (var propVal in props) {
				// определение списка *Списки обязательно должны быть определены при инициализации модели!
				if ((propVal.PropertyType).Namespace == "System.Collections.Generic") {
					var checkForListType = propVal.PropertyType.GetGenericArguments().FirstOrDefault();
					if (checkForListType != null) {
						// ищем элементы списка на форме
						var newList = (IList)propVal.GetValue(model, new object[] { });
						PropertyInfo propForListObj = model.GetType().GetProperty(propVal.Name, BindingFlags.Public | BindingFlags.Instance);
						for (int i = 0; i < form.AllKeys.Length; i++) {
							// Если совпадений нет для i-го эл-та, ищем следующий элемент
							if (!form.AllKeys.Any(s => s.IndexOf(checkForListType.FullName + "[" + i + "]") != -1)) {
								break;
							}
							else {
								if (i == 0) {
									// Если список есть на форме, чистим список из БД
									newList.Clear();
								}
							}
							// Создание эл-та списка
							var newListItem = Activator.CreateInstance(checkForListType);

							// поиск по дочернему элементу списка
							GetModelProperties(newListItem, form, checkForListType.FullName + "[" + i + "]");

							//добавление значений
							newList.Add(newListItem);
						}
						SetValueToModelProperty(model, propVal.Name, newList, propForListObj, session);
					}
				}
				// поиск на форме значения необходимого свойства
				object propertyValue = form.Get(parent.ToLower() + "." + propVal.Name);
				// получение по типу модели данные о необходимом свойстве
				PropertyInfo propCurrent = model.GetType().GetProperty(propVal.Name, BindingFlags.Public | BindingFlags.Instance);
				// уточнение, есть ли на форме необходимое свойство
				if (form.AllKeys.Any(s => ("." + s + ".").ToLower().IndexOf("." + parent.ToLower() + "." + propVal.Name.ToLower() + ".") != -1)) {
					// если на форме нет значения свойства
					if (propertyValue == null && propCurrent != null) {
						if (!propCurrent.PropertyType.IsInterface && propCurrent.PropertyType.IsSubclassOf(typeof(BaseModel))) {
							var getIdOfObject = form.Get(parent.ToLower() + "." + propVal.Name + ".id");
							// поиск в БД значения необходимого свойства, если оно отсуствует - возврат пустого значения 
							var id = getIdOfObject != null ? Convert.ToInt32(getIdOfObject) : 0;
							propertyValue = id == 0 ? Activator.CreateInstance(propCurrent.PropertyType) : session.Get(propCurrent.PropertyType, id);
						}
					}
					// если у необходимого свойства есть значение
					if (propertyValue != null) {
						// добавление значения в модель (предку)
						SetValueToModelProperty(model, propVal.Name, propertyValue, propCurrent, session);
						// поиск по дочернему элементу
						GetModelProperties(propertyValue, form, parent.ToLower() + "." + propVal.Name);
					}
				}
			}
			return null;
		}

		private int GetId(ValueProviderResult result, string fieldName)
		{
			return GetId(result.AttemptedValue, fieldName);
		}

		private static int GetId(string attemptedValue, string fieldName)
		{
			try {
				return int.Parse(attemptedValue);
			}
			catch (FormatException) {
				throw new ArgumentException(string.Format("Invalid value for field {0}: {1}", fieldName, attemptedValue));
			}
		}

		private bool FieldNotFoundOrValueIsEmpty(ValueProviderResult result)
		{
			return result == null || string.IsNullOrEmpty(result.AttemptedValue) ||
			       result.AttemptedValue == "System.Web.Mvc.UrlParameter";
		}

		public void SetValueToModelProperty(object inputObject, string propertyName, object propertyVal, PropertyInfo info, ISession session)
		{
			//find out the type
			var type = inputObject.GetType();

			//get the property information based on the type
			var propertyInfo = type.GetProperty(propertyName);

			//Convert.ChangeType does not handle conversion to nullable types
			//if the property type is nullable, we need to get the underlying type of the property
			var targetType = IsNullableType(propertyInfo.PropertyType)
				? Nullable.GetUnderlyingType(propertyInfo.PropertyType)
				: propertyInfo.PropertyType;

			if (targetType == typeof(Boolean))
				propertyVal = propertyVal.ToString().ToLower().Contains("true");
			else if (targetType == typeof(DateTime)) {
				DateTime date;
				if (!DateTime.TryParse(propertyVal.ToString(), out date))
					date = DateTime.MinValue;
				propertyVal = date;
			}
			else if (targetType.BaseType == typeof(Enum)) {
				propertyVal = Enum.Parse(targetType, propertyVal.ToString());
			}


			//Returns an System.Object with the specified System.Type and whose value is
			//equivalent to the specified object.
			try {
				propertyVal = Convert.ChangeType(propertyVal, targetType);
			}
			catch (Exception e) {
				if (!targetType.IsInterface && targetType.IsSubclassOf(typeof(BaseModel))) {
					//TODO Что делать со свойствами типа Proxy?
					if (!propertyVal.GetType().ToString().Contains("Proxy")) {
						var id = 0;
						int.TryParse((string)propertyVal, out id);
						var instance = id == 0 ? Activator.CreateInstance(propertyInfo.PropertyType) : session.Get(propertyInfo.PropertyType, id);
						propertyInfo.SetValue(inputObject, instance, null);
					}
				}
				return;
			}

			//Set the value of the property
			propertyInfo.SetValue(inputObject, propertyVal, null);
		}

		private static bool IsNullableType(Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}
	}
}