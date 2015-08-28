using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using Inforoom2.Components;
using Inforoom2.Models;
using InternetInterface.Models;
using NHibernate.Linq;
using NHibernate.Util;
using PackageSpeed = Inforoom2.Models.PackageSpeed;

namespace InforoomControlPanel.Controllers
{
	//TODO: файлы без ссылки!??
	/// <summary>
	/// Страница управления слайдами
	/// </summary>
	public class SlideController : ControlPanelController
	{
		/// <summary>
		/// Список слайдов
		/// </summary>
		public ActionResult SlideIndex()
		{
			var slides = DbSession.Query<Slide>().OrderBy(s => s.Region.Name).ThenByDescending(s => s.Priority).ToList();
			ViewBag.Slides = slides;
			var pathFromConfigUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadUrl"];
			if (pathFromConfigUrl == null) {
				throw new Exception("Значение 'inforoom2UploadUrl' отсуствует в Global.config либо невозможно найти сам Global.config !");
			}
			ViewBag.pathFromConfigURL = pathFromConfigUrl;
			return View();
		}

		/// <summary>
		/// Форма добавление слайда
		/// </summary>
		public ActionResult CreateSlide()
		{
			//Создаем слайд
			var slide = new Slide() { Enabled = true };
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.Slide = slide;
			ViewBag.RegionList = regionList;

			return View();
		}

		/// <summary>
		///		Добавление слайда в базу
		/// </summary>
		/// <param name="slide">Новый слайд</param>
		/// <param name="uploadedFile">Изображение</param> 
		[HttpPost]
		public ActionResult CreateSlide([EntityBinder] Slide slide, HttpPostedFileBase uploadedFile)
		{
			string imagePath = "";
			var ext = uploadedFile == null ? "" : new FileInfo(uploadedFile.FileName).Extension;
			string NewFileName = System.Guid.NewGuid() + ext;
			var pathFromConfig = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadPath"];
			var pathFromConfigUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadUrl"];
			if (pathFromConfig == null) {
				throw new Exception("Значение 'inforoom2UploadPath' отсуствует в Global.config либо невозможно найти сам Global.config !");
			}
			if (pathFromConfigUrl == null) {
				throw new Exception("Значение 'inforoom2UploadUrl' отсуствует в Global.config либо невозможно найти сам Global.config !");
			}
			ViewBag.pathFromConfigURL = pathFromConfigUrl;
			if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") {
				try
				{
                    //если путь = корню
				    pathFromConfig = pathFromConfig == "/" ? Server.MapPath("~") + pathFromConfig : pathFromConfig;
					imagePath = pathFromConfig + "Images/" + NewFileName;
					uploadedFile.SaveAs(imagePath);
				}
				catch (Exception уч) {
					imagePath = "";
				}
			}
			slide.ImagePath = "";
			slide.LastEdit = DateTime.Now;
			slide.Partner = DbSession.Query<Employee>().FirstOrDefault(s => s.Login == User.Identity.Name);
			var errors = ValidationRunner.Validate(slide);
			if (errors.Length == 0 && imagePath != "") {
				slide.ImagePath = "Images/" + NewFileName;
				DbSession.Save(slide);
				SuccessMessage("Слайд успешно добавлен");
				return RedirectToAction("SlideIndex");
			}
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.RegionList = regionList;
			ViewBag.Slide = slide;
			return View("CreateSlide");
		}

		/// <summary>
		/// Просмотр слайда
		/// </summary>
		public ActionResult EditSlide(int id)
		{ 
			var pathFromConfigUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadUrl"];
			if (pathFromConfigUrl == null)
			{
				throw new Exception("Значение 'inforoom2UploadUrl' отсуствует в Global.config либо невозможно найти сам Global.config !");
			}
			ViewBag.pathFromConfigURL = pathFromConfigUrl;
			//Создаем слайд
			var slide = DbSession.Query<Slide>().FirstOrDefault(s => s.Id == id);
			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.Slide = slide;
			ViewBag.RegionList = regionList;

			return View();
		}

