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
		public void SearchUserInfo(uint clientCode)
		{
			PropertyBag["Payments"] = Payment.FindAllByProperty("ClientID", clientCode);
			PropertyBag["ClientName"] = Client.Find(clientCode);
			PropertyBag["ClientCode"] = clientCode;
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["ChangeBy"] = new ChangeBalaceProperties {ChangeType = TypeChangeBalance.OtherSumm};
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
		}


		[AccessibleThrough(Verb.Post)]
		public void ChangeBalance([DataBind("ChangedBy")]ChangeBalaceProperties changeProperties, uint clientId, string balanceText)
		{
			var clientToch = Client.Find(clientId);
			decimal forChangeSumm = 0;
			var thisPay = new Payment();
			PropertyBag["ChangeBalance"] = true;
			try
			{
				if (changeProperties.IsForTariff())
				{
					forChangeSumm = clientToch.Tariff.Price;
				}
				if (changeProperties.IsOtherSumm())
				{
					forChangeSumm = Convert.ToDecimal(balanceText);
				}
				if (forChangeSumm != 0)
				{
					clientToch.Balance += forChangeSumm;

					thisPay.ClientID = clientId;
					thisPay.PaymentDate = DateTime.Now;
					thisPay.Summ = forChangeSumm;
					thisPay.ManagerID = InithializeContent.partner;
					thisPay.SaveAndFlush();
					clientToch.UpdateAndFlush();
					Flash["Applying"] = true;
				}
			}
			catch (Exception)
			{
				Flash["Applying"] = false;
			}
			RedirectToUrl(@"../UserInfo/SearchUserInfo.rails?ClientCode=" + clientId);
		}

		public void Test()
		{
		}

	}
}