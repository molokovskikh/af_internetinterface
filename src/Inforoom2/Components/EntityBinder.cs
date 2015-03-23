using System;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
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

		public EntityBinderAttribute(Type entityType, bool relaxed = false) : this("Id", entityType, relaxed)
		{
		}


		public EntityBinderAttribute(string idName, bool relaxed = false) : this(idName, null, relaxed)
		{
		}

		public EntityBinderAttribute(bool relaxed = false) : this("Id", relaxed)
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
				if (props.Count() != 0)
				{
					foreach (var propertyInfo in props)
					{
						var propertyName = propertyInfo.Name;
						var propertyValue = request.Form.Get(entityType.Name.ToLower() + "." + propertyName);
						if (!string.IsNullOrEmpty(propertyValue))
						{
							SetValue(instance, propertyName, propertyValue, propertyInfo,null);
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
				if(result != null)
					id = GetId(result, fieldName);
				var instance = session.Get(entityType, id) ?? Activator.CreateInstance(entityType);
				var props = instance.GetType().GetProperties();
				if (props.Count() != 0) {
					foreach (var propertyInfo in props) {
						var propertyName = propertyInfo.Name;
						var propertyValue = request.Form.Get(entityType.Name.ToLower() + "." + propertyName);
						var propertyValue2 = request.Form.Get(entityType.Name.ToLower() + "proxy." + propertyName);
						if (propertyValue != null) {
							SetValue(instance, propertyName, propertyValue, propertyInfo,session);
						}
						else if(propertyValue2 != null)
						{
							SetValue(instance, propertyName, propertyValue2, propertyInfo,session);
						}
						var objectId = request.Form.Get(entityType.Name.ToLower() + "." + propertyName + ".Id");
						var objectId2 = request.Form.Get(entityType.Name.ToLower() + "proxy." + propertyName + ".Id");
						if (!string.IsNullOrEmpty(objectId))
						{
							SetValue(instance, propertyName, objectId, propertyInfo, session);
						}
						else if(objectId2 != null)
						{
							SetValue(instance, propertyName, objectId2, propertyInfo, session);
						}
					}
				}

				bindingContext.ModelState.AddModelError("null",
					new HttpException(404, string.Format("Could not find {0} ({1}: {2}", entityType, fieldName, id)));
				return instance;
			}
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

		public void SetValue(object inputObject, string propertyName, object propertyVal, PropertyInfo info, ISession session)
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
				propertyVal = propertyVal.ToString().Contains("true");
			else if (targetType == typeof(DateTime))
			{
				DateTime date;
				if(!DateTime.TryParse(propertyVal.ToString(),out date))
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
				if (!propertyInfo.PropertyType.IsInterface) {
					var id = 0;
					int.TryParse((string) propertyVal, out id);
					var instance = session.Get(targetType, id) ?? Activator.CreateInstance(propertyInfo.PropertyType);
					propertyInfo.SetValue(inputObject, instance, null);
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