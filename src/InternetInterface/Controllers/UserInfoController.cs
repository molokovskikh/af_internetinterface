using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InternetInterface.AllLogic;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate.Criterion;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class UserInfoController : SmartDispatcherController
	{
		public void SearchUserInfo(uint clientCode, bool Editing, bool EditingConnect)
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
			Flash["EditingConnect"] = EditingConnect;
			PropertyBag["VB"] = new ValidBuilderHelper<PhisicalClients>(new PhisicalClients());
			PropertyBag["ConnectInfo"] = phisCl.GetConnectInfo();
		}


		private void SendRequestEditParameter()
		{
			PropertyBag["labelColors"] = ColorWork.GetColorSet();
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
				ARSesssionHelper<Label>.QueryWithSession(session =>
				                                         	{
				                                         		var query =
				                                         			session.CreateSQLQuery(
				                                         				"update internet.Requests R set r.`Label` = 0 where r.`Label`= :LabelIndex ;")
				                                         				.AddEntity(
				                                         					typeof (Label));
				                                         		query.SetParameter("LabelIndex", deletelabelch);
				                                         		query.ExecuteUpdate();
				                                         		return new List<Label>();
				                                         	});
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
		public void RequestView([DataBind("LabelList")]List<uint> labelList, uint labelch,
			[DataBind("toClient")]List<object> toClient, [DataBind("SetlabelButton")]List<object> SetlabelButton)
		{
			if (SetlabelButton.Count != 0)
			{
				foreach (var label in labelList)
				{
					var request = Requests.Find(label);
					request.Label = Label.Find(labelch);
					request.UpdateAndFlush();
				}
				PropertyBag["Clients"] = Requests.FindAll();
			}
			if (toClient.Count != 0)
			{
				foreach (var label in labelList)
				{
					var request = Requests.Find(label);
					var fio = new string[3];
					request.ApplicantName.Split(' ').CopyTo(fio , 0);
					var newClient = new PhisicalClients
					                	{
					                		Surname = fio[0],
					                		Name = fio[1],
					                		Patronymic = fio[2],
											PhoneNumber = TelephoneParcer(request.ApplicantPhoneNumber),
					                		Tariff = request.Tariff,
					                		City = request.City,
											CaseHouse = request.CaseHouse,
											Floor = request.Floor,
											House = request.House,
											Street = request.Street,
											Apartment = request.Apartment,
											Entrance = request.Entrance
					                	};
					newClient.SaveAndFlush();
					request.DeleteAndFlush();
				}
				PropertyBag["Clients"] = Requests.FindAll();
			}
			SendRequestEditParameter();
		}

		private string TelephoneParcer(string number)
		{
			if (number.Length == 10)
			{
				return "8-" + number.Substring(0, 3) + "-" + number.Substring(3, 3) + "-" + number.Substring(6, 2) + "-" +
				       number.Substring(8, 2);
			}
			return number;
		}

		[AccessibleThrough(Verb.Post)]
		public void LoadEditMudule(uint ClientID)
		{
			Flash["Editing"] = true;
			RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + ClientID + "&Editing=true");
		}

		public void ClientRegisteredInfo()
		{}

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
				ARSesssionHelper<PhisicalClients>.QueryWithSession(session =>
				{
					session.Evict(updateClient);
					return new List<PhisicalClients>();
				});
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
				ARSesssionHelper<Payment>.QueryWithSession(session => { session.Evict(thisPay);
				                                                      	return new List<Payment>();
				});
			}
			RedirectToUrl(@"../UserInfo/SearchUserInfo.rails?ClientCode=" + clientId);
		}
	}
}