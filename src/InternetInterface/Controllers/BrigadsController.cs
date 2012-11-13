using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Queries;

namespace InternetInterface.Controllers
{
	[Helper(typeof(PaginatorHelper)),
	 Helper(typeof(CategorieAccessSet)),]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class BrigadsController : BaseController
	{
		public void ShowBrigad()
		{
			PropertyBag["Brigads"] = Brigad.FindAllSort();
		}

		public void MakeBrigad()
		{
			PropertyBag["Brigad"] = new Brigad();
			PropertyBag["Editing"] = false;
			PropertyBag["VB"] = new ValidBuilderHelper<Brigad>(new Brigad());
		}

		public void MakeBrigad(uint brigad)
		{
			PropertyBag["Brigad"] = Brigad.Find(brigad);
			PropertyBag["brigadid"] = brigad;
			PropertyBag["Editing"] = true;
			PropertyBag["VB"] = new ValidBuilderHelper<Brigad>(new Brigad());
		}

		public void RegisterBrigad([DataBind("Brigad")] Brigad brigad)
		{
			if (Validator.IsValid(brigad)) {
				brigad.SaveAndFlush();
				RedirectToUrl("../Brigads/ShowBrigad.rails");
			}
			else {
				RenderView("MakeBrigad");
				Flash["Editing"] = false;
				brigad.SetValidationErrors(Validator.GetErrorSummary(brigad));
				Flash["VB"] = new ValidBuilderHelper<Brigad>(brigad);
				Flash["Brigad"] = brigad;
			}
		}

		public void EditBrigad([DataBind("Brigad")] Brigad brigad, uint brigadid)
		{
			if (Validator.IsValid(brigad)) {
				var edbrigad = Brigad.Find(brigadid);
				BindObjectInstance(edbrigad, ParamStore.Form, "Brigad");
				edbrigad.UpdateAndFlush();
				RedirectToUrl("../Brigads/ShowBrigad.rails");
			}
			else {
				brigad.SetValidationErrors(Validator.GetErrorSummary(brigad));
				brigad.Id = brigadid;
				PropertyBag["Editing"] = true;
				Flash["VB"] = new ValidBuilderHelper<Brigad>(brigad);
				Flash["Brigad"] = brigad;
				RenderView("MakeBrigad");
			}
		}

		public void ReportOnWork([SmartBinder("filter")] BrigadFilter filter)
		{
			PropertyBag["filter"] = filter;
			var result = filter.Find(DbSession);
			PropertyBag["SClients"] = result;
			PropertyBag["NoConnected"] = result.Count(c => c.client.BeginWork == null);
		}
	}
}