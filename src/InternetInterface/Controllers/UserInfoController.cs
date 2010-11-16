﻿using System;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class UserInfoController : SmartDispatcherController
	{
		private static bool _editFlag;
		//[AccessibleThrough(Verb.Get)]
		public void SearchUserInfo(uint clientCode, bool Editing)
		{
			PropertyBag["Client"] = Client.Find(clientCode);
			SendParam(clientCode);
			Flash["Editing"] = Editing;
			/*PropertyBag["EditFlag"] = _editFlag;
			_editFlag = false;*/
			//if (EditFlag) {EditInformation(); }
		}

		[AccessibleThrough(Verb.Post)]
		public void LoadEditMudule(uint ClientID)
		{
			//SearchUserInfo(ClientID, true);
			Flash["Editing"] = true;
			RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID + "&Editing=true");
		}

		[AccessibleThrough(Verb.Post)]
		public void EditInformation([DataBind("Client")]Client client, uint ClientID, uint tariff)
		{
			var updateClient = Client.Find(ClientID);
			updateClient.Name = client.Name;
			updateClient.Surname = client.Surname;
			updateClient.Patronymic = client.Patronymic;
			updateClient.City = client.City;
			updateClient.AdressConnect = client.AdressConnect;
			updateClient.PassportSeries = client.PassportSeries;
			updateClient.PassportNumber = client.PassportNumber;
			updateClient.WhoGivePassport = client.WhoGivePassport;
			updateClient.RegistrationAdress = client.RegistrationAdress;
			updateClient.Tariff = Tariff.Find(tariff);
			if (Validator.IsValid(updateClient))
			{
				updateClient.UpdateAndFlush();
				PropertyBag["Editing"] = false;
				Flash["EditFlag"] = true;
				//_editFlag = true;
				RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID );
			}
			else
			{
				updateClient.SetValidationErrors(Validator.GetErrorSummary(updateClient));
				Flash["Client"] = updateClient;
				var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
				var session = sessionHolder.CreateSession(typeof (Client));
				session.Evict(updateClient);
				//Flash["Validate"] = true;
				RenderView("SearchUserInfo");	
				SendParam(ClientID);
				//RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID + "&Editing=true");
			}
		}

		private void SendParam(uint ClientCode)
		{
			PropertyBag["ClientCode"] = ClientCode;
			PropertyBag["BalanceText"] = string.Empty;
			Flash["Tariffs"] = Tariff.FindAllSort();
			Flash["Popolnen"] = false;
			//Flash["thisPay"] = new Payment();
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["ChangeBy"] = new ChangeBalaceProperties {ChangeType = TypeChangeBalance.OtherSumm};
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["Payments"] = Payment.FindAllByProperty("ClientId", Client.Find(ClientCode));
		}

		[AccessibleThrough(Verb.Post)]
		public void ChangeBalance([DataBind("ChangedBy")]ChangeBalaceProperties changeProperties, uint clientId, string balanceText)
		{
			var clientToch = Client.Find(clientId);
			string forChangeSumm = string.Empty;
			var thisPay = new Payment();
			PropertyBag["ChangeBalance"] = true;
			if (changeProperties.IsForTariff())
			{
				forChangeSumm = Client.Find(clientId).Tariff.Price.ToString();
			}
			if (changeProperties.IsOtherSumm())
			{
				forChangeSumm = balanceText;
			}
			thisPay.Summ = forChangeSumm;
			thisPay.ManagerID = InithializeContent.partner;
			thisPay.ClientId = Client.Find(clientId);
			thisPay.PaymentDate = DateTime.Now;
			if (Validator.IsValid(thisPay))
			{
				thisPay.SaveAndFlush();
				Flash["thisPay"] = new Payment();
				Flash["Applying"] = true;
				clientToch.Balance = Convert.ToString(Convert.ToDecimal(clientToch.Balance) + Convert.ToDecimal(forChangeSumm));
				clientToch.UpdateAndFlush();
			}
			else
			{
				thisPay.SetValidationErrors(Validator.GetErrorSummary(thisPay));
				Flash["thisPay"] = thisPay;
				var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
				var session = sessionHolder.CreateSession(typeof(Payment));
				session.Evict(thisPay);
			}
			/*try
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

					thisPay.ClientId = clientId;
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
			}*/
			RedirectToUrl(@"../UserInfo/SearchUserInfo.rails?ClientCode=" + clientId);
		}

		public void Test()
		{
		}

	}
}