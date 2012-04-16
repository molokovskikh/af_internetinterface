using System.Collections.Generic;
using System.Linq;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	public class InvoiceFilter : PaginableSortable
	{
		public string SearchText { get; set; }

		public InvoiceFilter()
		{
			SortBy = "Date";
			SortDirection = "desc";
			SortKeyMap = new Dictionary<string, string> {
				{"Id", "Id"},
				{"ClientId", "ClientId"},
				{"Date", "Date"},
				{"Sum", "Sum"},
				{"PayerName", "PayerName"}
			};
		}

		public IList<Invoice> Find(ISession session)
		{
			var criteria = DetachedCriteria.For<Invoice>();
			if (!string.IsNullOrEmpty(SearchText))
				criteria.Add(Expression.Like("PayerName", SearchText, MatchMode.Anywhere));
			return Find<Invoice>(criteria);
		}
	}

	[Filter(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class InvoicesController : BaseController
	{
		public void Index([SmartBinder] InvoiceFilter filter)
		{
			PropertyBag["filter"] = filter;
			PropertyBag["invoices"] = filter.Find(DbSession);
		}

		public void Process()
		{
			var binder = new ARDataBinder();
			binder.AutoLoad = AutoLoadBehavior.Always;
			SetBinder(binder);
			var invoices = BindObject<Invoice[]>("invoices");

			if (Form["delete"] != null) {
				foreach (var invoice in invoices)
					DbSession.Delete(invoice);

				Notify("Удалено");
			}

			if (Form["email"] != null) {
				foreach (var invoice in invoices)
					this.Mailer().Invoice(invoice);

				Notify("Отправлено");
			}

			if (Form["print"] != null) {
				foreach (var invoice in invoices) {
					
				}

				Notify("Отправлено на печать");
			}
			RedirectToReferrer();
		}

		public void New(uint id)
		{
			var client = DbSession.Load<Client>(id);
			var invoice = new Invoice(client);
			PropertyBag["invoice"] = invoice;
			RenderView("Edit");

			SaveIfNeeded(invoice);
		}

		public void Edit(uint id)
		{
			var invoice = DbSession.Load<Invoice>(id);
			PropertyBag["invoice"] = invoice;

			SaveIfNeeded(invoice);
		}

		private void SaveIfNeeded(Invoice invoice)
		{
			if (IsPost) {
				BindObjectInstance(invoice, "invoice");
				if (IsValid(invoice)) {
					invoice.CalculateSum();
					DbSession.SaveOrUpdate(invoice);
					Notify("Сохранено");
					RedirectToReferrer();
				}
			}
		}

		public void Print(uint id)
		{
			LayoutName = "print";
			PropertyBag["invoice"] = DbSession.Load<Invoice>(id);
		}
	}
}