using System.Linq;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models.Services;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class ChannelGroupsController : BaseController
	{
		public void Index()
		{
			PropertyBag["groups"] = DbSession
				.Query<ChannelGroup>()
				.OrderBy(g => g.Name)
				.ToArray();
		}

		public void Edit(uint id)
		{
			var group = DbSession.Load<ChannelGroup>(id);
			PropertyBag["group"] = group;

			if (IsPost) {
				BindObjectInstance(group, "group");
				if (IsValid(group)) {
					DbSession.SaveOrUpdate(group);
					Notify("Сохранено");
					RedirectToAction("Index");
				}
			}
		}

		public void New()
		{
			var group = new ChannelGroup();
			PropertyBag["group"] = group;

			if (IsPost) {
				BindObjectInstance(group, "group");
				if (IsValid(group)) {
					DbSession.SaveOrUpdate(group);
					Notify("Сохранено");
					RedirectToAction("Index");
				}
			}
			RenderView("Edit");
		}
	}
}