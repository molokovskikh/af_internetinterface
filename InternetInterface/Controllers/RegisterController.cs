using System;
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
		public void RegisterClient([DataBind("client")]Client _user, bool Popolnenie, uint tariff)
		{
			if ((PartnerAccessSet.AccesPartner(AccessCategoriesType.RegisterClient)))
			{
				Session["df"] = "sdf";
				if (Client.RegistrLogicClient(_user, Popolnenie, tariff, Validator, Partner.FindAllByProperty("Pass", Session["HashPass"].ToString())[0]))
				{
					PropertyBag["Applying"] = "true";
					PropertyBag["Client"] = new Client();
					PropertyBag["Tariffs"] = Tariff.FindAllSort();
					PropertyBag["Popolnen"] = Popolnenie;
				}
				else
				{
					_user.SetValidationErrors(Validator.GetErrorSummary(_user));
					PropertyBag["Client"] = _user;
					PropertyBag["Tariffs"] = Tariff.FindAllSort();
					PropertyBag["Applying"] = "false";
					PropertyBag["Popolnen"] = Popolnenie;
				}
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void RegisterPartner([DataBind("Partner")]Partner _Partner, [DataBind("ForRight")]List<uint> _Rights)
		{
			if (PartnerAccessSet.AccesPartner(AccessCategoriesType.RegisterPartner))
			{
				if (Partner.RegistrLogicPartner(_Partner, _Rights, Validator))
				{
					PropertyBag["Applying"] = "true";
					PropertyBag["Rights"] = ActiveRecordBase<AccessCategories>.FindAll(DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("Code <> 16")));
					PropertyBag["Partner"] = new Partner();
				}
				else
				{
					_Partner.SetValidationErrors(Validator.GetErrorSummary(_Partner));
					PropertyBag["Rights"] = ActiveRecordBase<AccessCategories>.FindAll(DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("Code <> 16")));
					PropertyBag["Partner"] = _Partner;
					PropertyBag["Applying"] = "false";
				}
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}

		/*private uint CombineAccess(List<uint> Rights)
		{
			uint rightSet = 0;
			foreach (uint right in Rights)
			{
				rightSet += right;
			}
			if (((rightSet | (uint)AccessCategoriesType.SendDemand) == (uint)AccessCategoriesType.SendDemand) &&
			((rightSet | (uint)AccessCategoriesType.GetClientInfo) != (uint)AccessCategoriesType.GetClientInfo))
			{
				rightSet += (uint) AccessCategoriesType.GetClientInfo;
			}
			return rightSet;
		}*/

		//[AccessibleThrough(Verb.Get)]
		public void RegisterClient()
		{
			if (PartnerAccessSet.AccesPartner(AccessCategoriesType.RegisterClient))
			{
				PropertyBag["Tariffs"] = Tariff.FindAllSort();
				PropertyBag["Client"] = new Client();
				PropertyBag["Applying"] = "false";
				PropertyBag["Popolnen"] = false;
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}

		public void RegisterPartner()
		{
			if (PartnerAccessSet.AccesPartner(AccessCategoriesType.RegisterPartner))
			{
				PropertyBag["Rights"] = ActiveRecordBase<AccessCategories>.FindAll(DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("Code <> 16")));
				PropertyBag["Partner"] = new Partner();
				PropertyBag["Applying"] = "false";
				/*PropertyBag["Popolnen"] = false;*/
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}
	}

}