using System;
using System.Linq;
using Castle.ActiveRecord.Framework;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
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
				.Select(c => new {
					id = c.Id,
					name = String.Format("[{0}]. {1}", c.Id, c.Name)
				});
		}
	}
}