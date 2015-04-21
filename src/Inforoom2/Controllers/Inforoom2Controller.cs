using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class Inforoom2Controller : BaseController
	{
		protected new virtual CustomPrincipal User
		{
			get { return HttpContext.User as CustomPrincipal; }
		}

		protected Client CurrentClient
		{
			get
			{
				if (User == null || DbSession == null || !DbSession.IsConnected) {
					return null;
				}
				int id;
				int.TryParse(User.Identity.Name, out id);
				return DbSession.Get<Client>(id);
			}
		}

		private static string userCity;

		public static string UserCity
		{
			get { return userCity; }
		}

		public Region CurrentRegion
		{
			get
			{
				return DbSession.Query<Region>().FirstOrDefault(r => r.Name == UserCity)
				       ?? DbSession.Query<Region>().FirstOrDefault();
			}
		}

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			var cookieCity = GetCookie("userCity");
			if (!string.IsNullOrEmpty(cookieCity)) {
				userCity = cookieCity;
			}
			ViewBag.Title = "Инфорум";
			ViewBag.Cities = new[] { "Борисоглебск", "Белгород" };
			ViewBag.JavascriptParams["baseurl"] = String.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Authority, UrlHelper.GenerateContentUrl("~/", HttpContext));
			ViewBag.ActionName = filterContext.RouteData.Values["action"].ToString();
			ViewBag.ControllerName = GetType().Name.Replace("Controller", "");
			//todo куда это девать?
			var newCallMeBackTicket = new CallMeBackTicket() {
				Name = (CurrentClient == null) ? "" : CurrentClient.Name,
				PhoneNumber = (CurrentClient == null) ? "" : CurrentClient.PhoneNumber
			};

			ViewBag.CallMeBackTicket = ViewBag.CallMeBackTicket ?? newCallMeBackTicket;

			ProcessRegionPanel();
			if (TryAuthorizeNetworkClient())
				return;
			ViewBag.NetworkClientFlag = GetCookie("networkClient") != null;
			if (CurrentClient != null) {
				var sb = new StringBuilder();
				sb.AppendFormat("Здравствуйте, {0} {1}. Ваш баланс: {2} руб.", CurrentClient.PhysicalClient.Name,
					CurrentClient.PhysicalClient.Patronymic, CurrentClient.PhysicalClient.Balance);
				ViewBag.ClientInfo = sb.ToString();
			}
			if (!CheckNetworkClient())
				RedirectToAction("Index", "Home");

			if (CurrentRegion != null) {
				ViewBag.RegionOfficePhoneNumber = CurrentRegion.RegionOfficePhoneNumber;
				ViewBag.CurrentRegion = CurrentRegion;
			}
		}

		//Авторизация клиента из сети
		private bool TryAuthorizeNetworkClient()
		{
			var ipstring = Request.UserHostAddress;

#if DEBUG
			//Можем авторизоваться по лизе за клиента
			ipstring = Request.QueryString["ip"] ?? null;
			if (GetCookie("debugIp") == null && ipstring != null)
				SetCookie("debugIp", ipstring);
#endif
			if (CurrentClient != null || string.IsNullOrEmpty(ipstring))
				return false;
			var endpoint = ClientEndpoint.GetEndpointForIp(ipstring, DbSession);
			if (endpoint != null && endpoint.Client.PhysicalClient != null) //Юриков авторизовывать не нужно
			{
				SetCookie("networkClient", "true");
				// если у клиента есть адрес, связанный с эндпойнтом, по нему сохраняем город (userCity) в cookie 
				if (endpoint.Client.PhysicalClient.Address != null) {
					SetCookie("userCity", endpoint.Client.PhysicalClient.Address.House.Street.Region.Name);
				}

				this.Authenticate(ViewBag.ActionName, ViewBag.ControllerName, endpoint.Client.Id.ToString(), true);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Проверка cookie "networkClient" при авторизации 
		/// </summary>
		/// <returns></returns>
		private bool CheckNetworkClient()
		{
			var ipstring = Request.UserHostAddress;
#if DEBUG
			ipstring = GetCookie("debugIp");
#endif
			//если нет куки значит клиент не из нутри сети - все впроядке
			var cookie = GetCookie("networkClient");
			if (cookie == null)
				return true;
			//если нет текущего клиента то снимаем флаг клиента из интернета
			//больше ничего делать не надо - он может продолжить работку
			if (CurrentClient == null || string.IsNullOrEmpty(ipstring)) {
				SetCookie("networkClient", null);
				EmailSender.SendDebugInfo("Снимаем куку залогиненного автоматически клиента так как он не найден: " + ipstring, CollectDebugInfo().ToString());
				return true;
			}

			//Выкидываем юрика
			if (CurrentClient.PhysicalClient == null) {
				SetCookie("networkClient", null);
				var msg = "Выкидываем юридического клиента: " + CurrentClient.Id;
				EmailSender.SendDebugInfo(msg, CollectDebugInfo().ToString());
				FormsAuthentication.SignOut();
				return false;
			}

			var endpoint = ClientEndpoint.GetEndpointForIp(ipstring, DbSession);
			if (endpoint != null) {
				if (endpoint.Client.Id != CurrentClient.Id) {
					//Оказывается, что точка подключения принадлежит другому клиенту и текущий сидит в чужом ЛК
					//Снимаем куку и выкидываем клиента из ЛК
					//Возможно нужен еще редирект
					SetCookie("networkClient", null);
					var msg = "Выкидываем неправильно залогиненного клиента: " + ipstring + "," + endpoint.Client.Id + ", " + CurrentClient.Id;
					EmailSender.SendDebugInfo(msg, CollectDebugInfo().ToString());
					FormsAuthentication.SignOut();
					return false;
				}
				//был найден клиент по точке подключения и текущий клиент. они совпадают, так что все путем
				return true;
			}

			//Получается текущий клиент есть, флаг того, что мы его авторизовали есть, но точки подключения у него нет. Как так? Выкидываем
			SetCookie("networkClient", null);
			var str = "Выкидываем залогиненного клиента без аренды: " + ipstring + ", " + CurrentClient.Id;
			EmailSender.SendDebugInfo(str, CollectDebugInfo().ToString());
			FormsAuthentication.SignOut();
			return false;
		}

		private void ProcessCallMeBackTicket()
		{
			var binder = new EntityBinderAttribute("callMeBackTicket.Id", typeof(CallMeBackTicket));
			var callMeBackTicket = (CallMeBackTicket)binder.MapModel(Request);
			ViewBag.CallMeBackTicket = callMeBackTicket;
			if (Request.Params["callMeBackTicket.Name"] == null)
				return;
			callMeBackTicket.Client = CurrentClient;

			var errors = ValidationRunner.Validate(callMeBackTicket);
			if (errors.Length == 0) {
				DbSession.Save(callMeBackTicket);
				if (callMeBackTicket.Client != null) {
					var appeal = new Appeal("Клиент создал запрос на обратный звонок № " + callMeBackTicket.Id,
						callMeBackTicket.Client, AppealType.FeedBack) {
							Employee = GetCurrentEmployee()
						};
					DbSession.Save(appeal);
				}

				SuccessMessage("Заявка отправлена. В течении дня вам перезвонят.");
				return;
			}

			if (GetJavascriptParam("CallMeBack") == null)
				AddJavascriptParam("CallMeBack", "1");
		}

		public void ProcessRegionPanel()
		{
			var cookieCity = GetCookie("userCity");
			if (User == null) {
				//Анонимный посетитель. Определяем город.
				if (!string.IsNullOrEmpty(cookieCity)) {
					userCity = cookieCity;
				}
				else {
					userCity = GetVisitorCityByGeoBase();
				}
			}
			else {
				if (!string.IsNullOrEmpty(cookieCity)) {
					userCity = cookieCity;
				}
				else {
					//Куков нет, пытаемся достать город из базы, иначе определяем по геобазе
					Client user = null;
					int userId;
					int.TryParse(User.Identity.Name, out userId);
					if (userId != 0)
						user = DbSession.Query<Client>().FirstOrDefault(k => k.Id == userId);
					if (user != null && user.Address != null) {
						userCity = user.Address.House.Street.Region.City.Name;
					}
					else {
						userCity = GetVisitorCityByGeoBase();
					}
				}
			}

			ViewBag.UserCityBelongsToUs = IsUserCityBelongsToUs(UserCity);
			ViewBag.UserCity = UserCity;
			ViewBag.UserRegion = DbSession.Query<Region>().FirstOrDefault(i => i.Name == UserCity);
			if (ViewBag.UserRegion == null)
				ViewBag.UserRegion = DbSession.Query<Region>().First();
		}

		private bool IsUserCityBelongsToUs(string city)
		{
			if (city != null) {
				var region = DbSession.Query<Region>().FirstOrDefault(i => i.Name.Contains(city) && i.City != null);
				if (region != null)
					return true;
			}
			return false;
		}

		private string GetVisitorCityByGeoBase()
		{
			var geoService = new IpGeoBase();
			IpAnswer geoAnswer;
			try {
				geoAnswer = geoService.GetInfo();
			}
			catch (Exception) {
				return null;
			}

			if (geoAnswer == null) return null;
			return geoAnswer.City;
		}

		public void SubmitCallMeBackTicket(string actionString, string controllerString)
		{
			ProcessCallMeBackTicket();
			ForwardToAction(controllerString, actionString, new object[0]);
		}

		protected override StringBuilder CollectDebugInfo()
		{
			var builder = new StringBuilder(1000);
			if (CurrentClient != null)
				builder.Append("Клиент: " + CurrentClient.Id + " \n ");

			//Не должно случаться, но добавил, так как боюсь циклических исключений
			//Получаем ip, ловим исключение, собираем инфо, получаем ip и так до бесконечности
			try {
				var tryClient = Client.GetClientForIp(Request.UserHostAddress, DbSession);
				if (tryClient != null)
					builder.Append("Клиент (по аренде): " + tryClient.Id + " \n ");
			}
			catch (Exception e) {
				builder.Append("Поймали циклическое исключение на попытке получить ip клиента \n ");
			}

			builder.Append("Дата: " + DateTime.Now + " \n ");
			builder.Append("Referrer: " + Request.UrlReferrer + " \n ");
			builder.Append("Query: " + Request.QueryString + " \n ");
			builder.Append("Ip: " + Request.UserHostAddress + " \n ");
			builder.Append("Форма:] \n ");
			foreach (var key in Request.Form.AllKeys) {
				builder.Append(key);
				builder.Append(" : ");
				builder.Append(Request.Form[key]);
				builder.Append("\n");
			}
			builder.Append("]");
			builder.Append("Запрос: " + Request.FilePath + " : " + Request.QueryString + " \n ");
			builder.Append("Браузер: " + Request.Browser.Browser + " \n ");
			builder.Append("Куки:[ \n ");
			foreach (var key in Request.Cookies.AllKeys) {
				builder.Append(key);
				builder.Append(" : ");
				builder.Append(GetCookie(key) ?? "");
				builder.Append("\n");
			}
			builder.Append("]");
			return builder;
		}
	}
}