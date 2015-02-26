using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;
using Client = Inforoom2.Models.Client;
using House = Inforoom2.Models.House;
using ServiceRequest = Inforoom2.Models.ServiceRequest;
using Street = Inforoom2.Models.Street;

namespace InforoomControlPanel.Controllers
{
	public class ClientController : AdminController
	{
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.BreadCrumb = "Клиенты";
		}

		public ActionResult ClientList(int page=1)
		{
			var perpage = 100;
			var clients = DbSession.Query<Client>().Where(i=>i.PhysicalClient != null).Skip((page-1)*perpage).Take(perpage).ToList();
			ViewBag.Clients = clients;
			//Пагинация
			ViewBag.Models = clients;
			ViewBag.Page = page;
			ViewBag.ModelsPerPage = perpage;
			ViewBag.ModelsCount = DbSession.QueryOver<Client>().Where(i=>i.PhysicalClient != null).RowCount();
			return View("ClientList");
		}

		/// <summary>
		/// Отображает форму новой заявки
		/// </summary>
		public ActionResult ClientRequest()
		{
			InitClientRequest();
			return View();
		}

		[HttpPost]
		public ActionResult ClientRequest(ClientRequest clientRequest)
		{
			var tariff = InitRequestPlans().FirstOrDefault(k => k.Id == clientRequest.Plan.Id);
			clientRequest.Plan = tariff;
			clientRequest.ActionDate = clientRequest.RegDate = DateTime.Now;
			Employee reqAuthor = null;
			if (clientRequest.RequestSource == RequestType.FromOperator) {
				reqAuthor = DbSession.Query<Employee>()
					.FirstOrDefault(e => e.Id == clientRequest.RequestAuthor.Id);
			}
			clientRequest.RequestAuthor = reqAuthor;
			var errors = ValidationRunner.ValidateDeep(clientRequest);

			if (errors.Length == 0 && clientRequest.IsContractAccepted) {
				clientRequest.Address = GetAddressByYandexData(clientRequest);
				DbSession.Save(clientRequest);
				SuccessMessage(string.Format("Спасибо, Ваша заявка создана. Номер заявки {0}", clientRequest.Id));
				return RedirectToAction("ClientRequest");
			}
			// Пока используется IsContractAccepted=true, закомментированные строки кода не нужны
			//if (!clientRequest.IsContractAccepted) {
			//	ErrorMessage("Пожалуйста, подтвердите, что Вы согласны с договором-офертой");
			//}
			ViewBag.IsRedirected = false;
			ViewBag.IsCityValidated = false;
			ViewBag.IsStreetValidated = false;
			ViewBag.IsHouseValidated = false;
			ViewBag.ClientRequest = clientRequest;
			ViewBag.Employees = DbSession.Query<Employee>().ToList();
			return View();
		}

		private void InitClientRequest(Plan plan = null, string city = "", string street = "", string house = "")
		{
			ViewBag.IsRedirected = false;
			ViewBag.IsCityValidated = false;
			ViewBag.IsStreetValidated = false;
			ViewBag.IsHouseValidated = false;
			var clientRequest = new ClientRequest() {
				IsContractAccepted = true,
				RequestAuthor = GetCurrentEmployee()
			};

			if (!string.IsNullOrEmpty(UserCity)) {
				clientRequest.City = UserCity;
			}

			if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(street) && !string.IsNullOrEmpty(house)) {
				clientRequest.Street = street;
				clientRequest.City = city;
				ViewBag.IsCityValidated = true;
				ViewBag.IsStreetValidated = true;

				int housen;
				int.TryParse(house, out housen);
				if (housen != 0) {
					ViewBag.IsHouseValidated = true;
					clientRequest.HouseNumber = housen;
				}
			}

