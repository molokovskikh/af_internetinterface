using System.Linq;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class ServicesController : BaseController
	{
		public ServicesController()
		{
			SetSmartBinder(AutoLoadBehavior.NullIfInvalidKey);
		}

		public void Edit()
		{
			PropertyBag["services"] = DbSession.Query<Service>().Where(s => s.Configurable)
				.OrderBy(s => s.HumanName)
				.ToList();

			if (IsPost) {
				var services = BindObject<Service[]>("services");
				if (IsValid(services)) {
					Notify("Сохранено");
					Redirect("UserInfo", "Administration");
				}
			}
		}
	}
}