using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
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
			user.Password = PhisicalClients.GeneratePassword();
			if (PhisicalClients.RegistrLogicClient(user, tariff, Validator,
										  Partner.FindAllByProperty("Login", Session["Login"].ToString())[0]))
			{
				PropertyBag["Applying"] = "true";
				PropertyBag["Client"] = new PhisicalClients();
				PropertyBag["BalanceText"] = string.Empty;
				PropertyBag["NewPass"] = user.Password;
				PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(new PhisicalClients());
				PropertyBag["ChangeBy"] = new ChangeBalaceProperties { ChangeType = TypeChangeBalance.ForTariff };
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
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
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
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
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
				partner.Id = PID;
				//partner.UpdateAndFlush();
				var ChRights = GetPartnerAccess((int) partner.Id);
				/*if (rights.Count >= ChRights.Count)
				{*/
				for (int i = 0; i < rights.Count; i++)
				{
					if (!ChRights.Contains(rights[i]))
					{
						var newRight = new PartnerAccessSet
						               	{
						               		AccessCat = AccessCategories.Find(rights[i]),
						               		PartnerId = partner
						               	};
						newRight.SaveAndFlush();
					}
				}
				for (int i = 0; i < ChRights.Count; i++)
				{
					if (!rights.Contains(ChRights[i]))
					{
						if (ChRights[i] != 5)
						{
							var forDel = PartnerAccessSet.FindAll(DetachedCriteria.For(typeof (PartnerAccessSet))
							                                      	.Add(Expression.Eq("PartnerId", partner))
							                                      	.Add(Expression.Eq("AccessCat", AccessCategories.Find(ChRights[i]))));
							forDel[0].DeleteAndFlush();
						}
					}
				}

				if ((!rights.Contains(4)) && (ChRights.Contains(4)))
				{
					var delBrig = Brigad.FindAll(DetachedCriteria.For(typeof (Brigad))
					                             	.Add(Expression.Eq("PartnerID", partner)));
					delBrig[0].DeleteAndFlush();
				}
				RedirectToUrl("../Register/RegisterPartner?Partner="+partner.Id);
				/*}
				else
				{
					for (int i = 0; i < ChRights.Count; i++)
					{
						if (!rights.Contains(ChRights[i]))
						{
							var forDel = PartnerAccessSet.FindAll(DetachedCriteria.For(typeof(PartnerAccessSet))
																	.Add(Expression.Eq("PartnerId", partner))
																	.Add(Expression.Eq("AccessCat", AccessCategories.Find(ChRights[i]))));
							forDel[0].DeleteAndFlush();
						}
					}
				}*/
			}
		}

		private void CreateRight(Partner partner, int right)
		{

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
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
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
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["Rights"] =
				ActiveRecordBase<AccessCategories>.FindAll(DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("ReduceName <> 'RP'")));
			PropertyBag["Partner"] = new Partner();
			PropertyBag["Applying"] = "false";
			PropertyBag["ChRights"] = new List<int>();
			PropertyBag["VB"] = new ValidBuilderHelper<Partner>(new Partner());
		}
	}

}