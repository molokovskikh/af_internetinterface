using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using Inforoom2.Controllers;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Linq;
using Expression = NHibernate.Criterion.Expression;

namespace Inforoom2.Components
{
	public class InforoomModelFilter<TModel> : ModelFilter<TModel>
	{
		public InforoomModelFilter(BaseController controller)
			: base(controller)
		{
		}

		// переопредиление получения критерия для фильтрации 
		public new ICriteria GetCriteria(Expression<Func<TModel, bool>> expression = null)
		{
			var criteria = base.GetCriteria(expression);
			ProcessClientRegionFilter(criteria);
			ProcessServiceMan(criteria);
			return criteria;
		}

		/// <summary>
		/// Фильтрация клиентов по регионам, с учетом их типа
		/// </summary>
		/// <param name="criteria">Критерия</param>
		protected void ProcessClientRegionFilter(ICriteria criteria)
		{
			//фильтр срабатывает только если есть его "префикс"
			var paramname = Params.AllKeys.FirstOrDefault(i => i.Contains("clientregionfilter"));
			if (paramname == null)
				return;
			//получаем название региона
			var value = GetParam(paramname);
			int regionId = 0;
			int.TryParse(value, out regionId);
			string postfixName = regionId == 0 ? ".Name" : ".Id";

			var path = StripParamToFieldPath(paramname.Replace("clientregionfilter.", ""));
			path = (path.Contains("Client") ? path + "." : path);
			//для физиков учитываем привязку дома к региону
			var clientPath = path + "PhysicalClient";
			//для физиков учитываем привязку дома к региону
			var housepath = path + "PhysicalClient.Address.House.Region" + postfixName;
			//для физиков учитываем привязку улицы к региону
			var streetpath = path + "PhysicalClient.Address.House.Street.Region" + postfixName;
			//юриков находим вот так
			var lawerRegionPath = path + "LegalClient.Region" + postfixName;

			//создаем псевдонимы, формируя связи
			var clientlias = GetJoinedModelCriteria(criteria, clientPath).Alias;
			var housealias = GetJoinedModelCriteria(criteria, housepath).Alias;
			var streetalias = GetJoinedModelCriteria(criteria, streetpath).Alias;
			var lawerRegionAlias = GetJoinedModelCriteria(criteria, lawerRegionPath).Alias;

			// если есть значение, формируем и добавляем условия фильтрации в критэрию фильтра
			if (string.IsNullOrEmpty(value) == false || regionId != 0) {
				var expr = Expression.And(Expression.IsNotNull(housealias + postfixName),
					Expression.Eq(housealias + postfixName, regionId != 0 ? (object) regionId : value));
				var expr2 = Expression.And(Expression.IsNull(housealias + postfixName),
					Expression.Eq(streetalias + postfixName, regionId != 0 ? (object) regionId : value));

				var exprPhysicalClient = Expression.And(Expression.IsNotNull(clientlias + ".PhysicalClient"),
					Expression.Or(expr, expr2));
				var exprLawerClient = Expression.And(Expression.IsNull(clientlias + ".PhysicalClient"),
					Expression.Eq(lawerRegionAlias + postfixName, regionId != 0 ? (object) regionId : value));

				Criteria.Add(Expression.Or(exprPhysicalClient, exprLawerClient));
			}
		}

