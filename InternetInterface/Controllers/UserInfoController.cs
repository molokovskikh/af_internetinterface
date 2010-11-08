using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using NHibernate;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	public class UserInfoController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Get)]
		public void SearchUserInfo(uint ClientCode, bool Balance_TP, bool Other_Balance)
		{

			var Payments = Payment.FindAllByProperty("ClientID", ClientCode);
			var ClientName = Client.Find(ClientCode);
			PropertyBag["Payments"] = Payments;
			var sClientName = new List<string>();
			for (int i=0; i<Payments.Length; i++)
			{
				sClientName.Add(ClientName.Surname + " " + ClientName.Name + " " + " " + ClientName.Patronymic);
			}
			PropertyBag["ClientName"] = sClientName;
			PropertyBag["SClients"] = ClientName;
			PropertyBag["ClientTariffName"] = ClientName.Tariff.Name;
			PropertyBag["WhoRegister"] = ClientName.HasRegistered.Name;
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["ClientCode"] = ClientCode;
			var ChangeProperties = new ChangeBalaceProperties { ChangeType = TypeChangeBalance.ForTariff };
			PropertyBag["ChangeBy"] = ChangeProperties;
		}


		[AccessibleThrough(Verb.Post)]
		public void ChangeBalance([DataBind("ChangedBy")]ChangeBalaceProperties ChangeProperties, uint ClientID, string BalanceText)
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
					ClientTOCH.Balance += ForChangeSumm;
					
					thisPay.ClientID = ClientID;
					thisPay.PaymentDate = DateTime.Now;
					thisPay.Summ = ForChangeSumm;
					thisPay.SaveAndFlush();
					ClientTOCH.UpdateAndFlush();
					Flash["Applying"] = true;
				}
				catch (Exception)
				{
					Flash["Applying"] = false;
				} 
			RedirectToUrl(@"../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID);
		}
		public void Test()
		{
		}

	}
}