﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate.Criterion;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class UserInfoController : SmartDispatcherController
	{
		//private static bool _editFlag;
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

		private List<string> GetColorSet()
		{
			var colors = new List<string>();
			for (int i = 0; i < 256; i = i + 51)
			{
				var ival = i.ToString("X");
				if (ival.Length < 2)
				{
					ival = "0" + ival;
				}
				for (int j = 0; j < 256; j = j + 51)
				{
					var jval = j.ToString("X");
					if (jval.Length < 2)
					{
						jval = "0" + jval;
					}
					for (int k = 0; k < 256; k = k + 51)
					{
						var kval = k.ToString("X");
						if (kval.Length < 2)
						{
							kval = "0" + kval;
						}
						colors.Add('#' + ival + jval + kval);
					}
				}
			}
			return colors;
		}

		private void SendRequestEditParameter()
		{
			PropertyBag["labelColors"] = GetColorSet();
			PropertyBag["LabelName"] = string.Empty;
			PropertyBag["Labels"] = Label.FindAll();
		}

		public void EditLabel(uint deletelabelch, string LabelName, string labelcolor)
		{
			var labelForEdit = Label.Find(deletelabelch);
			if (labelForEdit != null)
			{
				if (LabelName != null)
					labelForEdit.Name = LabelName;
				if (labelcolor != "#000000")
				{
					labelForEdit.Color = labelcolor;
				}
				labelForEdit.UpdateAndFlush();
			}
			RedirectToUrl("../UserInfo/RequestView.rails");
		}

		public void DeleteLabel(uint deletelabelch)
		{
			var labelForDel = Label.Find(deletelabelch);
			if (labelForDel != null)
			{
				labelForDel.DeleteAndFlush();
				//File.Delete(AppDomain.CurrentDomain.BaseDirectory + "\\images\\Label" + deletelabelch + ".jpg");
				var session = HiberSession<Label>.GetHiberSission();
				var query =
					session.CreateSQLQuery("update internet.Requests R set r.`Label`=0 where r.`Label`= :LabelIndex ;").AddEntity(
						typeof (Label));
				query.SetParameter("LabelIndex", deletelabelch);
				query.ExecuteUpdate();
			}
			RedirectToUrl("../UserInfo/RequestView.rails");
		}

		public void RequestView()
		{
			PropertyBag["Clients"] = Requests.FindAll();
			SendRequestEditParameter();
		}

		/// <summary>
		/// Создать новую метку
		/// </summary>
		/// <param name="LabelName"></param>
		/// <param name="labelcolor"></param>
		public void RequestView(string LabelName, string labelcolor)
		{
			var newlab = new Label
			             	{
								Color = labelcolor,
								Name = LabelName
			             	};
			newlab.SaveAndFlush();
			RequestView();
		}

		/// <summary>
		/// Фильтр по меткам
		/// </summary>
		/// <param name="labelId"></param>
		public void RequestView(uint labelId)
		{
			PropertyBag["Clients"] = Requests.FindAll(DetachedCriteria.For(typeof(Requests))
				.Add(Expression.Eq("Label", Label.Find(labelId))));
			SendRequestEditParameter();
		}

		/// <summary>
		/// Устанавливает метки на клиентов
		/// </summary>
		/// <param name="labelList"></param>
		/// <param name="labelch"></param>
		[AccessibleThrough(Verb.Post)]
		public void RequestView([DataBind("LabelList")]List<uint> labelList, uint labelch)
		{
			foreach (var label in labelList)
			{
				var request = Requests.Find(label);
				request.Label = Label.Find(labelch);	
				request.UpdateAndFlush();
			}
			PropertyBag["Clients"] = Requests.FindAll();
			SendRequestEditParameter();
		}

		[AccessibleThrough(Verb.Post)]
		public void LoadEditMudule(uint ClientID)
		{
			//SearchUserInfo(ClientID, true);
			Flash["Editing"] = true;
			RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID + "&Editing=true");
		}

		public void ClientRegisteredInfo()
		{
			if (Flash["Client"] == null)
			{
				//RedirectToUrl("../Register/RegisterClient.rails");
			}
		}

		public void PartnerRegisteredInfo(int hiddenPartnerId, string hiddenPass)
		{
			if (Flash["Partner"] == null)
			{
				RedirectToUrl("../Register/RegisterPartner.rails");
			}
		}

		public void PartnersPreview()
		{
			PropertyBag["Partners"] = Partner.FindAllSort();
		}

		[AccessibleThrough(Verb.Post)]
		public void EditInformation([DataBind("Client")]PhisicalClients client, uint ClientID, uint tariff, uint status)
		{
			var updateClient = PhisicalClients.Find(ClientID);
			BindObjectInstance(updateClient, ParamStore.Form, "Client");
			updateClient.Tariff = Tariff.Find(tariff);
			updateClient.Status = Status.Find(status);

			if (Validator.IsValid(updateClient))
			{
				updateClient.UpdateAndFlush();
				PropertyBag["Editing"] = false;
				Flash["EditFlag"] = "Данные изменены";
				RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID );
			}
			else
			{
				updateClient.SetValidationErrors(Validator.GetErrorSummary(updateClient));
				PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(updateClient);
				var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
				var session = sessionHolder.CreateSession(typeof (PhisicalClients));
				session.Evict(updateClient);
				RenderView("SearchUserInfo");
				Flash["Editing"] = true;
				Flash["Client"] = updateClient;
				Flash["ChTariff"] = Tariff.Find(tariff).Id;
				Flash["ChStatus"] = Tariff.Find(status).Id;
				SendParam(ClientID);
			}
		}

		private void SendParam(uint ClientCode)
		{
			PropertyBag["ClientCode"] = ClientCode;
			PropertyBag["BalanceText"] = string.Empty;
			Flash["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["ChTariff"] = Tariff.FindFirst().Id;
			PropertyBag["ChStatus"] = Status.FindFirst().Id;
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["ChangeBy"] = new ChangeBalaceProperties {ChangeType = TypeChangeBalance.OtherSumm};
			PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
			PropertyBag["Payments"] = Payment.FindAllByProperty("Client", PhisicalClients.Find(ClientCode));
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
			thisPay.Sum = forChangeSumm;
			thisPay.Agent = Agent.FindAll(DetachedCriteria.For(typeof(Agent)).Add(Expression.Eq("Partner", InithializeContent.partner)))[0];
			thisPay.Client = PhisicalClients.Find(clientId);
			thisPay.RecievedOn = DateTime.Now;
			thisPay.PaidOn = DateTime.Now;
			if (Validator.IsValid(thisPay))
			{
				thisPay.SaveAndFlush();
				Flash["thisPay"] = new Payment();
				Flash["Applying"] = "Баланс пополнен";
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
	}
}