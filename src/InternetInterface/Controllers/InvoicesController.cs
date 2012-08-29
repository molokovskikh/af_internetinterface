using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
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
	public class DoNotRecreateCollectionBinder : ARDataBinder
	{
		protected override bool ShouldRecreateInstance(object value, System.Type type, string prefix, Castle.Components.Binder.Node node)
		{
			return value == null;
		}

		public static void Prepare(SmartDispatcherController controller, string expect)
		{
			var binder = new DoNotRecreateCollectionBinder();
			binder.AutoLoad = AutoLoadBehavior.NewInstanceIfInvalidKey;
			typeof(ARDataBinder).GetField("expectCollPropertiesList", BindingFlags.Instance | BindingFlags.NonPublic)
				.SetValue(binder, new[] { "root." + expect });

			typeof(SmartDispatcherController)
				.GetField("binder", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(controller, binder);
		}
	}

	public class InvoiceFilter : PaginableSortable
	{
		public string SearchText { get; set; }

		[Description("Выберите год:")]
		public int? Year { get; set; }

		[Description("Выберите период:")]
		public Interval? Interval { get; set; }

		public InvoiceFilter()
		{
			SortBy = "Date";
			SortDirection = "desc";
			SortKeyMap = new Dictionary<string, string> {
				{ "Id", "Id" },
				{ "ClientId", "c.Id" },
				{ "Date", "Date" },
				{ "Sum", "Sum" },
				{ "Period", "Period" },
				{ "PayerName", "PayerName" }
			};
		}

		public IList<Invoice> Find(ISession session)
		{
			var criteria = DetachedCriteria.For<Invoice>()
				.CreateAlias("Client", "c");
			if (!string.IsNullOrEmpty(SearchText)) {
				uint id;
				if (uint.TryParse(SearchText, out id))
					criteria.Add(Expression.Eq("c.Id", id));
				else
					criteria.Add(Expression.Like("PayerName", SearchText, MatchMode.Anywhere));
			}

			if (Year != null)
				criteria.Add(Expression.Sql("{alias}.Period like " + String.Format("'{0}-%'", Year)));

			if (Interval != null)
				criteria.Add(Expression.Sql("{alias}.Period like " + String.Format("'%-{0}'", (int)Interval)));

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
			PropertyBag["printers"] = Printer.All();
		}

		public void Process()
		{
			var binder = new ARDataBinder();
			binder.AutoLoad = AutoLoadBehavior.Always;
			SetBinder(binder);
			var invoices = BindObject<Invoice[]>("invoices");

			if (invoices.Length == 0)
				RedirectToReferrer();

			if (Form["delete"] != null) {
				foreach (var invoice in invoices)
					DbSession.Delete(invoice);

				Notify("Удалено");
			}

			if (Form["email"] != null) {
				foreach (var invoice in invoices)
					this.Mailer().Invoice(invoice).Send();

				Notify("Отправлено");
			}

			if (Form["print"] != null) {
				var printer = Form["printer"];
				var arguments = String.Format("invoice \"{0}\" \"{1}\"", printer, invoices.Implode(a => a.Id));
				Printer.Execute(arguments);

				Notify("Отправлено на печать");
			}
			RedirectToReferrer();
		}

		public void New(uint id)
		{
			var client = DbSession.Load<Client>(id);
			var invoice = new Invoice(client);
			invoice.Parts.Add(new InvoicePart(invoice, 1, 0, ""));
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
				DoNotRecreateCollectionBinder.Prepare(this, "invoice.Parts");
				BindObjectInstance(invoice, "invoice");
				if (IsValid(invoice)) {
					invoice.CalculateSum();
					DbSession.SaveOrUpdate(invoice);
					Notify("Сохранено");
					RedirectToAction("Edit", new { id = invoice.Id });
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