			if (plan != null) {
				clientRequest.Plan = plan;
				ViewBag.IsRedirected = true;
			}
			ViewBag.ClientRequest = clientRequest;
			ViewBag.Employees = DbSession.Query<Employee>().ToList();
			InitRequestPlans();
		}

		private List<Plan> InitRequestPlans()
		{
			var plans = DbSession.Query<Plan>().Where(p => !p.IsArchived).ToList();
			ViewBag.Plans = plans;
			return plans;
		}

		protected Address GetAddressByYandexData(ClientRequest clientRequest)
		{
			var city = GetList<City>().FirstOrDefault(c => c.Name.Equals(clientRequest.YandexCity, StringComparison.InvariantCultureIgnoreCase));

			if (city == null || !clientRequest.IsYandexAddressValid()) {
				var badAddress = new Address { IsCorrectAddress = false };
				return badAddress;
			}
			var region = GetList<Region>().FirstOrDefault(r => r.City == city);

			var street = GetList<Street>().FirstOrDefault(s => s.Name.Equals(clientRequest.YandexStreet, StringComparison.InvariantCultureIgnoreCase)
			                                                   && s.Region.Equals(region));

			if (street == null) {
				street = new Street(clientRequest.YandexStreet);
			}

			var house = GetList<House>().FirstOrDefault(h => h.Number.Equals(clientRequest.YandexHouse, StringComparison.InvariantCultureIgnoreCase)
			                                                 && h.Street.Name.Equals(clientRequest.YandexStreet, StringComparison.InvariantCultureIgnoreCase)
			                                                 && h.Street.Region.Equals(region));

			if (house == null) {
				house = new House(clientRequest.YandexHouse);
			}
			var address = GetList<Address>().FirstOrDefault(a => a.IsCorrectAddress
			                                                     && a.House.Equals(house)
			                                                     && a.House.Street.Equals(street)
			                                                     && a.House.Street.Region.Equals(region)
			                                                     && a.Entrance == clientRequest.Entrance
			                                                     && a.Floor == clientRequest.Floor
			                                                     && a.Apartment == clientRequest.Apartment);

			//if (address == null) {
			//	address = new Address();
			//	address.House = house;
			//	address.Apartment = clientRequest.Apartment;
			//	address.Floor = clientRequest.Floor;
			//	address.Entrance = clientRequest.Entrance;
			//	address.House.Street = street;
			//	address.House.Street.Region = region;
			//	address.IsCorrectAddress = true;
			//}
			return address;
		}

		/// <summary>
		/// Список заявок на подключение
		/// </summary>
		/// <param name="ClientRequest"></param>
		/// <returns></returns>
		public ActionResult ClientRequestsList()
		{
			var clientRequests = DbSession.Query<ClientRequest>().OrderByDescending(i=>i.Id).ToList();
			ViewBag.ClientRequests = clientRequests;
			return View();
		}

		/// <summary>
		/// Создание заявки на подключение
		/// </summary>
		/// <param name="clientId"></param>
		/// <returns></returns>
		public ActionResult ServiceRequest(int clientId)
		{
			var client = DbSession.Get<Client>(clientId);
			var ServiceRequest = new ServiceRequest(client);
			var servicemen = DbSession.Query<ServiceMan>().ToList();
			ViewBag.Client = client;
			ViewBag.ServiceRequest = ServiceRequest;
			ViewBag.Servicemen = servicemen;
			ViewBag.ServicemenDate = DateTime.Today;
			return View();
		}

		/// <summary>
		/// Создание заявки на подключение
		/// </summary>
		/// <param name="ServiceRequest"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult ServiceRequest([EntityBinder] ServiceRequest ServiceRequest)
		{
			var client = ServiceRequest.Client;
			this.ServiceRequest(client.Id);
			ViewBag.ServicemenDate = ServiceRequest.BeginTime.Date;
			var errors = ValidationRunner.ValidateDeep(ServiceRequest);
			if (errors.Length == 0) {
				DbSession.Save(ServiceRequest);
				SuccessMessage("Сервисная заявка успешно добавлена");
				return this.ServiceRequest(client.Id);
			}
			ViewBag.ServiceRequest = ServiceRequest;
			return View();
		}
	}
}