using System;
using System.Collections;
using System.Collections.Specialized;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Castle.Core.Internal;
using Common.Web.Ui.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Mapping;
using NHibernate.Type;
using Castle.Components.Binder;
using System.Linq;
using System.Collections.Generic;

namespace InternetInterface.Controllers
{
	public class InternetInterfaceController : BaseController
	{
		public InternetInterfaceController()
		{
			BeforeAction += (action, context1, controller, controllerContext) => {
				controllerContext.PropertyBag["config"] = Global.Config;
			};
		}

		protected void RedirectTo(Client client)
		{
			string uri;
			if (client.GetClientType() == ClientType.Phisical) {
				uri = "~/UserInfo/SearchUserInfo.rails?filter.ClientCode={0}";
			}
			else {
				uri = "~/UserInfo/LawyerPersonInfo.rails?filter.ClientCode={0}";
			}
			RedirectToUrl(string.Format(uri, client.Id));
		}

		public SmsHelper SmsHelper = new SmsHelper();

		protected Partner Partner
		{
			get { return InitializeContent.Partner; }
		}

		protected void RenderJson(object data)
		{
			Response.ContentType = "application/json";
			RenderText(JsonConvert.SerializeObject(data));
		}

		protected AppConfig Config
		{
			get { return Global.Config; }
		}

		/// <summary>
		/// Возвращает список ошибок данных в объекте ActiveRecord и его потомков.
		/// Из вложенных объектов учитываются только объекты с аттрибутами HasMany,Nested и OneToOne
		/// </summary>
		/// <param name="obj">Объект c аттрибутом ActiveRecord</param>
		/// <param name="validatedObjects">Список уже проверенных объектов</param>
		/// <returns>Список ошибок объекта, который первый не прошел валидацию. В случае отсутствия ошибок список пустой.</returns>
		public ErrorSummary ValidateDeep(object obj, IList validatedObjects = null)
		{
			if(validatedObjects == null)
				validatedObjects = new ArrayList();

			if(!obj.GetType().HasAttribute<ActiveRecordAttribute>())
				throw new Exception("Объект не имеет аттрибута ActiveRecord");

			var runner = new ValidatorRunner(new CachedValidationRegistry());
			var valid = runner.IsValid(obj);
			var summary = runner.GetErrorSummary(obj);
			if (!valid)
				return summary;
			validatedObjects.Add(obj);

			var allprops = obj.GetType().GetProperties();
			//Код оставил, так как считаю, что он может потребоваться в будущем
			//BelongsTo проверка на пустоту (без валиадции) - важно только для главного узла
			//Так как ссылки на родителя не выставляются при автоматическом создании моделей (даже при сохранении)
			//if (validatedObjects.Count == 0) {
			//	var bprops = allprops.Where(prop => Attribute.IsDefined(prop, typeof (BelongsToAttribute)));
			//	foreach (var p in bprops) {
			//		var value = p.GetValue(obj, null);
			//		if (value == null)
			//			summary.RegisterErrorMessage(p.Name, obj.GetType().Name + " принадлежит пустому объекту " + p.Name);
			//	}
			//	if (summary.HasError)
			//		return summary;
			//}

			//HasMany атрибуты
			var props = allprops.Where(prop => Attribute.IsDefined(prop, typeof(HasManyAttribute)));
			foreach (var p in props) {
				var value = p.GetValue(obj, null);
				if (!NHibernateUtil.IsInitialized(value))
					continue;
				var list = (ICollection)value;
				foreach (var o in list) {
					if(validatedObjects.Contains(o))
						continue;
					var errors = ValidateDeep(o, validatedObjects);
					if (errors.HasError)
						return errors;
				}
			}

			//OneToOne and Nested атрибуты
			var oneToOne = allprops.Where(prop => Attribute.IsDefined(prop, typeof(OneToOneAttribute)));
			var nested = allprops.Where(prop => Attribute.IsDefined(prop, typeof(NestedAttribute)));
			props = oneToOne.Concat(nested);
			foreach (var p in props) {
				var value = p.GetValue(obj, null);
				if(!NHibernateUtil.IsInitialized(value) || validatedObjects.Contains(value))
					continue;
				var errors = ValidateDeep(value, validatedObjects);
				if (errors.HasError)
					return errors;
			}

			return summary;
		}
	}
}