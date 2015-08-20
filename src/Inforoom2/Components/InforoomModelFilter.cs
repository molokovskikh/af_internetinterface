﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized; 
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using Inforoom2.Controllers;
using Inforoom2.Models;
using NHibernate; 
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
			var path = StripParamToFieldPath(paramname.Replace("clientregionfilter.", ""));
			path = (path.Contains("Client") ? path + "." : path);
			//для физиков учитываем привязку дома к региону
			var housepath = path + "PhysicalClient.Address.House.Region.Name";
			//для физиков учитываем привязку улицы к региону
			var streetpath = path + "PhysicalClient.Address.House.Street.Region.Name";
			//юриков находим вот так
			var lawerRegionPath = path + "Endpoints.Switch.NetworkNode.Addresses.House.Street.Region.Name";

			//создаем псевдонимы, формируя связи
			var housealias = GetJoinedModelCriteria(criteria, housepath).Alias;
			var streetalias = GetJoinedModelCriteria(criteria, streetpath).Alias;
			var lawerRegionAlias = GetJoinedModelCriteria(criteria, lawerRegionPath).Alias;

			//получаем название региона
			var value = GetParam(paramname);
			// если есть значение, формируем и добавляем условия фильтрации в критэрию фильтра
			if (!string.IsNullOrEmpty(value)) {

				var expr = Expression.And(Expression.IsNotNull(housealias + ".Name"), Expression.Eq(housealias + ".Name", value));
				var expr2 = Expression.And(Expression.IsNull(housealias + ".Name"), Expression.Eq(streetalias + ".Name", value));

				var exprPhysicalClient = Expression.And(Expression.Eq("Type", (ClientType)1), Expression.Or(expr, expr2));
				var exprLawerClient = Expression.And(Expression.Eq("Type", (ClientType)2), Expression.Eq(lawerRegionAlias + ".Name", value));

				Criteria.Add(Expression.Or(exprPhysicalClient, exprLawerClient));
			}
		}

		public HtmlString ClientRegionFilter(Expression<Func<TModel, object>> expression, object htmlAttributes = null)
		{
			//Тут нужно получить тип поля, чтобы убедиться что это клиент. Если нет то кидать эксепшн
			if (expression.Body.Type != typeof(Client)) {
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
				values = customValueList.AllKeys.Select(i => string.Format("<option {2} value='{0}'>{1}</option>", i, customValueList[i], selectedValue == i ? "selected='selected'" : "")).ToList();
			else
				values = TryToGetDropDownValueList(pathOfFilter, selectedValue);

			var html = string.Format("<select {0}>{1}</select>", GetPropsValues(attrs), string.Join("\n", values));


			var ret = new HtmlString(html);
			return ret;
		}
	}
}