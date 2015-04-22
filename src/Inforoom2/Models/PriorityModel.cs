using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Mvc;
using System.Web.Services.Configuration;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping;
using NHibernate.Mapping.Attributes;
using NHibernate.Mapping.ByCode.Impl;
using NHibernate.Proxy;
using NHibernate.Type;
using NHibernate.Util;
using NHibernate.Validator.Cfg.Loquacious.Impl;
using NHibernate.Validator.Engine;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель с параметром приоритета. Обычно сортируется по этому приоритету.
	/// Низший приоритет - 0. Высший приоритет - бесконечность. Все модели создаются с приоритетом 0. 
	/// Если при изменении приоритета модели обнаруживается, что существуют модели с таким же приоритетом, то выравнивается все модели данного типа.
	/// </summary>
	public class PriorityModel : BaseModel
	{
		[Property,Description("Приоритет для сортировки")]
		public virtual int Priority { get; set; }

		/// <summary>
		/// Выравнивает приоритеты моделей. Если существуют еще модели с таким же приоритетом, то переформировываются приоритеты у всех моделей.
		/// Функция производит сохранение моделей.
		/// </summary>
		/// <param name="session">Сесиия Nhibernate</param>
		protected virtual void NormalizePriority(ISession session)
		{
			var samePriorityModels = session.CreateCriteria(GetType())
			.Add(NHibernate.Criterion.Restrictions.Eq("Priority", Priority))
			.Add(NHibernate.Criterion.Restrictions.Not(NHibernate.Criterion.Restrictions.IdEq(Id)))
			.List();

			if (samePriorityModels.Count > 0)
				MakeSpace(session);
		}

		/// <summary>
		/// Выравнивает приоритеты моделей, располагая их по порядку.
		/// Функция производит сохранение моделей.
		/// </summary>
		/// <param name="session">Сесиия Nhibernate</param>
		protected virtual void MakeSpace(ISession session)
		{
			var allmodels = session.CreateCriteria(GetType())
				.AddOrder(NHibernate.Criterion.Order.Asc("Priority"))
				.List();
			for (var i = 0; i < allmodels.Count; i++)
			{
				var model = allmodels[i] as PriorityModel;
				model.Priority = 1 + i;
				session.Save(model);
			}
		}
	
		/// <summary>
		/// Увеличивает приоритет модели
		/// </summary>
		/// <param name="session">Сесиия Nhibernate</param>
		public virtual void IncereasePriority(ISession session)
		{
			NormalizePriority(session);
			var model = session.CreateCriteria(GetType())
				.SetMaxResults(1)
				.Add(NHibernate.Criterion.Restrictions.Gt("Priority", Priority))
				.AddOrder(NHibernate.Criterion.Order.Asc("Priority"))
				.AddOrder(NHibernate.Criterion.Order.Desc("Id"))
				.List().FirstOrNull();
			var pmodel = model as PriorityModel;
			if (pmodel == null)
				return;

			var tmp = Priority;
			Priority = pmodel.Priority;
			pmodel.Priority = tmp;
			session.Save(this);
			session.Save(pmodel);
		}

		/// <summary>
		/// Уменьшает приоритет модели
		/// </summary>
		/// <param name="session">Сесиия Nhibernate</param>
		public virtual void DecreasePriority(ISession session)
		{
			NormalizePriority(session);
			var nextPriorityModel = session.CreateCriteria(GetType())
				.SetMaxResults(1)
				.Add(NHibernate.Criterion.Restrictions.Lt("Priority", Priority))
				.AddOrder(NHibernate.Criterion.Order.Desc("Priority"))
				.List().FirstOrNull();

			var pmodel = nextPriorityModel as PriorityModel;
			if (pmodel == null)
				return;

			var tmp = Priority;
			Priority = pmodel.Priority;
			pmodel.Priority = tmp;
			session.Save(this);
			session.Save(pmodel);
		}
	}
}