		/// <summary>
		///		Изменение слайда
		/// </summary>
		/// <param name="slide">Измененный слайд</param>
		/// <param name="uploadedFile">Изображение</param> 
		public ActionResult UpdateSlide([EntityBinder] Slide slide, HttpPostedFileBase uploadedFile)
		{
			string imagePath = slide.ImagePath ?? "";
			var pathFromConfig = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadPath"];
			var pathFromConfigUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadUrl"];
			if (pathFromConfig == null) {
				throw new Exception("Значение 'inforoom2UploadPath' отсуствует в Global.config либо невозможно найти сам Global.config !");
			}
			if (pathFromConfigUrl == null) {
				throw new Exception("Значение 'inforoom2UploadUrl' отсуствует в Global.config либо невозможно найти сам Global.config !");
			}
			ViewBag.pathFromConfigURL = pathFromConfigUrl;
			var ext = uploadedFile == null ? "" : new FileInfo(uploadedFile.FileName).Extension;
			string NewFileName = System.Guid.NewGuid() + ext;
			if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") {
				try
                {//если путь = корню
                    pathFromConfig = pathFromConfig == "/" ? Server.MapPath("~") + pathFromConfig : pathFromConfig;
                    imagePath = pathFromConfig + "Images/" + NewFileName;
					uploadedFile.SaveAs(imagePath);
				}
				catch (Exception) {
					imagePath = "";
				}
			}
			slide.Partner = DbSession.Query<Employee>().FirstOrDefault(s => s.Login == User.Identity.Name);
			var errors = ValidationRunner.Validate(slide);
			if (errors.Length == 0 && imagePath != "") {
				if (uploadedFile != null)
                {//если путь = корню
                    pathFromConfig = pathFromConfig == "/" ? Server.MapPath("~") + pathFromConfig : pathFromConfig;
                    if (System.IO.File.Exists(pathFromConfig + slide.ImagePath)) {
						System.IO.File.Delete(pathFromConfig + slide.ImagePath);
					}
					slide.ImagePath = "Images/" + NewFileName;
				}
				DbSession.Save(slide);
				SuccessMessage("Слайд успешно сохранен");
				return RedirectToAction("SlideIndex");
			}

			var regionList = DbSession.Query<Region>().OrderBy(s => s.Name).ToList();
			ViewBag.RegionList = regionList;
			ViewBag.Slide = slide;
			return View("EditSlide");
		}


		/// <summary>
		/// Повышение приоритета слайда
		/// </summary>
		public ActionResult SlidePriorityIncerease(int id)
		{
			var slide = DbSession.Get<Slide>(id);
			slide.IncereasePriority(DbSession);
			return RedirectToAction("SlideIndex");
		}

		/// <summary>
		/// Понижение приоритета слайда
		/// </summary>
		public ActionResult SlidePriorityDecrease(int id)
		{
			var slide = DbSession.Get<Slide>(id);
			slide.DecreasePriority(DbSession);
			return RedirectToAction("SlideIndex");
		}

		/// <summary>
		/// Удаление слайда
		/// </summary>
		public ActionResult DeleteSlide(int id)
		{
			var pathFromConfig = System.Web.Configuration.WebConfigurationManager.AppSettings["inforoom2UploadPath"];
			if (pathFromConfig == null) {
				throw new Exception("Значение 'inforoom2UploadPath' отсуствует в Global.config либо невозможно найти сам Global.config !");
			}
			var slide = DbSession.Query<Slide>().FirstOrDefault(s => s.Id == id);
            //если путь = корню
            pathFromConfig = pathFromConfig == "/" ? Server.MapPath("~") + pathFromConfig : pathFromConfig;
            if (System.IO.File.Exists(pathFromConfig + slide.ImagePath)) {
				System.IO.File.Delete(pathFromConfig + slide.ImagePath);
			}

			DbSession.Delete(slide);
			DbSession.Flush();
			SuccessMessage("Слайд успешно удален");
			return RedirectToAction("SlideIndex");
		}
	}
}