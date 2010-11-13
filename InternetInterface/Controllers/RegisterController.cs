using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate.Criterion;


namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class RegisterController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Post)]
		public void RegisterClient([DataBind("client")]Client user, bool popolnenie, uint tariff)
		{
			Session["df"] = "sdf";
			if (Client.RegistrLogicClient(user, popolnenie, tariff, Validator,
										  Partner.FindAllByProperty("Login", Session["Login"].ToString())[0]))
			{
				PropertyBag["Applying"] = "true";
				PropertyBag["Client"] = new Client();
				PropertyBag["Tariffs"] = Tariff.FindAllSort();
				PropertyBag["Popolnen"] = popolnenie;
			}
			else
			{
				user.SetValidationErrors(Validator.GetErrorSummary(user));
				PropertyBag["Client"] = user;
				PropertyBag["Tariffs"] = Tariff.FindAllSort();
				PropertyBag["Applying"] = "false";
				PropertyBag["Popolnen"] = popolnenie;
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void RegisterPartner([DataBind("Partner")]Partner partner, [DataBind("ForRight")]List<int> rights)
		{
			if (Partner.RegistrLogicPartner(partner, rights, Validator))
			{
				PropertyBag["Applying"] = "true";
				PropertyBag["Rights"] =
					ActiveRecordBase<AccessCategories>.FindAll(
						DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("ReduceName <> 'RP'")));
				PropertyBag["Partner"] = new Partner();
			}
			else
			{
				partner.SetValidationErrors(Validator.GetErrorSummary(partner));
				PropertyBag["Rights"] =
					ActiveRecordBase<AccessCategories>.FindAll(
						DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("ReduceName <> 'RP'")));
				PropertyBag["Partner"] = partner;
				PropertyBag["Applying"] = "false";
			}
		}

		public void RegisterClient()
		{
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["Client"] = new Client();
			PropertyBag["Applying"] = "false";
			PropertyBag["Popolnen"] = false;
		}

		public void RegisterPartner()
		{

			PropertyBag["Rights"] =
				ActiveRecordBase<AccessCategories>.FindAll(DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("ReduceName <> 'RP'")));
			PropertyBag["Partner"] = new Partner();
			PropertyBag["Applying"] = "false";
		}
	}

}