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
		public void RegisterClient([DataBind("ChangedBy")]ChangeBalaceProperties changeProperties,
			[DataBind("client")]Client user, string balanceText, uint tariff)
		{
			//PropertyBag["Popolnen"] = popolnenie;
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			//string semdBalanceText = string.Empty;
			if (changeProperties.IsForTariff())
			{
				user.Balance = Tariff.Find(tariff).Price.ToString();
			}
			if (changeProperties.IsOtherSumm())
			{
				user.Balance = balanceText;
			}
			if (Client.RegistrLogicClient(user, tariff, Validator,
										  Partner.FindAllByProperty("Login", Session["Login"].ToString())[0]))
			{
				PropertyBag["Applying"] = "true";
				PropertyBag["Client"] = new Client();
				PropertyBag["BalanceText"] = string.Empty;
				PropertyBag["ChangeBy"] = new ChangeBalaceProperties { ChangeType = TypeChangeBalance.ForTariff };
			}
			else
			{
				user.SetValidationErrors(Validator.GetErrorSummary(user));
				PropertyBag["Client"] = user;
				PropertyBag["BalanceText"] = balanceText;
				PropertyBag["Applying"] = "false";
				PropertyBag["ChangeBy"] = changeProperties;
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void RegisterPartner([DataBind("Partner")]Partner partner, [DataBind("ForRight")]List<int> rights, [DataBind("ForRight")]List<int> ForRight)
		{
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["Rights"] =
	ActiveRecordBase<AccessCategories>.FindAll(
		DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("ReduceName <> 'RP'")));
			if (Partner.RegistrLogicPartner(partner, rights, Validator))
			{
				PropertyBag["Applying"] = "true";
				PropertyBag["Partner"] = new Partner();
				PropertyBag["ChRights"] = new List<int>();
			}
			else
			{
				partner.SetValidationErrors(Validator.GetErrorSummary(partner));
				PropertyBag["Partner"] = partner;
				PropertyBag["Applying"] = "false";
				PropertyBag["ChRights"] = ForRight;
			}
		}

		public void RegisterClient()
		{
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["Client"] = new Client();
			PropertyBag["Applying"] = "false";
			//PropertyBag["Popolnen"] = false;
			PropertyBag["ChangeBy"] = new ChangeBalaceProperties { ChangeType = TypeChangeBalance.ForTariff };
		}

		public void RegisterPartner()
		{
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["Rights"] =
				ActiveRecordBase<AccessCategories>.FindAll(DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("ReduceName <> 'RP'")));
			PropertyBag["Partner"] = new Partner();
			PropertyBag["Applying"] = "false";
			PropertyBag["ChRights"] = new List<int>();
		}
	}

}