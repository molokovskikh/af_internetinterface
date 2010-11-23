using System;
using System.Linq;
using System.Collections.Generic;
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
	public class UserInfoController : SmartDispatcherController
	{
		private static bool _editFlag;
		//[AccessibleThrough(Verb.Get)]
		public void SearchUserInfo(uint clientCode, bool Editing)
		{
			var phisCl = PhisicalClients.Find(clientCode);
			PropertyBag["Client"] = phisCl;
			var clDate = RequestsConnection.FindAll(DetachedCriteria.For(typeof (RequestsConnection))
			                                           	.Add(Expression.Eq("ClientID", phisCl)));
			if (clDate.Length != 0)
			{
				PropertyBag["RegisntationDate"] = clDate[0].RegDate.ToString();
				var FindCloseDate = clDate.ToList().Find(t => t.CloseDemandDate.ToString() != "01.01.0001 0:00:00");
				if (FindCloseDate != null)
				{
					PropertyBag["CloseDate"] = FindCloseDate.CloseDemandDate.ToString();
				}
			}
			

			SendParam(clientCode);
			Flash["Editing"] = Editing;
			PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(new PhisicalClients());
			/*PropertyBag["EditFlag"] = _editFlag;
			_editFlag = false;*/
			//if (EditFlag) {EditInformation(); }
		}

		/*[AccessibleThrough(Verb.Post)]
		public void LoadEditMudule(uint ClientID)
		{
			//SearchUserInfo(ClientID, true);
			Flash["Editing"] = true;
			RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID + "&Editing=true");
		}*/

		public void ClientRegisteredInfo()
		{
			if (Flash["Client"] == null)
			{
				RedirectToUrl("../Register/RegisterClient.rails");
			}
			else
			{
				PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			}
		}

		public void PartnerRegisteredInfo(int hiddenPartnerId, string hiddenPass)
		{
			if (Flash["Partner"] == null)
			{
				RedirectToUrl("../Register/RegisterPartner.rails");
			}
			else
			{
				PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			}
		}

		public void PartnersPreview()
		{
			PropertyBag["Partners"] = Partner.FindAllSort();
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
		}

		[AccessibleThrough(Verb.Post)]
		public void EditInformation([DataBind("Client")]PhisicalClients client, uint ClientID, uint tariff)
		{
			var updateClient = PhisicalClients.Find(ClientID);
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
				PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(updateClient);
				var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
				var session = sessionHolder.CreateSession(typeof (PhisicalClients));
				session.Evict(updateClient);
				//Flash["Validate"] = true;
				RenderView("SearchUserInfo");
				Flash["Editing"] = true;
				SendParam(ClientID);
				//RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID + "&Editing=true");
			}
		}

		private void SendParam(uint ClientCode)
		{
			PropertyBag["ClientCode"] = ClientCode;
			PropertyBag["BalanceText"] = string.Empty;
			Flash["Tariffs"] = Tariff.FindAllSort();
			//Flash["Popolnen"] = false;
			//Flash["thisPay"] = new Payment();
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["ChangeBy"] = new ChangeBalaceProperties {ChangeType = TypeChangeBalance.OtherSumm};
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["Payments"] = Payment.FindAllByProperty("ClientId", PhisicalClients.Find(ClientCode));
		}

		[AccessibleThrough(Verb.Post)]
		public void ChangeBalance([DataBind("ChangedBy")]ChangeBalaceProperties changeProperties, uint clientId, string balanceText)
		{
			var clientToch = PhisicalClients.Find(clientId);
			string forChangeSumm = string.Empty;
			var thisPay = new Payment();
			PropertyBag["ChangeBalance"] = true;
			if (changeProperties.IsForTariff())
			{
				forChangeSumm = PhisicalClients.Find(clientId).Tariff.Price.ToString();
			}
			if (changeProperties.IsOtherSumm())
			{
				forChangeSumm = balanceText;
			}
			thisPay.Summ = forChangeSumm;
			thisPay.ManagerID = InithializeContent.partner;
			thisPay.ClientId = PhisicalClients.Find(clientId);
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
			RedirectToUrl(@"../UserInfo/SearchUserInfo.rails?ClientCode=" + clientId);
		}

		public void Test()
		{
		}

	}
}