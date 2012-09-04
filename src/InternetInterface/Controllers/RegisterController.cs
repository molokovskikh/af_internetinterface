using System;
using System.Linq;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Controllers;
using InternetInterface.AllLogic;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class RegisterController : BaseController
	{
		[AccessibleThrough(Verb.Post)]
		public void RegisterClient(decimal balanceText, uint status, uint BrigadForConnect,
			[DataBind("ConnectInfo")] ConnectInfo ConnectInfo, bool VisibleRegisteredInfo, uint house_id,
			uint requestID)
		{
			SetARDataBinder();

			var phisClient = new PhysicalClient();
			phisClient.Balance = balanceText;
			var defaultServices = new Service[] {
				DbSession.Query<Internet>().First(),
				DbSession.Query<IpTv>().First(),
			};
			var client = new Client(phisClient, defaultServices) {
				WhoRegistered = InitializeContent.Partner,
				WhoRegisteredName = InitializeContent.Partner.Name,
				Status = Status.Find((uint)StatusType.BlockedAndNoConnected),
				PhysicalClient = phisClient,
				Recipient = Recipient.Queryable.FirstOrDefault(r => r.INN == "3666152146")
			};
			var iptv = client.Iptv;
			var internet = client.Internet;

			BindObjectInstance(phisClient, "client", AutoLoadBehavior.Always);
			BindObjectInstance(iptv, "iptv", AutoLoadBehavior.Always);
			BindObjectInstance(internet, "internet", AutoLoadBehavior.Always);

			PropertyBag["iptv"] = iptv;
			PropertyBag["internet"] = internet;

			phisClient.Password = CryptoPass.GeneratePassword();
			if (!CategorieAccessSet.AccesPartner("SSI"))
				status = 1;
			if (!CategorieAccessSet.AccesPartner("DHCP")) {
				ConnectInfo.Port = null;
			}
			var portException = Validation.ValidationConnectInfo(ConnectInfo);

			var registerClient = Validator.IsValid(phisClient);

			if ((registerClient && string.IsNullOrEmpty(portException)) ||
				(registerClient && string.IsNullOrEmpty(ConnectInfo.Port))) {
				PhysicalClient.RegistrLogicClient(phisClient, house_id, Validator);

				var havePayment = phisClient.Balance > 0;
				client.Name = string.Format("{0} {1} {2}", phisClient.Surname, phisClient.Name, phisClient.Patronymic);
				client.AutoUnblocked = havePayment;
				client.Disabled = !havePayment;

				client.SaveAndFlush();

				if (!string.IsNullOrEmpty(phisClient.PhoneNumber)) {
					Contact.SaveNew(client, phisClient.PhoneNumber.Replace("-", string.Empty), "Указан при регистрации",
						ContactType.MobilePhone);
					Contact.SaveNew(client, phisClient.PhoneNumber.Replace("-", string.Empty), "Указан при регистрации",
						ContactType.SmsSending);
				}

				if (!string.IsNullOrEmpty(phisClient.HomePhoneNumber))
					Contact.SaveNew(client, phisClient.HomePhoneNumber.Replace("-", string.Empty), "Указан при регистрации", ContactType.HousePhone);

				if (!string.IsNullOrEmpty(phisClient.Email))
					Contact.SaveNew(client, phisClient.Email, "Указан при регистрации", ContactType.Email);


				if (havePayment) {
					var payment = new Payment {
						Agent =
							Agent.FindAllByProperty("Partner", InitializeContent.Partner).First(),
						BillingAccount = true,
						Client = client,
						PaidOn = DateTime.Now,
						RecievedOn = DateTime.Now,
						Sum = phisClient.Balance
					};
					payment.SaveAndFlush();
				}
				var apartmentForClient =
					Apartment.Queryable.FirstOrDefault(a => a.House == phisClient.HouseObj && a.Number == phisClient.Apartment);
				if (apartmentForClient != null)
					apartmentForClient.Delete();
				if (!string.IsNullOrEmpty(ConnectInfo.Port) && CategorieAccessSet.AccesPartner("DHCP")) {
					var endpoint = new ClientEndpoint(client,
						Convert.ToInt32(ConnectInfo.Port),
						DbSession.Load<NetworkSwitch>(ConnectInfo.Switch));
					endpoint.SaveAndFlush();
					if (BrigadForConnect != 0) {
						var brigad = Brigad.Find(BrigadForConnect);
						client.WhoConnected = brigad;
						client.WhoConnectedName = brigad.Name;
					}
					client.ConnectedDate = DateTime.Now;
					client.Status = Status.Find((uint)StatusType.BlockedAndConnected);
					client.UpdateAndFlush();
				}
				Flash["_client"] = client;
				Flash["WhoConnected"] = client.WhoConnected;
				Flash["Password"] = CryptoPass.GeneratePassword();
				Flash["Client"] = phisClient;
				Flash["AccountNumber"] = client.Id.ToString("00000");
				Flash["ConnectSumm"] = phisClient.ConnectSum;
				Flash["ConnectInfo"] = client.GetConnectInfo(DbSession).FirstOrDefault();
				foreach (var requestse in Models.Request.FindAllByProperty("Id", requestID)) {
					if (requestse.Registrator != null) {
						phisClient.Update();
					}
					requestse.Label = Label.Queryable.FirstOrDefault(l => l.ShortComment == "Registered");
					requestse.Archive = true;
					requestse.Client = client;
					requestse.Update();
				}
				if (InitializeContent.Partner.Categorie.ReductionName == "Office")
					if (VisibleRegisteredInfo)
						RedirectToUrl("../UserInfo/ClientRegisteredInfo.rails");
					else {
						RedirectToUrl("../UserInfo/SearchUserInfo.rails?filter.ClientCode=" + client.Id);
					}
				if (InitializeContent.Partner.Categorie.ReductionName == "Diller")
					RedirectToUrl("../UserInfo/ClientRegisteredInfoFromDiller.rails");
			}
			else {
				EditorValues();

				PropertyBag["Client"] = phisClient;
				PropertyBag["BalanceText"] = balanceText;
				PropertyBag["ChHouse"] = house_id;
				PropertyBag["Applying"] = "false";
				PropertyBag["PortError"] = portException;
				PropertyBag["ChStatus"] = status;
				PropertyBag["ChBrigad"] = BrigadForConnect;
				phisClient.SetValidationErrors(Validator.GetErrorSummary(phisClient));
				if (!string.IsNullOrEmpty(portException))
					ConnectInfo.Port = string.Empty;
				PropertyBag["ConnectInfo"] = ConnectInfo;
				PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClient>(phisClient);
			}
		}


		public void RegisterLegalPerson()
		{
			PropertyBag["ClientCode"] = 0;
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["Switches"] = NetworkSwitch.All(DbSession);
			PropertyBag["ChBrigad"] = Brigad.FindFirst().Id;
			PropertyBag["ConnectInfo"] = new ConnectInfo();
			PropertyBag["Editing"] = false;
			PropertyBag["LegalPerson"] = new LawyerPerson();
			PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(new LawyerPerson());
		}

		public void RegisterLegalPerson(int speed, [DataBind("ConnectInfo")] ConnectInfo info, uint brigadForConnect)
		{
			SetBinder(new DecimalValidateBinder { Validator = Validator });
			var person = new LawyerPerson();
			BindObjectInstance(person, ParamStore.Form, "LegalPerson");
			var connectErrors = Validation.ValidationConnectInfo(info);
			if (IsValid(person) && string.IsNullOrEmpty(connectErrors)) {
				person.SaveAndFlush();
				var client = new Client {
					Recipient = Recipient.Queryable.FirstOrDefault(r => r.INN == "3666152146"),
					WhoRegistered = InitializeContent.Partner,
					WhoRegisteredName = InitializeContent.Partner.Name,
					RegDate = DateTime.Now,
					Status = Status.Find((uint)StatusType.BlockedAndNoConnected),
					Disabled = person.Tariff == null,
					LawyerPerson = person,
					Name = person.ShortName,
					Type = ClientType.Legal,
				};
				client.SaveAndFlush();

				if (!string.IsNullOrEmpty(person.Telephone))
					Contact.SaveNew(client, person.Telephone, "Указан при регистрации", ContactType.MobilePhone);

				if (!string.IsNullOrEmpty(person.Email))
					Contact.SaveNew(client, person.Email, "Указан при регистрации", ContactType.Email);

				if (!string.IsNullOrEmpty(info.Port)) {
					new ClientEndpoint {
						Client = client,
						Port = Int32.Parse(info.Port),
						Switch = DbSession.Load<NetworkSwitch>(info.Switch),
					}.SaveAndFlush();
					var brigad = Brigad.Find(brigadForConnect);
					client.WhoConnected = brigad;
					client.WhoConnectedName = brigad.Name;
					client.Status = Status.Find((uint)StatusType.Worked);
					client.UpdateAndFlush();
				}
				RegisterLegalPerson();
				PropertyBag["EditiongMessage"] = "Клиент успешно загистрирвоан";
				RedirectToUrl("../UserInfo/LawyerPersonInfo.rails?filter.ClientCode=" + client.Id);
			}
			else {
				PropertyBag["ClientCode"] = 0;
				PropertyBag["Brigads"] = Brigad.FindAllSort();
				PropertyBag["Switches"] = NetworkSwitch.All(DbSession);
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
		public void RegisterPartner([DataBind("Partner")] Partner partner)
		{
			string Pass = CryptoPass.GeneratePassword();
			if (Partner.RegistrLogicPartner(partner, Validator)) {
#if !DEBUG
				if (ActiveDirectoryHelper.FindDirectoryEntry(partner.Login) == null)
					ActiveDirectoryHelper.CreateUserInAD(partner.Login, Pass);
#endif
				Flash["Partner"] = partner;
				Flash["PartnerPass"] = Pass;
				RedirectToUrl("..//UserInfo/PartnerRegisteredInfo.rails");
			}
			else {
				partner.SetValidationErrors(Validator.GetErrorSummary(partner));
				PropertyBag["Partner"] = partner;
				PropertyBag["catType"] = partner.Categorie.Id;
				PropertyBag["Editing"] = false;
				PropertyBag["VB"] = new ValidBuilderHelper<Partner>(partner);
			}
		}

		public void RegisterClient()
		{
			var client = new PhysicalClient();
			SendRegisterParam(client);
			PropertyBag["ChHouse"] = 0;
			PropertyBag["Client"] = client;
		}

		public void RegisterClient(uint requestID)
		{
			var request = Models.Request.Find(requestID);
			var fio = new string[3];
			var _fio =
				request.ApplicantName.Split(' ').Select(s => s.Replace(" ", string.Empty)).Where(s => !string.IsNullOrEmpty(s)).ToArray();
			if (_fio.Length >= 3) {
				_fio.Take(3).ToArray().CopyTo(fio, 0);
			}
			else {
				_fio.Take(_fio.Length).ToArray().CopyTo(fio, 0);
			}
			var newPhisClient = new PhysicalClient {
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
			if (request.ApplicantPhoneNumber.Length == 10)
				newPhisClient.PhoneNumber = UsersParsers.MobileTelephoneParcer(request.ApplicantPhoneNumber);
			if (request.ApplicantPhoneNumber.FirstOrDefault() == '4')
				newPhisClient.HomePhoneNumber = UsersParsers.MobileTelephoneParcer(request.ApplicantPhoneNumber);
			PropertyBag["Client"] = newPhisClient;
			PropertyBag["requestID"] = requestID;
			if (newPhisClient.House != null) {
				var houses =
					House.Queryable.Where(
						h =>
							h.Street == newPhisClient.Street &&
								h.Number == newPhisClient.House &&
								h.Case == newPhisClient.CaseHouse)
						.ToList();
				if (houses.Count != 0)
					PropertyBag["ChHouse"] = houses.First().Id;
				else {
					PropertyBag["ChHouse"] = 0;
					PropertyBag["Message"] = Message.Error("Не удалось сопоставить адрес из заявки ! Будте внимательны при заполнении адреса клиента !");
				}
			}
			else {
				PropertyBag["ChHouse"] = 0;
				PropertyBag["Message"] = Message.Error("Не удалось сопоставить адрес из заявки ! Будте внимательны при заполнении адреса клиента !");
			}
			SendRegisterParam(newPhisClient);
		}

		[return: JSONReturnBinder]
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
			if (string.IsNullOrEmpty(errors)) {
				house = new House { Street = street, Number = Int32.Parse(number) };
				if (!string.IsNullOrEmpty(_case))
					house.Case = _case;
				house.Save();
			}
			return new { Name = string.Format("{0} {1} {2}", street, number, _case), house.Id };
		}

		public void SendRegisterParam(PhysicalClient client)
		{
			var internalClient = new Client(client, new Service[] {
				DbSession.Query<Internet>().First(),
				DbSession.Query<IpTv>().First(),
			});

			client.Client = internalClient;

			EditorValues();

			PropertyBag["BalanceText"] = string.Empty;

			PropertyBag["iptv"] = client.Client.Iptv;
			PropertyBag["internet"] = client.Client.Internet;

			PropertyBag["ChBrigad"] = 0;
			PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClient>(new PhysicalClient());

			PropertyBag["Applying"] = "false";
			PropertyBag["BalanceText"] = 0;
			PropertyBag["ConnectInfo"] = new ClientConnectInfo();
		}

		private void EditorValues()
		{
			PropertyBag["Houses"] = House.AllSort;
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["Statuss"] = Status.FindAllSort();
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["channels"] = ChannelGroup.All(DbSession);
			PropertyBag["Switches"] = NetworkSwitch.All(DbSession);
		}

		[AccessibleThrough(Verb.Post)]
		public void EditPartner([DataBind("Partner")] Partner partner, int PartnerKey)
		{
			var part = Partner.Find((uint)PartnerKey);
			var edit = false;
			if (Partner.Find((uint)PartnerKey).Login == partner.Login) {
				Validator.IsValid(partner);
				partner.SetValidationErrors(Validator.GetErrorSummary(partner));
				var ve = partner.GetValidationErrors();
				if (ve.ErrorsCount == 1)
					if ((ve.ErrorMessages[0] == "Логин должен быть уникальный") || (ve.ErrorMessages[0] == "Login is currently in use. Please pick up a new Login.")) {
						edit = true;
					}
			}
			if (Validator.IsValid(partner) || edit) {
				BindObjectInstance(part, ParamStore.Form, "Partner");
				part.Categorie.Refresh();
				part.UpdateAndFlush();
				var agent = Agent.Queryable.Where(a => a.Partner == part).ToList().FirstOrDefault();
				if (agent != null) {
					agent.Name = partner.Name;
					agent.Update();
				}
				Flash["EditiongMessage"] = "Изменения внесены успешно";
				RedirectToUrl("../Register/RegisterPartner?PartnerKey=" + part.Id + "&catType=" + part.Categorie.Id);
			}
			else {
				partner.SetValidationErrors(Validator.GetErrorSummary(partner));
				RegisterPartnerSendParam((int)partner.Id);
				RenderView("RegisterPartner");
				Flash["Partner"] = partner;
				Flash["catType"] = partner.Categorie.Id;
				PropertyBag["VB"] = new ValidBuilderHelper<Partner>(partner);
			}
		}

		public void RegisterPartnerSendParam(int PartnerKey)
		{
			PropertyBag["VB"] = new ValidBuilderHelper<Partner>(new Partner());
			PropertyBag["Applying"] = "false";
			PropertyBag["Editing"] = true;
		}

		public void RegisterPartner(int PartnerKey, int catType)
		{
			var partner = Partner.Queryable.FirstOrDefault(p => p.Id == (uint)PartnerKey);
			if (partner != null) {
				RegisterPartnerSendParam(PartnerKey);
				PropertyBag["Partner"] = partner;
				PropertyBag["catType"] = catType;
				PropertyBag["PartnerKey"] = PartnerKey;
			}
			else {
				RedirectToUrl("../Register/RegisterPartner");
			}
		}

		public void RegisterPartner(int catType)
		{
			PropertyBag["Partner"] = new Partner {
				Categorie = new UserCategorie()
			};
			PropertyBag["catType"] = catType;
			PropertyBag["VB"] = new ValidBuilderHelper<Partner>(new Partner());
			PropertyBag["Editing"] = false;
			PropertyBag["catType"] = catType;
		}

		public void RegisterRequest(uint house, int apartment)
		{
			var houseEntity = House.Find(house);
			PropertyBag["tariffs"] = Tariff.FindAll();
			PropertyBag["Request"] = new Request {
				Street = houseEntity.Street,
				CaseHouse = houseEntity.Case,
				House = houseEntity.Number,
				Apartment = apartment
			};
			PropertyBag["houseNumber"] = house;
		}

		public void RegisterRequest([DataBind("Request")] Request request, uint houseNumber, uint tariff)
		{
			if (Validator.IsValid(request)) {
				var phone = request.ApplicantPhoneNumber;
				phone = phone.Remove(0, 2);
				request.ApplicantPhoneNumber = phone.Replace("-", string.Empty);
				request.Tariff = Tariff.Find(tariff);
				request.Registrator = InitializeContent.Partner;
				request.ActionDate = DateTime.Now;
				request.RegDate = DateTime.Now;
				request.Operator = InitializeContent.Partner;
				request.Save();
				var apartment = Apartment.Queryable.FirstOrDefault(a => a.House == House.Find(houseNumber) && a.Number == request.Apartment);
				if (apartment == null) {
					apartment = new Apartment {
						House = House.Find(houseNumber),
						Number = request.Apartment != null ? request.Apartment.Value : 0,
					};
					apartment.Save();
				}
				apartment.Status = ApartmentStatus.Queryable.FirstOrDefault(aps => aps.ShortName == "Request");
				apartment.Update();
				RedirectToUrl("../HouseMap/ViewHouseInfo.rails?House=" + houseNumber);
			}
		}
	}
}