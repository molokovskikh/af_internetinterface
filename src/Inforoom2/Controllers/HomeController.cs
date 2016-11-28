using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class HomeController : Inforoom2Controller
	{

		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server, VaryByCustom = "User,Cookies")]
		public ActionResult Index()
		{
			var pathFromConfigUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadUrl"];
			if (pathFromConfigUrl == null)
			{
				throw new Exception("Значение 'inforoom2UploadUrl' отсуствует в Global.config!");
			}

			ViewBag.pathFromConfigURL = pathFromConfigUrl;
			ViewBag.Message = "HomeController";
			var news = DbSession.Query<NewsBlock>().Where(k => k.IsPublished && (k.Region == CurrentRegion 
				|| k.Region == null)).OrderByDescending(n => n.Priority).ToList();
			var newsList = new List<NewsBlock>();
			int i = 0;
			foreach (var newsBlock in news)
			{
				i++;
				if (i < 4 && newsBlock != null)
				{
					newsList.Add(newsBlock);
				}
			}
			ViewBag.News = newsList; 

			ViewBag.SlideList = DbSession.Query<Slide>().Where(k => k.Enabled && (k.Region == CurrentRegion
				|| k.Region == null)).OrderByDescending(n => n.Priority).ToList();
			var bannerList = DbSession.Query<Banner>().Where(k => k.Enabled && k.Type == BannerType.ForMainPage).ToList();

			ViewBag.Banner = bannerList.FirstOrDefault(s => s.Region != null && CurrentRegion != null &&
				s.Region.Id == CurrentRegion.Id)?? bannerList.FirstOrDefault(s => s.Region == null) ?? new Banner();  
			 
			return View();
		}

		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server, VaryByParam = "*", VaryByCustom = "User,Cookies")]
		public ActionResult Plans(int? id)
		{
			ViewBag.Client = CurrentClient;
			ViewBag.ContentHtml = DbSession.Query<PlanHtmlContent>().FirstOrDefault(s => s.Region == CurrentRegion);
			if (id != null) {
				var plan = DbSession.Get<Plan>(id);
				ViewBag.Plan = plan;
			}
			ViewBag.Plans = DbSession.Query<RegionPlan>().Where(s => s.Region == CurrentRegion && s.Plan.AvailableForNewClients && s.Plan.Disabled==false)
				.OrderBy(s=>s.Region).ThenByDescending(s=>s.Plan.Priority).Select(s=>s.Plan)
				.Take(3).ToList();
			return View();
		}

		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server, VaryByParam = "*",
			VaryByCustom = "User,Cookies")]
		public ActionResult PricesList()
		{
			var model =
				DbSession.Query<PublicData>()
					.Where(
						s => s.ItemType == PublicDataType.PriceList && s.Display && (s.Region == null || s.Region.Id == CurrentRegion.Id))
					.OrderBy(s => s.PositionIndex)
					.ToList();
			return View("ExtraServicesPriceList", model);
		}

		[OutputCache(Duration = 600, Location = System.Web.UI.OutputCacheLocation.Server, VaryByParam = "*")]
		public ActionResult ViewNewsBlock(int id)
		{
			var newsBlock = DbSession.Get<NewsBlock>(id);
			ViewBag.NewsBlock = newsBlock;
			return View();
		}

		public ActionResult PermanentHomeRedirect()
		{
			return RedirectToActionPermanent("Index");
		}



		/// <summary>
		/// формирование капчи 
		/// </summary> 
		/// <param name="Id">для мены капчи</param>
		public void ProcessCallMeBackTicketCaptcha(int Id)
		{
			// формирование значения капчи, сохранение его в сессии
			var sub = new Random().Next(1000, 9999).ToString();
			HttpContext.Session.Add("captcha", sub);
			// создание коллекции шрифтов
			var pfc = new PrivateFontCollection();
			// формирвоание изображения капчи
			var captchImage = DrawCaptchaText(sub,
				new Font(LoadFontFamily(System.IO.File.ReadAllBytes(Server.MapPath("~") + "/Fonts/captcha.ttf"),
					out pfc), 24, FontStyle.Bold), Color.Tomato, Color.White);
			//передача пользователю изображения капчи
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

		public ActionResult SubmitCallMeBackTicket(CallMeBackTicket callMeBackTicket, string urlBack)
		{
			ViewBag.CallMeBackTicket = callMeBackTicket;
			var filledCapcha = HttpContext.Session["captcha"] as string;
			callMeBackTicket.SetConfirmCaptcha(filledCapcha);
			callMeBackTicket.Client = CurrentClient;
		    if (string.IsNullOrEmpty(urlBack)) {
		        urlBack = Url.Action("Index");

		    }
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
				return Redirect(urlBack);
			}
			ErrorMessage($"Заявка не была отправлена: {errors.First().Message} ");
			if (GetJavascriptParam("CallMeBack") == null)
				AddJavascriptParam("CallMeBack", "1");
			return Redirect(urlBack);
		}

	}
}