		public HtmlString ClientRegionFilter(Expression<Func<TModel, object>> expression, object htmlAttributes = null,
			string propertyText = "", string propertyValue = "")
		{
			//Тут нужно получить тип поля, чтобы убедиться что это клиент. Если нет то кидать эксепшн
			if (expression.Body.Type != typeof (Client)) {
				throw new Exception("Данная функция доступна только для фильтрации клиентов (модель Client)");
			}

			var path = ExtractFieldNameFromLambda(expression);
			var inputName = string.Format("{0}.clientregionfilter.{1}", Prefix, path);
			var attrs = ObjectToDictionary(htmlAttributes);

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

			attrs["class"] = " form-control " + attrs["class"];
			//заполняем выбранное значение, если оно есть
			string selectedValue = null;
			if (attrs.ContainsKey("value"))
				selectedValue = attrs["value"];

			List<string> values;
			var attrName = attrs["name"];

			var pathOfFilter = attrName;
			if (attrName.Contains("clientregionfilter")) {
				pathOfFilter = pathOfFilter.Replace(".clientregionfilter.", ".filter.");
				if (pathOfFilter.Contains("Client")) {
					pathOfFilter = pathOfFilter.Replace(".Client", ".Client.PhysicalClient.Address.House.Region.Name");
				}
				else {
					pathOfFilter += "PhysicalClient.Address.House.Region.Name";
				}
			}

			var customValueList = htmlAttributes as NameValueCollection;
			if (customValueList != null)
				values =
					customValueList.AllKeys.Select(
						i =>
							string.Format("<option {2} value='{0}'>{1}</option>", i, customValueList[i],
								selectedValue == i ? "selected='selected'" : "")).ToList();
			else
				values = TryToGetDropDownValueList(ComparsionType.Equal, pathOfFilter, selectedValue, propertyText, propertyValue);

			var html = string.Format("<select {0}>{1}</select>", GetPropsValues(attrs), string.Join("\n", values));


			var ret = new HtmlString(html);
			return ret;
		}


		/// <summary>
		/// Фильтрация клиентов по регионам, с учетом их типа
		/// </summary>
		/// <param name="criteria">Критерия</param>
		protected void ProcessServiceMan(ICriteria criteria)
		{
			//фильтр срабатывает только если есть его "префикс"
			var paramname = Params.AllKeys.FirstOrDefault(i => i.Contains("servicemanfilter"));
			if (paramname == null)
				return;
			//для физиков учитываем привязку дома к региону
			var ServicemenScheduleItems = "ServicemenScheduleItems";
			//для физиков учитываем привязку дома к региону
			var serviceMan = "ServicemenScheduleItems.ServiceMan";
			//для физиков учитываем привязку дома к региону
			var employee = "ServicemenScheduleItems.ServiceMan.Employee.Id";

			//создаем псевдонимы, формируя связи
			var servicemenScheduleItemslias = GetJoinedModelCriteria(criteria, ServicemenScheduleItems).Alias;
			var serviceManalias = GetJoinedModelCriteria(criteria, serviceMan).Alias;
			var employeeAlias = GetJoinedModelCriteria(criteria, employee).Alias;

			//получаем название региона
			var value = GetParam(paramname);
			// если есть значение, формируем и добавляем условия фильтрации в критэрию фильтра
			if (!string.IsNullOrEmpty(value)) {
				var exprLawerClient = Expression.Eq(employeeAlias + ".Id", Convert.ToInt32(value));
				Criteria.Add(exprLawerClient);
			}
		}

		public HtmlString ServiceManFilter(Expression<Func<TModel, object>> expression, object htmlAttributes = null)
		{
			//Тут нужно получить тип поля, чтобы убедиться что это клиент. Если нет то кидать эксепшн
			if (expression.Body.Type != typeof (Client)) {
				throw new Exception("Данная функция доступна только для фильтрации клиентов (модель Client)");
			}
			var inputName = string.Format("{0}.servicemanfilter.{1}", Prefix, "ServicemenScheduleItems.ServiceMan");
			var attrs = ObjectToDictionary(htmlAttributes);
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

			attrs["class"] = " form-control " + attrs["class"];
			//заполняем выбранное значение, если оно есть
			string selectedValue = null;
			if (attrs.ContainsKey("value"))
				selectedValue = attrs["value"];

			var customValueList = DbSession.Query<ServiceMan>().OrderBy(s => s.Employee.Name).ToList();

			List<string> values =
				customValueList.Select(
					i =>
						string.Format("<option {2} value='{0}'>{1}</option>", i.Employee.Id, i.Employee.Name,
							selectedValue == i.Employee.Id.ToString() ? "selected='selected'" : "")).ToList();
			values.Insert(0, "<option value=''></option>");
			var html = string.Format("<select {0}>{1}</select>", GetPropsValues(attrs), string.Join("\n", values));

			var ret = new HtmlString(html);
			return ret;
		}
	}
}