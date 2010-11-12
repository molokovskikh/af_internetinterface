using System;
using Castle.MonoRail.Framework;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	public class UserInfoController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Get)]
		public void SearchUserInfo(uint ClientCode)
		{
			var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if (MapPartner.Length != 0)
			{
				PropertyBag["Payments"] = Payment.FindAllByProperty("ClientID", ClientCode);
				PropertyBag["ClientName"] = Client.Find(ClientCode);
				PropertyBag["BalanceText"] = string.Empty;
				PropertyBag["ClientCode"] = ClientCode;
				PropertyBag["ChangeBy"] = new ChangeBalaceProperties {ChangeType = TypeChangeBalance.OtherSumm};
			}
			else
			{
				RedirectToUrl(@"..\\Errors\AccessDin.aspx");
			}
		}


		[AccessibleThrough(Verb.Post)]
		public void ChangeBalance([DataBind("ChangedBy")]ChangeBalaceProperties ChangeProperties, uint ClientID, string BalanceText)
		{
			var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if (MapPartner.Length != 0)
			{
				var ClientTOCH = Client.Find(ClientID);
				decimal ForChangeSumm = 0;
				var thisPay = new Payment();
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
						thisPay.ManagerID = MapPartner[0];
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