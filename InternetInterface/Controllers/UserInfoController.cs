using System;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class UserInfoController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Get)]
		public void SearchUserInfo(uint ClientCode)
		{

			PropertyBag["Payments"] = Payment.FindAllByProperty("ClientID", ClientCode);
			PropertyBag["ClientName"] = Client.Find(ClientCode);
			PropertyBag["ClientCode"] = ClientCode;
			PropertyBag["BalanceText"] = string.Empty;
			if (PartnerAccessSet.AccesPartner(AccessCategoriesType.ChangeBalance))
			{
				PropertyBag["ChangeBalance"] = true;
				PropertyBag["ChangeBy"] = new ChangeBalaceProperties {ChangeType = TypeChangeBalance.OtherSumm};
			}
			else
			{
				PropertyBag["ChangeBalance"] = false;
			}

		}


		[AccessibleThrough(Verb.Post)]
		public void ChangeBalance([DataBind("ChangedBy")]ChangeBalaceProperties ChangeProperties, uint ClientID, string BalanceText)
		{
			if (PartnerAccessSet.AccesPartner(AccessCategoriesType.ChangeBalance))
			{
				var ClientTOCH = Client.Find(ClientID);
				decimal ForChangeSumm = 0;
				var thisPay = new Payment();
				PropertyBag["ChangeBalance"] = true;
				try
				{
					if (ChangeProperties.IsForTariff())
					{
						ForChangeSumm = ClientTOCH.Tariff.Price;
					}
					if (ChangeProperties.IsOtherSumm())
					{
						ForChangeSumm = Convert.ToDecimal(BalanceText);
					}
					if (ForChangeSumm != 0)
					{
						ClientTOCH.Balance += ForChangeSumm;

						thisPay.ClientID = ClientID;
						thisPay.PaymentDate = DateTime.Now;
						thisPay.Summ = ForChangeSumm;
						thisPay.ManagerID = InithializeContent.partner;
						thisPay.SaveAndFlush();
						ClientTOCH.UpdateAndFlush();
						Flash["Applying"] = true;
					}
				}
				catch (Exception)
				{
					Flash["Applying"] = false;
				}
				RedirectToUrl(@"../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID);
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}

		public void Test()
		{
		}

	}
}