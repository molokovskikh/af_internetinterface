using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.MonoRail.Framework;
using Common.Tools;
using InternetInterface.AllLogic;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using IFilter = Castle.MonoRail.Framework.IFilter;


namespace InternetInterface.Controllers
{
	//[Layout("Main")]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class RegisterController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Post)]
		public void RegisterClient([DataBind("ChangedBy")]ChangeBalaceProperties changeProperties,
			[DataBind("client")]PhysicalClients phisClient, string balanceText, uint tariff, uint status, uint BrigadForConnect
			 , [DataBind("ConnectInfo")]ConnectInfo ConnectInfo, bool VisibleRegisteredInfo, uint house_id,
			uint requestID)
		{
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			if (changeProperties.IsForTariff())
			{
				phisClient.Balance = Tariff.Find(tariff).Price;
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

			var registerClient = Validator.IsValid(phisClient);

			if ((registerClient && string.IsNullOrEmpty(portException)) ||
				(registerClient && string.IsNullOrEmpty(ConnectInfo.Port)))
			{
				DbLogHelper.SetupParametersForTriggerLogging();

				PhysicalClients.RegistrLogicClient(phisClient, tariff, house_id, Validator);
				var client = new Client {
											 AutoUnblocked = true,
											 RegDate = DateTime.Now,
											 WhoRegistered = InitializeContent.partner,
											 WhoRegisteredName = InitializeContent.partner.Name,
											 Status = Status.Find((uint) StatusType.BlockedAndNoConnected),

											 Name =
												 string.Format("{0} {1} {2}", phisClient.Surname, phisClient.Name,
															   phisClient.Patronymic),
											 PhysicalClient = phisClient,
											 Type = ClientType.Phisical,
											 BeginWork = null
										 };
				client.SaveAndFlush();
				var payment = new Payment {
											  Agent =
												  Agent.FindAllByProperty("Partner", InitializeContent.partner).First(),
											  BillingAccount = true,
											  Client = client,
											  PaidOn = DateTime.Now,
											  RecievedOn = DateTime.Now,
											  Sum = phisClient.Balance
										  };
				payment.SaveAndFlush();
				var apartmentForClient =
					Apartment.Queryable.Where(a => a.House == phisClient.HouseObj && a.Number == phisClient.Apartment).
						FirstOrDefault();
				if (apartmentForClient != null)
					apartmentForClient.Delete();
				if (!string.IsNullOrEmpty(ConnectInfo.Port) && CategorieAccessSet.AccesPartner("DHCP"))
				{
					var newCEP = new ClientEndpoints {
														 Client = client,
														 Port = Convert.ToInt32(ConnectInfo.Port),
														 Switch =
															 NetworkSwitches.Find(Convert.ToUInt32(ConnectInfo.Switch)),
														 PackageId = phisClient.Tariff.PackageId
													 };
					newCEP.SaveAndFlush();
					if (BrigadForConnect != 0)
					{
						var brigad = Brigad.Find(BrigadForConnect);
						client.WhoConnected = brigad;
						client.WhoConnectedName = brigad.Name;
					}
					client.ConnectedDate = DateTime.Now;
					client.Status = Status.Find((uint) StatusType.BlockedAndConnected);
					client.UpdateAndFlush();
					foreach (var requestse in Requests.FindAllByProperty("Id", requestID))
					{
						if (requestse.Registrator != null)
						{
							PaymentsForAgent.CreatePayment(AgentActions.ConnectClient, string.Format("Зачисление за подключенного клиента #{0}", client.Id),
							                               requestse.Registrator);
							PaymentsForAgent.CreatePayment(requestse.Registrator,
							                               string.Format("Зачисление бонусов по заявке #{0} за поключенного клиента #{1}",
							                                             requestse.Id, client.Id), requestse.VirtualBonus);
							//PaymentsForAgent.CreatePayment(requestse.Registrator, "Зачисление штрафов", -requestse.VirtualWriteOff);
							requestse.PaidBonus = true;
							requestse.Update();
							//requestse.SetRequestBoduses();
						}
					}
				}

				Flash["WhoConnected"] = client.WhoConnected;
				Flash["Password"] = Password;
				Flash["Client"] = phisClient;
				Flash["AccountNumber"] = client.Id.ToString("00000");
				Flash["ConnectSumm"] = phisClient.ConnectSum;
				Flash["ConnectInfo"] = client.GetConnectInfo();
				foreach (var requestse in Requests.FindAllByProperty("Id", requestID))
				{
					if (requestse.Registrator != null)
					{
						phisClient.Request = requestse;
						phisClient.Update();
						PaymentsForAgent.CreatePayment(AgentActions.CreateClient, string.Format("Зачисление за зарегистрированного клиента #{0}", client.Id), requestse.Registrator);
					}
					if (requestse.Label != null && requestse.Label.ShortComment == "Refused" && requestse.Registrator != null)
					{
						PaymentsForAgent.CreatePayment(requestse.Registrator,
						                               string.Format("Снятие штрафа за закрытие заявки #{0}", requestse.Id),
						                               -AgentTariff.GetPriceForAction(AgentActions.DeleteRequest));
					}
					requestse.Label = Label.Queryable.Where(l => l.ShortComment == "Registered").FirstOrDefault();
					requestse.Update();
				}
				if (InitializeContent.partner.Categorie.ReductionName == "Office")
					if (VisibleRegisteredInfo)
						RedirectToUrl("..//UserInfo/ClientRegisteredInfo.rails");
					else
					{
						RedirectToUrl("../UserInfo/SearchUserInfo.rails?filter.ClientCode=" + client.Id);
					}
				if (InitializeContent.partner.Categorie.ReductionName == "Diller")
					RedirectToUrl("..//UserInfo/ClientRegisteredInfoFromDiller.rails");
			}
			else
			{
				PropertyBag["Client"] = phisClient;
				PropertyBag["BalanceText"] = balanceText;
				PropertyBag["ChHouse"] = house_id;
				PropertyBag["Houses"] = House.FindAll().OrderBy(h => h.Street);
				PropertyBag["Applying"] = "false";
				PropertyBag["PortError"] = portException;
				PropertyBag["ChStatus"] = status;
				PropertyBag["ChTariff"] = tariff;
				PropertyBag["ChBrigad"] = BrigadForConnect;
				phisClient.SetValidationErrors(Validator.GetErrorSummary(phisClient));
				if (!string.IsNullOrEmpty(portException))
					ConnectInfo.Port = string.Empty;
				PropertyBag["ConnectInfo"] = ConnectInfo;
				PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(s => !string.IsNullOrEmpty(s.Name));
				PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClients>(phisClient);
				PropertyBag["ChangeBy"] = changeProperties;
			}
		}


		public void RegisterLegalPerson()
		{
			PropertyBag["ClientCode"] = 0;
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
				DbLogHelper.SetupParametersForTriggerLogging();
				person.Recipient = Recipient.Queryable.Where(r => r.INN == "3666152146").FirstOrDefault();
				person.SaveAndFlush();
				var client = new Client
								{
									WhoRegistered = InitializeContent.partner,
									WhoRegisteredName = InitializeContent.partner.Name,
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
							Port = Int32.Parse(info.Port),
							Switch = NetworkSwitches.Find(info.Switch),
							
						}.SaveAndFlush();
					var brigad =  Brigad.Find(brigadForConnect);
					client.WhoConnected = brigad;
					client.WhoConnectedName = brigad.Name;
					client.Status = Status.Find((uint)StatusType.Worked);
					client.UpdateAndFlush();

				}
				RegisterLegalPerson();
				PropertyBag["EditiongMessage"] = "Клиент успешно загистрирвоан";
				RedirectToUrl("../UserInfo/LawyerPersonInfo.rails?filter.ClientCode=" + client.Id);
			}
			else
			{
				PropertyBag["ClientCode"] = 0;
				PropertyBag["Brigads"] = Brigad.FindAllSort();
				PropertyBag["Switches"] = NetworkSwitches.FindAllSort().Where(s => !string.IsNullOrEmpty(s.Name));
				PropertyBag["ChBrigad"] = brigadForConnect;
				if (!string.IsNullOrEmpty(connectErrors))
					info.Port = string.Empty;
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
			PropertyBag["ChHouse"] = 0;
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
			};
			if (request.ApplicantPhoneNumber.Length == 15)
				newPhisClient.PhoneNumber = UsersParsers.MobileTelephoneParcer(request.ApplicantPhoneNumber);
			if (request.ApplicantPhoneNumber.Length == 5)
				newPhisClient.HomePhoneNumber = UsersParsers.HomeTelephoneParser(request.ApplicantPhoneNumber);
			PropertyBag["Client"] = newPhisClient;
			PropertyBag["ChTariff"] = request.Tariff.Id;
			PropertyBag["requestID"] = requestID;
			if (newPhisClient.House != null)
			{
				var houses =
					House.Queryable.Where(
						h =>
						h.Street == newPhisClient.Street &&
						h.Number == newPhisClient.House &&
						h.Case == newPhisClient.CaseHouse).ToList();
				if (houses.Count != 0)
					PropertyBag["ChHouse"] = houses.First().Id;
				else {
					PropertyBag["ChHouse"] = 0;
					PropertyBag["Message"] = Message.Error("Не удалось сопоставить адрес из заявки ! Будте внимательны при заполнении адреса клиента !");
				}
			}
			else
			{
				PropertyBag["ChHouse"] = 0;
			}
			SendRegisterParam();
		}

		[return : JSONReturnBinder]
		public object RegisterHouse()
		{
			var street = Request.Form["Street"];
			var number = Request.Form["Number"];
			var _case = Request.Form["Case"];
			int res;
			var house = new House();
			var errors = string.Empty;
			if (!Int32.TryParse(number, out res))
				errors += "Неправильно введен номер дома" + res;
			if (string.IsNullOrEmpty(errors))
			{
				house = new House {Street = street, Number = Int32.Parse(number)};
				if (!string.IsNullOrEmpty(_case))
					house.Case = _case;
				house.Save();
			}
			return new { Name = string.Format("{0} {1} {2}", street, number, _case), Id = house.Id};
		}

		public void SendRegisterParam()
		{
			PropertyBag["Houses"] = House.AllSort;
			PropertyBag["BalanceText"] = string.Empty;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["ChStatus"] = Status.FindFirst().Id;
			PropertyBag["ChBrigad"] = 0;
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClients>(new PhysicalClients());

			PropertyBag["Applying"] = "false";
			PropertyBag["ChangeBy"] = new ChangeBalaceProperties { ChangeType = TypeChangeBalance.OtherSumm };
			PropertyBag["BalanceText"] = 0;
			PropertyBag["ConnectInfo"] = new ClientConnectInfo();
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
					if ((ve.ErrorMessages[0] == "Логин должен быть уникальный") || (ve.ErrorMessages[0] == "Login is currently in use. Please pick up a new Login."))
				{
					edit = true;
				}
			}
			if (Validator.IsValid(partner) || edit)
			{
				BindObjectInstance(part, ParamStore.Form, "Partner");
				part.UpdateAndFlush();
				var agent = Agent.Queryable.Where(a => a.Partner == part).ToList().FirstOrDefault();
				if (agent != null)
				{
					agent.Name = partner.Name;
					agent.Update();
				}
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

		public void RegisterRequest(uint house, int apartment)
		{
			var _house = House.Find(house);
			PropertyBag["tariffs"] = Tariff.FindAll();
			PropertyBag["request"] = new Requests {
													  Street = _house.Street,
													  CaseHouse = _house.Case,
													  House = _house.Number,
													  Apartment = apartment
												  };
			PropertyBag["houseNumber"] = house;
		}

		public void RegisterRequest([DataBind("request")]Requests request, uint houseNumber, uint tariff)
		{
			if (Validator.IsValid(request))
			{
				request.Tariff = Tariff.Find(tariff);
				request.Registrator = InitializeContent.partner;
				request.ActionDate = DateTime.Now;
				request.RegDate = DateTime.Now;
				request.Operator = InitializeContent.partner;
				request.Save();
				request.SetRequestBoduses();
				var apartment =
					Apartment.Queryable.Where(
						a => a.House == House.Find(houseNumber) && a.Number == request.Apartment).
						FirstOrDefault();
				if (apartment == null)
				{
					apartment = new Apartment {
												  House = House.Find(houseNumber),
												  Number = request.Apartment != null ? request.Apartment.Value : 0,
											  };
					apartment.Save();
				}
				//apartment.Comment = string.Format("Заявка номер {0}", request.Id);
				apartment.Status = ApartmentStatus.Queryable.Where(aps => aps.ShortName == "request").FirstOrDefault();
				apartment.Update();
				PaymentsForAgent.CreatePayment(AgentActions.CreateRequest,
				                               string.Format("Начисление за создание заявки #{0}", request.Id),
				                               InitializeContent.partner);
				RedirectToUrl("../HouseMap/ViewHouseInfo.rails?House=" + houseNumber);
			}
		}
	}

}