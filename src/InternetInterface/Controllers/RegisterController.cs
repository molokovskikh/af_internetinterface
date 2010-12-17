using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.Components.Validator;
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
		private static bool EditValidFlag;

		[AccessibleThrough(Verb.Post)]
		public void RegisterClient([DataBind("ChangedBy")]ChangeBalaceProperties changeProperties,
			[DataBind("client")]PhisicalClients user, string balanceText, uint tariff,uint status, [DataBind("ConnectSumm")]PaymentForConnect connectSumm)
		{
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["Statuss"] = Status.FindAllSort();
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
			user.Login = LoginCreatorHelper.GetUniqueEnLogin(user.Surname);
			if (PhisicalClients.RegistrLogicClient(user, tariff,status, Validator, InithializeContent.partner, connectSumm))
			{
				user.Tariff = Tariff.Find(tariff);
				user.HasRegistered = InithializeContent.partner;
				Flash["Password"] = Password;
				Flash["Client"] = user;
				Flash["ConnectSumm"] = connectSumm;
				RedirectToUrl("..//UserInfo/ClientRegisteredInfo.rails");
			}
			else
			{
				PropertyBag["Client"] = user;
				PropertyBag["BalanceText"] = balanceText;
				Flash["ConnectSumm"] = connectSumm;
				PropertyBag["Applying"] = "false";
				PropertyBag["ChStatus"] = status;
				PropertyBag["ChTariff"] = tariff;
				Validator.IsValid(connectSumm);
				connectSumm.SetValidationErrors(Validator.GetErrorSummary(connectSumm));
				user.SetValidationErrors(Validator.GetErrorSummary(user));
				
				PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(user);
				PropertyBag["NS"] = new ValidBuilderHelper<PaymentForConnect>(connectSumm);
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
				//PropertyBag["Applying"] = "false";
				PropertyBag["ChRights"] = rights;
				PropertyBag["Editing"] = false;
				PropertyBag["VB"] = new ValidBuilderHelper<Partner>(partner);
			}
		}

		public void RegisterClient()
		{
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["ChTariff"] = Tariff.FindFirst().Id;
			PropertyBag["ChStatus"] = Status.FindFirst().Id;
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["Client"] = new PhisicalClients();
			PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(new PhisicalClients());
			PropertyBag["NS"] = new ValidBuilderHelper<PaymentForConnect>(new PaymentForConnect());
			Flash["ConnectSumm"] = new PaymentForConnect();
			PropertyBag["Applying"] = "false";
			PropertyBag["ChangeBy"] = new ChangeBalaceProperties { ChangeType = TypeChangeBalance.ForTariff };
		}

		[AccessibleThrough(Verb.Post)]
		public void EditPartner([DataBind("Partner")]Partner partner, [DataBind("ForRight")]List<int> rights)
		{
			var PID = Partner.FindAllByProperty("Login", partner.Login)[0].Id;
			partner.Id = PID;
			if (rights.Count != 0)
			{
				if (Validator.IsValid(partner) || EditValidFlag)
				{
					var basePartner = Partner.FindAllByProperty("Login", partner.Login);
					if (basePartner.Length != 0)
					{
						//Далее написана несистемная фигня, проблема в привязке объекта вне хиберовской сесии к сесии
						//как реализовать пока не придумал, поработает пока что так, потом займусь.
						/*partner.Id = basePartner[0].Id;
						if (Validator.IsValid(partner))
							basePartner[0].Name = partner.Name;*/
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
							if (t == (int) AccessCategoriesType.RegisterPartner) continue;
							var forDel = PartnerAccessSet.FindAll(DetachedCriteria.For(typeof (PartnerAccessSet))
							                                      	.Add(Expression.Eq("PartnerId", partner))
							                                      	.Add(Expression.Eq("AccessCat", AccessCategories.Find(t))));
							foreach (var partnerAccessSet in forDel)
							{
								partnerAccessSet.DeleteAndFlush();
							}
						}

						if ((!rights.Contains((int) AccessCategoriesType.RegisterPartner))
						    && (ChRights.Contains((int) AccessCategoriesType.RegisterPartner)))
						{
							rights.Add((int) AccessCategoriesType.RegisterPartner);
						}

						AccessDependence.SetCrossAccess(ChRights, rights, partner);
						EditValidFlag = false;
						Flash["EditiongMessage"] = "Изменения внесены успешно";
					}
					else
					{
						Flash["EditiongMessage"] = "Был зарегистрирован новый партнер";
						Partner.RegistrLogicPartner(partner, rights, Validator);
					}

					RedirectToUrl("../Register/RegisterPartner?PartnerKey=" + PID);
				}
				else
				{
					partner.SetValidationErrors(Validator.GetErrorSummary(partner));
					var ve =  partner.GetValidationErrors();
					if (ve.ErrorsCount == 1)
						if (ve.ErrorMessages[0] == "Логин должен быть уникальный")
						{
							EditValidFlag = true;
							EditPartner(partner, rights);
						}
					RegisterPartnerSendParam((int) PID);
					RenderView("RegisterPartner");
					Flash["Partner"] = partner;
					/*if (ve.ErrorMessages.ToList().Contains("Логин должен быть уникальный"))
					{
						var veList = ve.ErrorMessages.ToList();
						veList.RemoveAt(veList.IndexOf("Логин должен быть уникальный"));
						var test = new ErrorSummary();
						
						partner.SetValidationErrors(veList.ToArray());
						/*var newErrors = new ErrorSummary[ve.ErrorMessages.Count()-1];
						foreach (var errorSummary in ve.ErrorMessages)
						{
							newErrors
						}*/
					//}
					PropertyBag["VB"] = new ValidBuilderHelper<Partner>(partner);
					/*Flash["Partner"] = partner;
					RedirectToUrl(string.Format("../Register/RegisterPartner?PartnerKey={0}&Errors=true", PID));*/
				}
			}
			else
			{
				RedirectToUrl("../Register/RegisterPartner?PartnerKey=" + PID);
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

		public void RegisterPartnerSendParam(int PartnerKey)
		{
			PropertyBag["Rights"] =
	ActiveRecordBase<AccessCategories>.FindAll(
		DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("ReduceName <> 'RP'")));
			//PropertyBag["Partner"] = partner;
			PropertyBag["ChRights"] = GetPartnerAccess(PartnerKey);
			PropertyBag["VB"] = new ValidBuilderHelper<Partner>(new Partner());
			PropertyBag["Applying"] = "false";
			PropertyBag["Editing"] = true;
		}

		public void RegisterPartner(int PartnerKey)
		{
			if (Partner.FindAll().Count(p => p.Id == PartnerKey) != 0)
			{
				RegisterPartnerSendParam(PartnerKey);
				PropertyBag["Partner"] = Partner.Find((uint)PartnerKey);
			}
			else
			{
				RedirectToUrl("../Register/RegisterPartner");
			}
		}

		public void RegisterPartner()
		{
			PropertyBag["Rights"] =
				ActiveRecordBase<AccessCategories>.FindAll(DetachedCriteria.For<AccessCategories>().Add(Expression.Sql("ReduceName <> 'RP'")));
			PropertyBag["Partner"] = new Partner();
			//PropertyBag["Applying"] = "false";
			PropertyBag["ChRights"] = new List<int>();
			PropertyBag["VB"] = new ValidBuilderHelper<Partner>(new Partner());
			PropertyBag["Editing"] = false;
		}
	}

}