using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;
using NHibernate.Validator.Engine;
using Region = Inforoom2.Models.Region;

namespace Inforoom2.Controllers
{
	public class Inforoom2Controller : BaseController
	{
		protected new virtual CustomPrincipal User
		{
			get { return HttpContext.User as CustomPrincipal; }
		}

		protected Client _CurrentClient;

		protected Client CurrentClient
		{
			get
			{
				if (_CurrentClient != null)
					return _CurrentClient;

				if (User == null || DbSession == null || !DbSession.IsConnected) {
					return null;
				}
				int id;
				int.TryParse(User.Identity.Name, out id);
				var client = DbSession.Get<Client>(id);
				_CurrentClient = client;
				return client;
			}
			set { _CurrentClient = value; }
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
			var CityList = DbSession.Query<Region>().Where(s => s.ShownOnMainPage).Select(s => s.Name).OrderBy(s => s).ToArray();
			ViewBag.Cities = CityList;
			//todo куда это девать?
			var newCallMeBackTicket = new CallMeBackTicket() {
				Name = (CurrentClient == null) ? "" : CurrentClient.Name,
				PhoneNumber = (CurrentClient == null) ? "" : CurrentClient.PhoneNumber
			};

			ViewBag.CallMeBackTicket = ViewBag.CallMeBackTicket ?? newCallMeBackTicket;

			ProcessRegionPanel();
			ProcessPrivateMessage();

			if (TryAuthorizeNetworkClient())
				return;
			ViewBag.NetworkClientFlag = GetCookie("networkClient") != null;
			if (CurrentClient != null) {
				var sb = new StringBuilder();
				sb.AppendFormat("Здравствуйте, {0} {1}. Ваш баланс: {2} руб.", CurrentClient.PhysicalClient.Name,
					CurrentClient.PhysicalClient.Patronymic, CurrentClient.PhysicalClient.Balance);
				ViewBag.ClientInfo = sb.ToString();
				ViewBag.CurrentClient = CurrentClient;
			}
			if (!CheckNetworkClient())
				RedirectToAction("Index", "Home");

			if (CurrentRegion != null) {
				ViewBag.RegionOfficePhoneNumber = CurrentRegion.RegionOfficePhoneNumber;
				ViewBag.CurrentRegion = CurrentRegion;
			}
		}

		//Авторизация клиента из сети
		public bool TryAuthorizeNetworkClient()
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
				//Это необходимо, чтобы авторизация срабатывала моментально. Так как метод authenticate требует перезагрузки страницы
				CurrentClient = endpoint.Client;
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

		public void ProcessCallMeBackTicketCaptcha(int Id)
		{
			// формирование капчи 
			var sub = new Random().Next(1000, 9999).ToString();
			HttpContext.Session.Add("captcha", sub);
			var pfc = new PrivateFontCollection();
			pfc.AddFontFile(Server.MapPath("~") + "/Fonts/captcha.ttf");
			var captchImage = DrawCaptchaText(sub, new Font(pfc.Families[0], 24, FontStyle.Bold), Color.Tomato, Color.White);
			var ms = new MemoryStream();
			captchImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
			HttpContext.Response.ContentType = "image/Jpeg";
			HttpContext.Response.BinaryWrite(ms.ToArray());
		}

		/// <summary>
		/// Изображение для капчи
		/// </summary> 
		private Image DrawCaptchaText(String text, Font font, Color textColor, Color backColor)
		{
			//first, create a dummy bitmap just to get a graphics object
			Image img = new Bitmap(1, 1);
			Graphics drawing = Graphics.FromImage(img);

			//measure the string to see how big the image needs to be
			SizeF textSize = drawing.MeasureString(text, font);

			//free up the dummy image and old graphics object
			img.Dispose();
			drawing.Dispose();

			//create a new image of the right size
			img = new Bitmap((int)textSize.Width, (int)textSize.Height);

			drawing = Graphics.FromImage(img);

			//paint the background
			drawing.Clear(backColor);

			//create a brush for the text
			Brush textBrush = new SolidBrush(textColor);

			drawing.DrawString(text, font, textBrush, 0, 0);

			drawing.Save();

			textBrush.Dispose();
			drawing.Dispose();

			return img;
		}

		private void ProcessCallMeBackTicket()
		{
			var binder = new EntityBinder();
			var callMeBackTicket = (CallMeBackTicket)binder.MapModel(Request, typeof(CallMeBackTicket));
			ViewBag.CallMeBackTicket = callMeBackTicket;
			//проверка капчи 
			if (CurrentClient == null && (callMeBackTicket.Captcha.Length < 4
			                              || (HttpContext.Session["captcha"].ToString()).IndexOf(callMeBackTicket.Captcha) == -1)) {
				callMeBackTicket.Captcha = "";
			}
			if (Request.Params["callMeBackTicket.Name"] == null) {
				callMeBackTicket.Captcha = "";
				return;
			}

			callMeBackTicket.Client = CurrentClient;

			var errors = ValidationRunner.Validate(callMeBackTicket);
			if (CurrentClient != null) {
				errors.RemoveErrors("CallMeBackTicket", "Captcha");
			}
			if (errors.Length == 0) {
				DbSession.Save(callMeBackTicket);
				if (callMeBackTicket.Client != null) {
					var appeal = new Appeal("Клиент создал запрос на обратный звонок № " + callMeBackTicket.Id,
						callMeBackTicket.Client, AppealType.FeedBack) {
							Employee = GetCurrentEmployee()
						};
					DbSession.Save(appeal);
				}
				ViewBag.CallMeBackTicket = new CallMeBackTicket();
				SuccessMessage("Заявка отправлена. В течении дня вам перезвонят.");
				return;
			}
			callMeBackTicket.Captcha = "";
			if (GetJavascriptParam("CallMeBack") == null)
				AddJavascriptParam("CallMeBack", "1");
		}

		private void ProcessRegionPanel()
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

		/// <summary>
		/// Вывод приватного сообщения в ЛК клиенту
		/// </summary>
		private void ProcessPrivateMessage()
		{
			var message = DbSession.Query<PrivateMessage>().
				FirstOrDefault(pm => pm.Client == CurrentClient && pm.Enabled && pm.EndDate.Date > SystemTime.Today());
			ViewBag.PrivateMsg = message;
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

			builder.Append("Дата: " + SystemTime.Now() + " \n ");
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