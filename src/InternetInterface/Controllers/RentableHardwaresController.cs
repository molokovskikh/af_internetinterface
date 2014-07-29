using System.Linq;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class RentableHardwaresController : InternetInterfaceController
	{
		public void Index()
		{
			PropertyBag["items"] = DbSession.Query<RentableHardware>().OrderBy(h => h.Name).ToList();
		}

		public void New()
		{
			var item = new RentableHardware();
			PropertyBag["item"] = item;
			if (IsPost) {
				Bind(item, "item");
				if (IsValid(item)) {
					DbSession.Save(item);
					Notify("Сохранено");
					RedirectToAction("Index");
				}
			}
		}

		public void Edit(uint id)
		{
			var item = DbSession.Load<RentableHardware>(id);
			PropertyBag["item"] = item;
			if (IsPost) {
				Bind(item, "item");
				if (IsValid(item)) {
					DbSession.Save(item);
					Notify("Сохранено");
					RedirectToAction("Index");
				}
			}
		}

		public void Delete(uint id)
		{
			var hardware = DbSession.Load<RentableHardware>(id);
			if (DbSession.Query<ClientService>().Any(s => s.RentableHardware == hardware)) {
				Error("Невозможно удалить оборудование тк есть клиенты которым оно выдано в аренду");
				RedirectToReferrer();
				return;
			}
			DbSession.Delete(hardware);
			Notify("Удалено");
			RedirectToAction("Index");
		}
	}
}