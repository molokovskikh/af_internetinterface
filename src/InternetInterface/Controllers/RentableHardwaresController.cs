using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using Common.MySql;
using Common.Tools;
using Common.Web.Ui.MonoRailExtentions;
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
				PerformSave(item);
			}
		}

		public void Edit(uint id)
		{
			var item = DbSession.Load<RentableHardware>(id);
			PropertyBag["item"] = item;
			if (IsPost) {
				PerformSave(item);
			}
		}

		private void PerformSave(RentableHardware item)
		{
			DoNotRecreateCollectionBinder.Prepare(this, "item.DefaultDocItems");
			Bind(item, "item");
			if (IsValid(item) && IsValid(item.DefaultDocItems)) {
				DbSession.Save(item);
				Notify("Сохранено");
				RedirectToAction("Index");
			}
			else {
				//если хотя бы один из элементов оказался не валидным мы должны все отметить как не валидные
				//что бы избежать каскадного сохранения
				Validator.GetErrorSummary(item).RegisterErrorMessage("dummy", "dummy");
				item.DefaultDocItems.Each(i => Validator.GetErrorSummary(i).RegisterErrorMessage("dummy", "dummy"));
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

		public void Show(uint id)
		{
			var item = DbSession.Load<ClientService>(id);
			PropertyBag["item"] = item;
		}

		public void ReturnDoc(uint id)
		{
			var item = DbSession.Load<ClientService>(id);
			PropertyBag["item"] = item;
			PropertyBag["docItems"] = item.GetDocItems();
			PropertyBag["header"] = "АКТ ПРИЕМА-ПЕРЕДАЧИ ОБОРУДОВАНИЯ";
			if (IsPost) {
				var items = BindObject<RentDocItem[]>("docItems");
				PropertyBag["docItems"] = items;
				PropertyBag["client"] = item.Client;
				RenderView("ReturnDocForm");
			}
			else {
				RenderView("BuildDocForm");
			}
		}

		public void RentDoc(uint id)
		{
			var item = DbSession.Load<ClientService>(id);
			PropertyBag["item"] = item;
			PropertyBag["docItems"] = item.GetDocItems();
			PropertyBag["header"] = "АКТ ВОЗВРАТА ОБОРУДОВАНИЯ";
			if (IsPost) {
				var items = BindObject<RentDocItem[]>("docItems");
				PropertyBag["docItems"] = items;
				PropertyBag["client"] = item.Client;
				RenderView("RentDocForm");
			}
			else {
				RenderView("BuildDocForm");
			}
		}

		public void Upload(uint id)
		{
			var file = Request.Files["doc"] as HttpPostedFile;
			if (file == null) {
				Error("Нужно выбрать файл для загрузки");
				RedirectToAction("Show", new { id });
				return;
			}
			var service = DbSession.Load<ClientService>(id);
			var doc = new UploadDoc(file);
			service.Docs.Add(doc);

			DbSession.Save(doc);
			doc.SaveFile(file, Config);
			Notify("Сохранено");
			RedirectToAction("Show", new { id });
		}

		public void ShowDoc(uint id)
		{
			var doc = DbSession.Load<UploadDoc>(id);
			this.RenderFile(doc.GetFilePath(Config), doc.Filename);
		}

		public void DeleteDoc(uint id)
		{
			var doc = DbSession.Load<UploadDoc>(id);
			DbSession.Delete(doc);
			RedirectToAction("Show", new { id = doc.AssignedService.Id });
		}
	}
}