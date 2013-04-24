﻿using System;
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
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	public class Settings
	{
		public Status DefaultStatus;
		public Recipient DefaultRecipient;
		public Service[] DefaultServices;

		public Settings()
		{
			DefaultServices = new Service[0];
		}

		public Settings(ISession session)
		{
			DefaultStatus = session.Load<Status>((uint)StatusType.BlockedAndNoConnected);
			DefaultRecipient = session.Query<Recipient>().FirstOrDefault(r => r.INN == "3666152146");
			DefaultServices = new Service[] {
				session.Query<Internet>().First(),
				session.Query<IpTv>().First(),
			};
		}

		public static Settings UnitTestSettings()
		{
			var settings = new Settings {
				DefaultServices = new Service[] {
					new Internet(),
					new IpTv(),
				}
			};
			return settings;
		}
	}

	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class RegisterController : BaseController
	{
		[AccessibleThrough(Verb.Post)]
		public void RegisterClient(decimal balanceText, uint status, uint BrigadForConnect, bool VisibleRegisteredInfo, uint house_id, uint requestID)
		{
			SetARDataBinder();

			var registrator = InitializeContent.Partner;
			var agent = DbSession.Query<Agent>().FirstOrDefault(a => a.Partner == registrator);
			var settings = new Settings(DbSession);

			var physicalClient = new PhysicalClient {
				Balance = balanceText
			};
			var client = new Client(physicalClient, settings, registrator);
			var iptv = client.Iptv;
			var internet = client.Internet;

			BindObjectInstance(physicalClient, "client", AutoLoadBehavior.Always);
			BindObjectInstance(iptv, "iptv", AutoLoadBehavior.Always);
			BindObjectInstance(internet, "internet", AutoLoadBehavior.Always);

			PropertyBag["iptv"] = iptv;
			PropertyBag["internet"] = internet;

			if (Validator.IsValid(physicalClient) && Validator.IsValid(client)) {
				client.PhysicalClient.UpdateHouse(DbSession.Load<House>(house_id));
				client.AfterRegistration(agent, registrator);
				foreach (var payment in client.Payments)
					DbSession.Save(payment);

				foreach (var contact in client.Contacts)
					DbSession.Save(contact);

				DbSession.Save(client);
				DbSession.Flush();

				//перед генерацией пароля нужно все сохранить тк для
				var password = client.GeneragePassword();

				var apartmentForClient =
					Apartment.Queryable.FirstOrDefault(a => a.House == physicalClient.HouseObj && a.Number == physicalClient.Apartment);
				if (apartmentForClient != null)
					apartmentForClient.Delete();

				Flash["_client"] = client;
				Flash["WhoConnected"] = client.WhoConnected;
				Flash["Password"] = password;
				Flash["Client"] = physicalClient;
				Flash["AccountNumber"] = client.Id.ToString("00000");
				Flash["ConnectSumm"] = physicalClient.ConnectSum;
				Flash["ConnectInfo"] = client.GetConnectInfo(DbSession).FirstOrDefault();
				foreach (var requestse in Models.Request.FindAllByProperty("Id", requestID)) {
					if (requestse.Registrator != null) {
						physicalClient.Update();
					}
					requestse.Label = Label.Queryable.FirstOrDefault(l => l.ShortComment == "Registered");
					requestse.Archive = true;
					requestse.Client = client;
					requestse.Update();
				}
				if (registrator.Categorie.ReductionName == "Office")
					if (VisibleRegisteredInfo)
						RedirectToUrl("../UserInfo/ClientRegisteredInfo.rails");
					else {
						RedirectToUrl("../UserInfo/SearchUserInfo.rails?filter.ClientCode=" + client.Id);
					}
				if (registrator.Categorie.ReductionName == "Diller")
					RedirectToUrl("../UserInfo/ClientRegisteredInfoFromDiller.rails");
			}
			else {
				EditorValues();

				if (!CategorieAccessSet.AccesPartner("SSI"))
					status = 1;

				PropertyBag["Client"] = physicalClient;
				PropertyBag["BalanceText"] = balanceText;
				PropertyBag["ChHouse"] = DbSession.Get<House>(house_id);
				PropertyBag["Applying"] = "false";
				PropertyBag["ChStatus"] = status;
				PropertyBag["ChBrigad"] = BrigadForConnect;
				physicalClient.SetValidationErrors(Validator.GetErrorSummary(physicalClient));

				PropertyBag["VB"] = new ValidBuilderHelper<PhysicalClient>(physicalClient);
			}
			PropertyBag["RegionList"] = RegionHouse.All();
		}

		public void RegisterLegalPerson()
		{
			PropertyBag["OrderInfo"] = new ClientOrderInfo {
				Order = new Orders() { Number = Orders.GetNextNumber(DbSession) },
				ClientConnectInfo = new ClientConnectInfo()
			};
			PropertyBag["ClientCode"] = 0;
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["Switches"] = NetworkSwitch.All(DbSession);
			PropertyBag["ChBrigad"] = Brigad.FindFirst().Id;
			PropertyBag["ConnectInfo"] = new ConnectInfo();
			PropertyBag["Editing"] = false;
			PropertyBag["LegalPerson"] = new LawyerPerson();
			PropertyBag["VB"] = new ValidBuilderHelper<LawyerPerson>(new LawyerPerson());
			PropertyBag["RegionList"] = RegionHouse.All();
		}

		public void RegisterLegalPerson(int speed, [DataBind("ConnectInfo")] ConnectInfo info, uint brigadForConnect, [DataBind("order")] Orders Order)
		{
			SetBinder(new DecimalValidateBinder { Validator = Validator });
			var person = new LawyerPerson();
			BindObjectInstance(person, ParamStore.Form, "LegalPerson");
			var connectErrors = Validation.ValidationConnectInfo(info, true);
			if (!string.IsNullOrEmpty(info.Port) && Order.OrderServices == null)
				connectErrors = "Невозможно создать подключение, не создавая услуг в заказе";
			if (IsValid(person) && string.IsNullOrEmpty(connectErrors)) {
				person.SaveAndFlush();
				var client = new Client {
					Recipient = Recipient.Queryable.FirstOrDefault(r => r.INN == "3666152146"),
					WhoRegistered = InitializeContent.Partner,
					WhoRegisteredName = InitializeContent.Partner.Name,
					RegDate = DateTime.Now,
					Status = Status.Find((uint)StatusType.BlockedAndNoConnected),
					Disabled = Order.OrderServices == null,
					LawyerPerson = person,
					Name = person.ShortName,
					Type = ClientType.Legal,
				};
				client.SaveAndFlush();

				if (!string.IsNullOrEmpty(person.Telephone)) {
					client.Contacts.Add(new Contact(client, ContactType.MobilePhone, person.Telephone) {
						Comment = "Указан при регистрации"
					});
				}
				if (!string.IsNullOrEmpty(person.Email)) {
					client.Contacts.Add(new Contact(client, ContactType.Email, person.Email) {
						Comment = "Указан при регистрации"
					});
				}

				foreach (var contact in client.Contacts) {
					DbSession.Save(contact);
				}

				ClientEndpoint endPoint = null;
				if (!string.IsNullOrEmpty(info.Port)) {
					endPoint = new ClientEndpoint {
						Client = client,
						Port = Int32.Parse(info.Port),
						Switch = DbSession.Load<NetworkSwitch>(info.Switch),
					};
					endPoint.SaveAndFlush();
					var brigad = Brigad.Find(brigadForConnect);
					client.WhoConnected = brigad;
					client.WhoConnectedName = brigad.Name;
					client.Status = Status.Find((uint)StatusType.Worked);
					client.UpdateAndFlush();
				}
				if (Order.OrderServices != null) {
					Order.Client = client;
					Order.EndPoint = endPoint;
					DbSession.Save(Order);
					// создаем новый акт
					Act act = null;
					if (Order.OrderServices.Any(o => !o.IsPeriodic)) {
						act = new Act(client);
						DbSession.Save(act);
					}
					// создаем новый счет
					Invoice invoice = null;
					if (Order.OrderServices.Any(o => o.IsPeriodic)) {
						invoice = new Invoice(client);
						DbSession.Save(invoice);
					}
					// создаем новый договор
					var contract = new Contract(Order);
					DbSession.Save(contract);
					foreach (var orderService in Order.OrderServices) {
						orderService.Order = Order;
						DbSession.Save(orderService);
						if (!orderService.IsPeriodic) {
							var partAct = new ActPart(act) {
								Count = 1,
								Cost = orderService.Cost,
								Name = orderService.Description + " по заказу №" + orderService.Order.Number,
								OrderService = orderService
							};
							DbSession.Save(partAct);
						}
						else {
							var daysInMonth = DateTime.DaysInMonth(Order.BeginDate.Value.Year, Order.BeginDate.Value.Month);
							var partInvoice = new InvoicePart(invoice,
								1,
								orderService.Cost / daysInMonth * (daysInMonth - Order.BeginDate.Value.Day),
								orderService.Description + " по заказу №" + orderService.Order.Number);
							partInvoice.OrderService = orderService;
							DbSession.Save(partInvoice);
						}
					}
				}
				RegisterLegalPerson();
				PropertyBag["EditiongMessage"] = "Клиент успешно загистрирвоан";
				RedirectToUrl("../UserInfo/LawyerPersonInfo.rails?filter.ClientCode=" + client.Id);
			}
			else {
				PropertyBag["OrderInfo"] = new ClientOrderInfo {
					Order = Order,
					ClientConnectInfo = new ClientConnectInfo()
				};
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
			PropertyBag["RegionList"] = RegionHouse.All();
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
			PropertyBag["ChHouse"] = new House();
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
					PropertyBag["ChHouse"] = houses.First();
				else {
					PropertyBag["ChHouse"] = new House();
					PropertyBag["Message"] = Message.Error("Не удалось сопоставить адрес из заявки ! Будет внимательны при заполнении адреса клиента !");
				}
			}
			else {
				PropertyBag["ChHouse"] = new House();
				PropertyBag["Message"] = Message.Error("Не удалось сопоставить адрес из заявки ! Будет внимательны при заполнении адреса клиента !");
			}
			SendRegisterParam(newPhisClient);
		}

		[return: JSONReturnBinder]
		public object RegisterHouse()
		{
			var street = Request.Form["Street"];
			var number = Request.Form["Number"];
			var _case = Request.Form["Case"];
			var region = Request.Form["RegionId"];
			int res;
			var house = new House();
			var errors = string.Empty;
			if (!Int32.TryParse(number, out res))
				errors += "Неправильно введен номер дома" + res;
			if (string.IsNullOrEmpty(errors)) {
				house = new House { Street = street, Number = Int32.Parse(number) };
				if (!string.IsNullOrEmpty(_case))
					house.Case = _case;
				house.Region = DbSession.Load<RegionHouse>(Convert.ToUInt32(region));
				house.Save();
			}
			return new { Name = string.Format("{0} {1} {2}", street, number, _case), house.Id };
		}

		public void SendRegisterParam(PhysicalClient client)
		{
			var registrator = InitializeContent.Partner;
			var settings = new Settings(DbSession);
			var internalClient = new Client(client, settings, registrator);

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
			PropertyBag["RegionList"] = DbSession.Query<RegionHouse>().ToList();
		}

		private void EditorValues()
		{
			//PropertyBag["Houses"] = House.AllSort;
			PropertyBag["Regions"] = DbSession.Query<RegionHouse>().ToList();
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

		public void HouseSelect(uint regionCode, uint chHouse)
		{
			CancelLayout();
			var houses = DbSession.Query<House>().Where(h => h.Region.Id == regionCode).ToList();
			PropertyBag["ChHouse"] = DbSession.Get<House>(chHouse) ?? new House();
			PropertyBag["Houses"] = houses;
		}
	}
}