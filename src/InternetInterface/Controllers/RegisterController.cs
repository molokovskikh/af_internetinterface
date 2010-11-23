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
using NHibernate.Tool.hbm2ddl;


namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class RegisterController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Post)]
		public void RegisterClient([DataBind("ChangedBy")]ChangeBalaceProperties changeProperties,
			[DataBind("client")]PhisicalClients user, string balanceText, uint tariff)
		{
			//PropertyBag["Popolnen"] = popolnenie;
			/*PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["PARTNERNAME"] = InithializeContent.partner.Name;*/
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
			user.Password = PhisicalClients.GeneratePassword();
			if (PhisicalClients.RegistrLogicClient(user, tariff, Validator, InithializeContent.partner))
			{
				/*PropertyBag["Applying"] = "true";
				PropertyBag["Client"] = new PhisicalClients();
				PropertyBag["BalanceText"] = string.Empty;
				PropertyBag["NewPass"] = user.Password;
				PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(new PhisicalClients());
				PropertyBag["ChangeBy"] = new ChangeBalaceProperties { ChangeType = TypeChangeBalance.ForTariff };*/
				user.Tariff = Tariff.Find(tariff);
				user.HasRegistered = InithializeContent.partner;
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
			/*PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["PARTNERNAME"] = InithializeContent.partner.Name;*/
			PropertyBag["Rights"] =
	ActiveRecordBase<AccessCategories>.FindAll(
		DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("ReduceName <> 'RP'")));
			/*if ((Pass != null) && (Pass.Length > 4))
			{*/
			if (Partner.RegistrLogicPartner(partner, rights, Validator))
			{
#if !DEBUG
				ActiveDirectoryHelper.CreateUserInAD(partner.Login, Pass);
#endif
				//PropertyBag["Applying"] = "true";
				Flash["Partner"] = partner;//new Partner();
				Flash["PartnerPass"] = Pass;
				/*PropertyBag["ChRights"] = new List<int>();
				PropertyBag["VB"] = new ValidBuilderHelper<Partner>(new Partner());*/
				RedirectToUrl("..//UserInfo/PartnerRegisteredInfo.rails");
			}
			else
			{
				partner.SetValidationErrors(Validator.GetErrorSummary(partner));
				PropertyBag["Partner"] = partner;
				PropertyBag["Applying"] = "false";
				PropertyBag["ChRights"] = rights;
				PropertyBag["VB"] = new ValidBuilderHelper<Partner>(partner);
				//NoValidPartner(partner, ForRight);
			}
			//}
			/*else
			{
				Validator.IsValid(partner);
				NoValidPartner(partner, ForRight);
				PropertyBag["PassError"] = "Пароль не может быть пустым, введите пароль (мин 5 знаков)";
			}*/
		}

		/*private void NoValidPartner(Partner partner, List<int> forRight)
		{
			partner.SetValidationErrors(Validator.GetErrorSummary(partner));
			PropertyBag["Partner"] = partner;
			PropertyBag["Applying"] = "false";
			PropertyBag["ChRights"] = forRight;
			PropertyBag["VB"] = new ValidBuilderHelper<Partner>(partner);
		}*/

		public void RegisterClient()
		{
			/*PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["PARTNERNAME"] = InithializeContent.partner.Name;*/
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["Client"] = new PhisicalClients();
			PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(new PhisicalClients());

			PropertyBag["Applying"] = "false";
			//PropertyBag["Popolnen"] = false;
			PropertyBag["ChangeBy"] = new ChangeBalaceProperties { ChangeType = TypeChangeBalance.ForTariff };
		}

		[AccessibleThrough(Verb.Post)]
		public void EditPartner([DataBind("Partner")]Partner partner, [DataBind("ForRight")]List<int> rights)
		{
			if (Validator.IsValid(partner))
			{
				var PID = Partner.FindAllByProperty("Login", partner.Login)[0].Id;
				//partner.Id = PID;
				var basePartner = Partner.FindAllByProperty("Login", partner.Login);
				//schema.Create(true, true);
				if (basePartner.Length != 0)
				{
					/*var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
					var session = sessionHolder.CreateSession(typeof(PhisicalClients));
					session.Update(partner);
					sessionHolder.ReleaseSession(session);*/

					//Далее написана несистемная фигня, проблема в привязке объекта вне хиберовской сесии к сесии
					//как реализовать пока не придумал, поработает пока что так, потом займусь.
					partner.Id = basePartner[0].Id;
					basePartner[0].Name = partner.Name;
					basePartner[0].TelNum = partner.TelNum;
					basePartner[0].Adress = partner.Adress;
					basePartner[0].Email = partner.Email;
					basePartner[0].UpdateAndFlush();
					//конец несистемной фигни
					//partner.UpdateAndFlush();
					var ChRights = GetPartnerAccess((int) PID);
					/*if (rights.Count >= ChRights.Count)
					{*/
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

					if ((!rights.Contains((int)AccessCategoriesType.CloseDemand))
						&& (ChRights.Contains((int)AccessCategoriesType.CloseDemand)))
					{
						var delBrig = Brigad.FindAll(DetachedCriteria.For(typeof (Brigad))
														.Add(Expression.Eq("PartnerID", partner)));
						foreach (var brigad in delBrig)
						{
							brigad.DeleteAndFlush();
						}
					}

					if ((rights.Contains((int)AccessCategoriesType.CloseDemand))
						&& (!ChRights.Contains((int)AccessCategoriesType.CloseDemand)))
					{
						var brigad = new Brigad
						             	{
						             		Adress = partner.Adress,
											BrigadCount = 1,
											Name = partner.Name,
											PartnerID = partner
						             	};
						brigad.SaveAndFlush();
					}

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

		private List<int> GetPartnerAccess(int Partner)
		{
			var RightArray = PartnerAccessSet.FindAll(DetachedCriteria.For(typeof(PartnerAccessSet))
																.CreateAlias("PartnerId", "P", JoinType.InnerJoin)
																.Add(Expression.Eq("P.Id", (uint)Partner)));
			var ChRights = new List<int>();
			foreach (var partnerAccessSet in RightArray)
			{
				ChRights.Add(partnerAccessSet.AccessCat.Id);
			}
			return ChRights;
		}

		public void RegisterPartner(int Partner)
		{
			/*PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["PARTNERNAME"] = InithializeContent.partner.Name;*/
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
			/*PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["PARTNERNAME"] = InithializeContent.partner.Name;*/
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