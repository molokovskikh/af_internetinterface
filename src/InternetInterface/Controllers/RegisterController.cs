using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InternetInterface.AllLogic;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;


namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class RegisterController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Post)]
		public void RegisterClient([DataBind("ChangedBy")]ChangeBalaceProperties changeProperties,
			[DataBind("client")]PhysicalClients phisClient, string balanceText, uint tariff, uint status, uint BrigadForConnect//,
			/*[DataBind("ConnectSumm")]PaymentForConnect connectSumm*/
			 , [DataBind("ConnectInfo")]ConnectInfo ConnectInfo, bool VisibleRegisteredInfo,
			uint requestID)
		{
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			if (changeProperties.IsForTariff())
			{
				phisClient.Balance = Tariff.Find(tariff).Price;//.ToString();
			}
			if (changeProperties.IsOtherSumm())
			{
				phisClient.Balance = Convert.ToDecimal(balanceText);
			}
			var Password = CryptoPass.GeneratePassword();
			phisClient.Password = Password;
			if (!CategorieAccessSet.AccesPartner("SSI"))
			{
				phisClient.ConnectSum = 700;
				status = 1;
			}
			if (!CategorieAccessSet.AccesPartner("DHCP"))
			{
				ConnectInfo.Port = null;
			}
			var portException = Validation.ValidationConnectInfo(ConnectInfo);
			/*var unPort = false;
			var validPortSwitch = true;
			try
			{
				var Switch = NetworkSwitches.FindAll(DetachedCriteria.For(typeof(NetworkSwitches)).Add(Expression.Eq("Id", Convert.ToUInt32(ConnectInfo.Switch))));
				if ((Switch.Length != 0) && (ConnectInfo.Port != null))
				unPort = Point.isUnique(Switch.First(), Convert.ToInt32(ConnectInfo.Port));
				else
				{
					validPortSwitch = false;
				}
				if ((Convert.ToInt32(ConnectInfo.Port) > 48) || (Convert.ToInt32(ConnectInfo.Port) < 1))
					throw new BaseUsersException("Невалидное значения порта (1-48)");
				if (ConnectInfo.Switch == 0.ToString())
					throw new BaseUsersException("Не выбран свич");
			}
			catch (BaseUsersException ex)
			{
				if (!string.IsNullOrEmpty(ConnectInfo.Port))
				{
					PropertyBag["PortError"] = ex.Message;
					validPortSwitch = false;
				}
			}
			catch(Exception ex)
			{
				PropertyBag["PortError"] = "Ошибка ввода номера порта";
				validPortSwitch = false;
			}*/
			//var registerClient = PhisicalClients.RegistrLogicClient(phisClient, tariff, status, Validator, InithializeContent.partner, connectSumm);
			var registerClient = Validator.IsValid(phisClient)/* && Validator.IsValid(connectSumm)*/;
			/*if (registerClient && validPortSwitch) &&
				((!string.IsNullOrEmpty(ConnectInfo.Port) &&
					 (unPort))
					|| string.IsNullOrEmpty(ConnectInfo.Port)))*/
			if ((registerClient && String.IsNullOrEmpty(portException)) || (registerClient && string.IsNullOrEmpty(ConnectInfo.Port)))
			{
				PhysicalClients.RegistrLogicClient(phisClient, tariff, Validator);
				var client = new Clients
				{
					AutoUnblocked = true,
					RegDate = DateTime.Now,
					WhoRegistered = InithializeContent.partner,
					WhoRegisteredName = InithializeContent.partner.Name,
					Status = Status.Find((uint)StatusType.BlockedAndNoConnected),

					Name = string.Format("{0} {1} {2}", phisClient.Surname, phisClient.Name, phisClient.Patronymic),
					PhysicalClient = phisClient,
					Type = ClientType.Phisical,
					//FirstLease = true,
					BeginWork = null
					//SayDhcpIsNewClient = true
				};
				client.SaveAndFlush();
				var payment = new Payment
				              	{
									Agent = Agent.FindAllByProperty("Partner", InithializeContent.partner).First(),
									BillingAccount = true,
									Client = client,
									PaidOn = DateTime.Now,
									RecievedOn = DateTime.Now,
									Sum = phisClient.Balance
				              	};
				payment.SaveAndFlush();
				if (!string.IsNullOrEmpty(ConnectInfo.Port) && CategorieAccessSet.AccesPartner("DHCP"))
				{
					var newCEP = new ClientEndpoints
					             	{
					             		Client = client,
					             		Port = Convert.ToInt32(ConnectInfo.Port),
					             		Switch = NetworkSwitches.Find(Convert.ToUInt32(ConnectInfo.Switch)),
										PackageId = phisClient.Tariff.PackageId
					             	};
					newCEP.SaveAndFlush();
					if (BrigadForConnect != 0)
					{
						var brigad = Brigad.Find(BrigadForConnect);
						client.WhoConnected = brigad;
						client.WhoConnectedName = brigad.Name;
					}
					//phisClient.Connected = true;
					client.ConnectedDate = DateTime.Now;
					client.Status = Status.Find((uint)StatusType.BlockedAndConnected);
					client.UpdateAndFlush();
				}
				Flash["Password"] = Password;
				Flash["Client"] = phisClient;
				Flash["ConnectSumm"] = phisClient.ConnectSum;
				foreach (var requestse in Requests.FindAllByProperty("Id", requestID))
				{
					requestse.DeleteAndFlush();
				}
				if (InithializeContent.partner.Categorie.ReductionName == "Office")
					if (VisibleRegisteredInfo)
					RedirectToUrl("..//UserInfo/ClientRegisteredInfo.rails");
					else
					{
						RedirectToUrl("../UserInfo/SearchUserInfo.rails?ClientCode=" + client.Id);
					}
				if (InithializeContent.partner.Categorie.ReductionName == "Diller")
					RedirectToUrl("..//UserInfo/ClientRegisteredInfoFromDiller.rails");
			}
			else
			{
				PropertyBag["Client"] = phisClient;
				PropertyBag["BalanceText"] = balanceText;
				//connectSumm.ManagerID = InithializeContent.partner;
				//PropertyBag["ConnectSumm"] = connectSumm;
				PropertyBag["Applying"] = "false";
				/*if (!unPort && validPortSwitch)
					PropertyBag["PortError"] = @"Такая пара порт\свич уже существует";*/
				PropertyBag["PortError"] = portException;
				PropertyBag["ChStatus"] = status;
				PropertyBag["ChTariff"] = tariff;
				PropertyBag["ChBrigad"] = BrigadForConnect;
				/*Validator.IsValid(connectSumm);
				connectSumm.SetValidationErrors(Validator.GetErrorSummary(connectSumm));*/
				phisClient.SetValidationErrors(Validator.GetErrorSummary(phisClient));
				PropertyBag["ConnectInfo"] = ConnectInfo;
				PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(s => !string.IsNullOrEmpty(s.Name));
				PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClients>(phisClient);
				//PropertyBag["NS"] = new ValidBuilderHelper<PaymentForConnect>(connectSumm);
				PropertyBag["ChangeBy"] = changeProperties;
			}
		}

		public void RegisterLegalPerson()
		{
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(s => !string.IsNullOrEmpty(s.Name));
			PropertyBag["ChBrigad"] = Brigad.FindFirst().Id;
			PropertyBag["ConnectInfo"] = new ConnectInfo();
			PropertyBag["Editing"] = false;
			PropertyBag["LegalPerson"] = new LawyerPerson();
			PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(new LawyerPerson());
		}

		public void RegisterLegalPerson([DataBind("LegalPerson")]LawyerPerson person, int speed, [DataBind("ConnectInfo")]ConnectInfo info, uint brigadForConnect)
		{
			var connectErrors = Validation.ValidationConnectInfo(info);
			if (Validator.IsValid(person) && string.IsNullOrEmpty(connectErrors))
			{
				/*person.WhoRegistered = InithializeContent.partner;
				person.WhoRegisteredName = InithializeContent.partner.Name;
				person.RegDate = DateTime.Now;
				person.Status = Status.Find((uint) StatusType.BlockedAndNoConnected);*/
				person.Speed = PackageSpeed.Find(speed);
				person.SaveAndFlush();
				var client = new Clients
				             	{
									WhoRegistered = InithializeContent.partner,
									WhoRegisteredName = InithializeContent.partner.Name,
									RegDate = DateTime.Now,
									Status = Status.Find((uint) StatusType.BlockedAndNoConnected),

				             		LawyerPerson = person,
									Name = person.ShortName,
									Type = ClientType.Legal,
				             	};
				client.SaveAndFlush();
				if (!string.IsNullOrEmpty(info.Port))
				{
					new ClientEndpoints
						{	
							Client = client,
							PackageId = person.Speed.PackageId,
							Port = Int32.Parse(info.Port),
							Switch = NetworkSwitches.Find(info.Switch),
							
						}.SaveAndFlush();
					var brigad =  Brigad.Find(brigadForConnect);
					client.WhoConnected = brigad;
					client.WhoConnectedName = brigad.Name;
					client.Status = Status.Find((uint)StatusType.Worked);
					client.UpdateAndFlush();

				}
				/*PropertyBag["Editing"] = false;
				PropertyBag["LegalPerson"] = new LawyerPerson();
				PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(new LawyerPerson());*/
				RegisterLegalPerson();
				PropertyBag["EditiongMessage"] = "Клиент успешно загистрирвоан";
			}
			else
			{
				PropertyBag["Brigads"] = Brigad.FindAllSort();
				PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(s => !string.IsNullOrEmpty(s.Name));
				PropertyBag["ChBrigad"] = brigadForConnect;
				PropertyBag["ConnectInfo"] = info;
				PropertyBag["PortError"] = connectErrors;
				PropertyBag["Editing"] = false;
				PropertyBag["LegalPerson"] = person;
				person.SetValidationErrors(Validator.GetErrorSummary(person));
				PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(person);
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void RegisterPartner([DataBind("Partner")]Partner partner)
		{
			string Pass = CryptoPass.GeneratePassword();
			if (Partner.RegistrLogicPartner(partner, Validator))
			{
#if !DEBUG
				if (ActiveDirectoryHelper.FindDirectoryEntry(partner.Login) == null)
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
				PropertyBag["catType"] = partner.Categorie.Id;
				PropertyBag["Editing"] = false;
				PropertyBag["VB"] = new ValidBuilderHelper<Partner>(partner);
			}
		}

		public void RegisterClient()
		{
			SendRegisterParam();
			PropertyBag["Client"] = new PhysicalClients();
			PropertyBag["ChTariff"] = Tariff.FindFirst().Id;
		}

		public void RegisterClient(uint requestID)
		{
			var request = Requests.Find(requestID);
			var fio = new string[3];
			request.ApplicantName.Split(' ').CopyTo(fio, 0);
			var newPhisClient = new PhysicalClients
			{
				Surname = fio[0],
				Name = fio[1],
				Patronymic = fio[2],
				Tariff = request.Tariff,
				City = request.City,
				CaseHouse = request.CaseHouse,
				Floor = request.Floor,
				House = request.House,
				Street = request.Street,
				Apartment = request.Apartment,
				Entrance = request.Entrance,
				Email = request.ApplicantEmail,
				//RegDate = DateTime.Now
			};
			if (request.ApplicantPhoneNumber.Length == 10)
				newPhisClient.PhoneNumber = UsersParsers.MobileTelephoneParcer(request.ApplicantPhoneNumber);
			if (request.ApplicantPhoneNumber.Length == 5)
				newPhisClient.HomePhoneNumber = UsersParsers.HomeTelephoneParser(request.ApplicantPhoneNumber);
			PropertyBag["Client"] = newPhisClient;
			PropertyBag["ChTariff"] = request.Tariff.Id;
			PropertyBag["requestID"] = requestID;
			SendRegisterParam();
		}

		public void SendRegisterParam()
		{
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["ChStatus"] = Status.FindFirst().Id;
			PropertyBag["ChBrigad"] = 0;
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClients>(new PhysicalClients());
			//PropertyBag["NS"] = new ValidBuilderHelper<PaymentForConnect>(new PaymentForConnect());
			/*PropertyBag["ConnectSumm"] = new PaymentForConnect
			                             	{
			                             		Summ = 700.ToString(),
												ManagerID = InithializeContent.partner
			                             	};*/
			PropertyBag["Applying"] = "false";
			PropertyBag["ChangeBy"] = new ChangeBalaceProperties { ChangeType = TypeChangeBalance.OtherSumm };
			PropertyBag["BalanceText"] = 0;
			PropertyBag["ConnectInfo"] = new Clients().GetConnectInfo();
			PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(s => !string.IsNullOrEmpty(s.Name));
		}

		[AccessibleThrough(Verb.Post)]
		public void EditPartner([DataBind("Partner")]Partner partner, int PartnerKey)
		{
			var part = Partner.Find((uint) PartnerKey);
			var edit = false;
			if (Partner.Find((uint)PartnerKey).Login == partner.Login)
			{
				Validator.IsValid(partner);
				partner.SetValidationErrors(Validator.GetErrorSummary(partner));
				var ve = partner.GetValidationErrors();
				if (ve.ErrorsCount == 1)
				if (ve.ErrorMessages[0] == "Логин должен быть уникальный")
				{
					edit = true;
				}
			}
			if (Validator.IsValid(partner) || edit)
			{
				BindObjectInstance(part, ParamStore.Form, "Partner");
				part.UpdateAndFlush();
				Flash["EditiongMessage"] = "Изменения внесены успешно";
				RedirectToUrl("../Register/RegisterPartner?PartnerKey=" + part.Id + "&catType=" + part.Categorie.Id);
			}
			else
			{
				partner.SetValidationErrors(Validator.GetErrorSummary(partner));
				RegisterPartnerSendParam((int)partner.Id);
				RenderView("RegisterPartner");
				Flash["Partner"] = partner;
				Flash["catType"] = partner.Categorie.Id;
				PropertyBag["VB"] = new ValidBuilderHelper<Partner>(partner);
			}
		}

		/// <summary>
		/// Возвращает список прав партнера
		/// </summary>
		/// <param name="Partner"></param>
		/// <returns></returns>
		private List<int> GetPartnerAccess(int Partner)
		{
			var RightArray = CategorieAccessSet.FindAll(DetachedCriteria.For(typeof (CategorieAccessSet))
			                                            	.Add(Expression.Eq("Categorie",
			                                            	Models.Partner.Find((uint) Partner).Categorie)));
			return RightArray.Select(partnerAccessSet => partnerAccessSet.AccessCat.Id).ToList();
		}

		public void RegisterPartnerSendParam(int PartnerKey)
		{
			PropertyBag["VB"] = new ValidBuilderHelper<Partner>(new Partner());
			PropertyBag["Applying"] = "false";
			PropertyBag["Editing"] = true;
		}

		public void RegisterPartner(int PartnerKey, int catType)
		{
			if (Partner.FindAll().Count(p => p.Id == PartnerKey) != 0)
			{
				RegisterPartnerSendParam(PartnerKey);
				PropertyBag["Partner"] = Partner.Find((uint)PartnerKey);
				PropertyBag["catType"] = catType;
				PropertyBag["PartnerKey"] = PartnerKey;
			}
			else
			{
				RedirectToUrl("../Register/RegisterPartner");
			}
		}

		public void RegisterPartner(int catType)
		{
			PropertyBag["Partner"] = new Partner
			                         	{
			                         		Categorie =  new UserCategorie()
			                         	};
			PropertyBag["catType"] = catType;
			PropertyBag["VB"] = new ValidBuilderHelper<Partner>(new Partner());
			PropertyBag["Editing"] = false;
			PropertyBag["catType"] = catType;
		}
	}

}