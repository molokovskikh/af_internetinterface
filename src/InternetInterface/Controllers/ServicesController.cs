using System.Linq;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models.Services;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class ServicesController : BaseController
	{
		public void Edit()
		{
			var rent = DbSession.Query<IpTvBoxRent>().First();
			PropertyBag["rent"] = rent;

			if (IsPost) {
				BindObjectInstance(rent, "rent");
				if (IsValid(rent)) {
					DbSession.SaveOrUpdate(rent);
					Notify("Сохранено");
					Redirect("UserInfo", "Administration");
				}
			}
		}
	}
}