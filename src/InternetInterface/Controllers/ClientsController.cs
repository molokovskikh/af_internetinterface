using System;
using System.Collections;
using System.Linq;
using Castle.ActiveRecord.Framework;
using Castle.MonoRail.Framework;
using Common.MySql;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Models;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	public class ClientsController : BaseController
	{
		[return: JSONReturnBinder]
		public object Search(string text)
		{
			uint id;
			var dissolved = Status.Get(StatusType.Dissolved, DbSession);
			uint.TryParse(text, out id);
			return DbSession.Query<Client>()
				.Where(c => c.Name.Contains(text) || c.Id == id)
				.Where(c => c.Status != dissolved)
				.Take(20)
				.ToList()
				.Select(c => new
				{
					id = c.Id,
					name = String.Format("[{0}]. {1}", c.Id, c.Name)
				});
		}


		/// <summary>
		/// Используется в связи с переходом на новую админку
		/// </summary>
		public void UpdateAddressByClient(int clientId, string path)
		{
			UpdateOldAddressHelper.UpdateOldAddressOfPhysicByClientId(clientId, DbSession);
			RedirectToUrl(path);
		}
	}
}