using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Criterion;
using NHibernate.SqlCommand;


namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class RegisterController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Post)]
		public void RegisterClient([DataBind("ChangedBy")]ChangeBalaceProperties changeProperties,
			[DataBind("client")]PhisicalClients user, string balanceText, uint tariff)
		{
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			if (changeProperties.IsForTariff())
			{
				user.Balance = Tariff.Find(tariff).Price.ToString();
			}
			if (changeProperties.IsOtherSumm())
			{
				user.Balance = balanceText;
			}
			var Password = PhisicalClients.GeneratePassword();
			user.Password = Password;
			if (PhisicalClients.RegistrLogicClient(user, tariff, Validator, InithializeContent.partner))
			{
				user.Tariff = Tariff.Find(tariff);
				user.HasRegistered = InithializeContent.partner;
				Flash["Password"] = Password;
				Flash["Client"] = user;
				RedirectToUrl("..//UserInfo/ClientRegisteredInfo.rails");
			}
			else
			{
				user.SetValidationErrors(Validator.GetErrorSummary(user));
				PropertyBag["Client"] = user;
				PropertyBag["BalanceText"] = balanceText;
				PropertyBag["Applying"] = "false";
				PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(user);
				PropertyBag["ChangeBy"] = changeProperties;
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void RegisterPartner([DataBind("Partner")]Partner partner, [DataBind("ForRight")]List<int> rights)
		{
			string Pass = Partner.GeneratePassword();
			PropertyBag["Rights"] =
	ActiveRecordBase<AccessCategories>.FindAll(
		DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("ReduceName <> 'RP'")));
			if (Partner.RegistrLogicPartner(partner, rights, Validator))
			{
#if !DEBUG
				ActiveDirectoryHelper.CreateUserInAD(partner.Login, Pass);
#endif
				Flash["Partner"] = partner;
				Flash["PartnerPass"] = Pass;
				RedirectToUrl("..//UserInfo/PartnerRegisteredInfo.rails");
			}
			else
			{
				partner.SetValidationErrors(Validator.GetErrorSummary(partner));
				PropertyBag["Partner"] = partner;
				PropertyBag["Applying"] = "false";
				PropertyBag["ChRights"] = rights;
				PropertyBag["VB"] = new ValidBuilderHelper<Partner>(partner);
			}
		}

		public void RegisterClient()
		{
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["Client"] = new PhisicalClients();
			PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(new PhisicalClients());

			PropertyBag["Applying"] = "false";
			PropertyBag["ChangeBy"] = new ChangeBalaceProperties { ChangeType = TypeChangeBalance.ForTariff };
		}

		[AccessibleThrough(Verb.Post)]
		public void EditPartner([DataBind("Partner")]Partner partner, [DataBind("ForRight")]List<int> rights)
		{
			if (Validator.IsValid(partner))
			{
				var PID = Partner.FindAllByProperty("Login", partner.Login)[0].Id;
				var basePartner = Partner.FindAllByProperty("Login", partner.Login);
				if (basePartner.Length != 0)
				{
					//Далее написана несистемная фигня, проблема в привязке объекта вне хиберовской сесии к сесии
					//как реализовать пока не придумал, поработает пока что так, потом займусь.
					partner.Id = basePartner[0].Id;
					basePartner[0].Name = partner.Name;
					basePartner[0].TelNum = partner.TelNum;
					basePartner[0].Adress = partner.Adress;
					basePartner[0].Email = partner.Email;
					basePartner[0].UpdateAndFlush();
					//конец несистемной фигни
					var ChRights = GetPartnerAccess((int) PID);
					foreach (var t in rights)
					{
						if (ChRights.Contains(t)) continue;
						var newRight = new PartnerAccessSet
						               	{
						               		AccessCat = AccessCategories.Find(t),
						               		PartnerId = partner
						               	};
						newRight.SaveAndFlush();
					}
					foreach (var t in ChRights)
					{
						if (rights.Contains(t)) continue;
						if (t == (int)AccessCategoriesType.RegisterPartner) continue;
						var forDel = PartnerAccessSet.FindAll(DetachedCriteria.For(typeof (PartnerAccessSet))
						                                      	.Add(Expression.Eq("PartnerId", partner))
						                                      	.Add(Expression.Eq("AccessCat", AccessCategories.Find(t))));
						foreach (var partnerAccessSet in forDel)
						{
							partnerAccessSet.DeleteAndFlush();
						}
					}

					if ((!rights.Contains((int)AccessCategoriesType.RegisterPartner))
						&& (ChRights.Contains((int)AccessCategoriesType.RegisterPartner)))
					{
						rights.Add((int)AccessCategoriesType.RegisterPartner);
					}

					AccessDependence.SetCrossAccess(ChRights, rights, partner);

					Flash["EditiongMessage"] = "Изменения внесены успешно";
				}
				else
				{
					Flash["EditiongMessage"] = "Был зарегистрирован новый партнер";
					Partner.RegistrLogicPartner(partner, rights, Validator);
				}
				RedirectToUrl("../Register/RegisterPartner?Partner=" + PID);
			}
		}

		/// <summary>
		/// Возвращает список прав партнера
		/// </summary>
		/// <param name="Partner"></param>
		/// <returns></returns>
		private List<int> GetPartnerAccess(int Partner)
		{
			var RightArray = PartnerAccessSet.FindAll(DetachedCriteria.For(typeof(PartnerAccessSet))
																.CreateAlias("PartnerId", "P", JoinType.InnerJoin)
																.Add(Expression.Eq("P.Id", (uint)Partner)));
			return RightArray.Select(partnerAccessSet => partnerAccessSet.AccessCat.Id).ToList();
		}

		public void RegisterPartner(int Partner)
		{
			PropertyBag["Rights"] =
				ActiveRecordBase<AccessCategories>.FindAll(
					DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("ReduceName <> 'RP'")));
			PropertyBag["Partner"] = Models.Partner.Find((uint)Partner);
			var ChRights = GetPartnerAccess(Partner);
			PropertyBag["ChRights"] = ChRights;
			PropertyBag["VB"] = new ValidBuilderHelper<Partner>(new Partner());
			PropertyBag["Applying"] = "false";
			PropertyBag["Editing"] = true;
		}

		public void RegisterPartner()
		{
			PropertyBag["Rights"] =
				ActiveRecordBase<AccessCategories>.FindAll(DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("ReduceName <> 'RP'")));
			PropertyBag["Partner"] = new Partner();
			PropertyBag["Applying"] = "false";
			PropertyBag["ChRights"] = new List<int>();
			PropertyBag["VB"] = new ValidBuilderHelper<Partner>(new Partner());
			PropertyBag["Editing"] = false;
		}
	}

}