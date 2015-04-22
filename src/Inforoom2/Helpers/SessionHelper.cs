using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.UI.WebControls;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;
using NHibernate.Linq.Functions;
using NHibernate.Validator.Cfg.Loquacious;

namespace Inforoom2.Helpers
{

	public static class SessionHelper
	{
		/// <summary>
		/// Пытается удалить объект. Возврает индикатор успешного выполнения задачи.
		/// Очищает сессию, в случае неудачи.
		/// </summary>
		/// <param name="DbSession">Сессия Nhibernate</param>
		/// <param name="model">Модель, которую необходимо удалить</param>
		/// <returns>True, в случае успеха</returns>
		public static bool AttemptDelete(this NHibernate.ISession DbSession, BaseModel model)
		{
			try
			{
				DbSession.Delete(model);
				//Внешние ключи могут не дать удалить объект
				//В этом случае, если сейчас не закрыть транзакцию, то приложение упадет с ошибкой, когда будет когда будет
				//произведено автоматическое закрытие транзакии в BaseController
				DbSession.Transaction.Commit();
				return true;
			}
			catch (Exception e)
			{
				DbSession.Clear();
			}
			return false;
		}
	}
}